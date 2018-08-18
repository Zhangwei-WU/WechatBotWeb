namespace WechatBotWeb.TableData
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.WebKey;
    using Microsoft.WindowsAzure.Storage.Table;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;
    using WechatBotWeb.Security;
    using WechatBotWeb.TableData.Entities;

    public class AuthenticationService : IAppAuthenticationService, IUserAuthenticationService
    {
        public const string AcknowledgeCodeTableName = "AcknowledgeCodes";
        public const string DirectLoginCodeTableName = "DirectLoginCodes";
        public const string RefreshTokenTableName = "RefreshTokens";

        private static SemaphoreSlim semaphoreForInitialize = new SemaphoreSlim(1, 1);

        private CloudTableClient tableClient;
        private KeyVaultClient keyVaultClient;
        private string signKeyIdentifier;
        private string encryptionKeyIdentifier;

        private JsonWebKey jwk;
        private byte[] encryptionKey;
        private Random rnd;

        IApplicationInsights insight;

        public AuthenticationService(IApplicationInsights insight, CloudTableClient table, KeyVaultClient keyVault, string signKeyIdentifier, string encryptionKeyIdentifier)
        {
            this.insight = insight ?? throw new ArgumentNullException("insight");
            tableClient = table ?? throw new ArgumentNullException("table");
            keyVaultClient = keyVault ?? throw new ArgumentNullException("keyVault");

            this.signKeyIdentifier = signKeyIdentifier;
            this.encryptionKeyIdentifier = encryptionKeyIdentifier;
            
            rnd = new Random();
        }

        public async Task<IAppAuthenticationToken> CreateAppTokenAsync(ISession session)
        {
            if (session == null || string.IsNullOrEmpty(session.ClientDeviceId) || string.IsNullOrEmpty(session.ClientSessionId)) return AppAuthenticationToken.BadRequest;

            // check banned session

            var accssToken = await CreateAccessToken(new AppIdentity
            {
                ClientDeviceId = session.ClientDeviceId,
                ClientSessionId = session.ClientSessionId,
                ExpireIn = DateTime.MaxValue.Ticks,
                IdentityType = IdentityType.App,
                IsAuthenticated = true,
                Name = session.ClientDeviceId
            });

            return new AppAuthenticationToken
            {
                AccessToken = accssToken,
                Status = StatusCode.Confirmed
            };
        }

        #region CreateCodeAsync
        public async Task<IUserAuthenticationCode> CreateCodeAsync(ISessionIdentity identity, ICreateUserAuthenticationCodeRequest request)
        {
            if (identity == null || !identity.IsAuthenticated) return UserAuthenticationCode.UnAuthorized;

            if (request.CodeType == UserAuthenticationCodeType.AcknowledgeCode)
            {
                return await CreateAcknowledgeCodeAsync(identity, request);
            }
            else if (request.CodeType == UserAuthenticationCodeType.DirectLoginCode)
            {
                return await CreateDirectLoginCodeAsync(identity, request);
            }
            else
            {
                throw new ArgumentOutOfRangeException("request.CodeType");
            }
        }

        private async Task<IUserAuthenticationCode> CreateAcknowledgeCodeAsync(ISessionIdentity identity, ICreateUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.App) return UserAuthenticationCode.UnAuthorized;

            var table = tableClient.GetTableReference(AcknowledgeCodeTableName);

            var retry = true;
            while (retry)
            {
                retry = false;

                var code = GenerateCode(6);
                var now = DateTime.UtcNow;
                var nowTicks = now.Ticks;
                var expireIn = now.AddMinutes(1).Ticks;
                var availableAfter = now.AddMinutes(5).Ticks;

                var rowKey = nowTicks.ToString("X16");

                var result = await insight.WatchAsync(
                    async () => await table.RetrieveAsync<AcknowledgeCodeStatusEntity>(code, 0L.ToString("X16")),
                    (r, e) =>e.Status = r.HttpStatusCode.ToString(),
                    ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

                if (result.HttpStatusCode >= 400 && result.HttpStatusCode != 404) return UserAuthenticationCode.Error(result.HttpStatusCode, "Error retrieving acknowledge code status");

                var statusEntity = result.Entity;
                if (statusEntity == null || statusEntity.AvailableAfter < nowTicks)
                {
                    if (statusEntity == null)
                    {
                        statusEntity = new AcknowledgeCodeStatusEntity
                        {
                            PartitionKey = code,
                            RowKey = 0L.ToString("X16"),
                        };
                    }

                    statusEntity.CurrentRowKey = rowKey;
                    statusEntity.AvailableAfter = availableAfter;

                    var status = await insight.WatchAsync(
                        async () => await table.InsertOrMergeAsync(statusEntity),
                        (r, e) => e.Status = r.ToString(),
                        ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertOrMergeAsync", "TableName", table.Name);

                    if (status >= 400) return UserAuthenticationCode.Error(status, "Error updating acknowledge code status");
                }
                else
                {
                    // not available, just retry
                    retry = true;
                    continue;
                }

                var codeEntity = new AcknowledgeCodeEntity
                {
                    PartitionKey = code,
                    RowKey = rowKey,
                    ExpireIn = expireIn,
                    Status = (int)StatusCode.Pending,
                    TargetUser = request.TargetUser,
                    ReqDeviceId = identity.ClientDeviceId,
                    ReqSessionId = identity.ClientSessionId
                };

                var insertStatus = await insight.WatchAsync(
                    async () => await table.InsertAsync(codeEntity),
                    (r, e) => e.Status = r.ToString(),
                    ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertAsync", "TableName", table.Name);

                if (insertStatus >= 400) return UserAuthenticationCode.Error(insertStatus, "Error inserting new acknowledge code");

                return new UserAuthenticationCode
                {
                    Code = code,
                    ExpireIn = expireIn,
                    Status = (StatusCode)codeEntity.Status,
                    TargetUser = codeEntity.TargetUser
                };
            }

            throw new NotImplementedException();
        }

        private async Task<IUserAuthenticationCode> CreateDirectLoginCodeAsync(ISessionIdentity identity, ICreateUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.Bot) return UserAuthenticationCode.UnAuthorized;
            if (string.IsNullOrEmpty(request.TargetUser)) return UserAuthenticationCode.BadRequest;

            var table = tableClient.GetTableReference(DirectLoginCodeTableName);

            var retry = true;
            while (retry)
            {
                retry = false;

                var now = DateTime.UtcNow;
                var prefix = now.ToString("yyyyMMdd");
                var code = prefix + GenerateCode(32);

                var expireIn = now.AddMinutes(1).Ticks;

                var codeEntity = new DirectLoginCodeEntity
                {
                    PartitionKey = prefix,
                    RowKey = code,
                    TargetUser = request.TargetUser,
                    ExpireIn = expireIn,
                    ReqVia = identity.Name,
                    Status = (int)StatusCode.Pending
                };

                var status = await insight.WatchAsync(
                    async () => await table.InsertAsync(codeEntity),
                    (r, e) => e.Status = r.ToString(),
                    ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertAsync", "TableName", table.Name);
                if (status == 409)
                {
                    retry = true;
                    continue;
                }

                if (status >= 400) return UserAuthenticationCode.Error(status, "Error inserting new directlogin code");

                return new UserAuthenticationCode
                {
                    Code = code,
                    ExpireIn = expireIn,
                    Status = (StatusCode)codeEntity.Status,
                    TargetUser = codeEntity.TargetUser
                };
            }

            throw new NotImplementedException();
        }

        #endregion

        #region AcknowledgeCodeAsync
        public async Task<IUserAuthenticationCode> AcknowledgeCodeAsync(ISessionIdentity identity, IAcknowledgeUserAuthenticationCodeRequest request)
        {
            if (request.CodeType == UserAuthenticationCodeType.AcknowledgeCode)
            {
                return await AcknowledgeAcknowledgeCodeAsync(identity, request);
            }
            else if (request.CodeType == UserAuthenticationCodeType.DirectLoginCode)
            {
                return await AcknowledgeDirectLoginCodeAsync(identity, request);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task<IUserAuthenticationCode> AcknowledgeAcknowledgeCodeAsync(ISessionIdentity identity, IAcknowledgeUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.Bot) return UserAuthenticationCode.UnAuthorized;
            if (string.IsNullOrEmpty(request.Code)) return UserAuthenticationCode.BadRequest;

            var table = tableClient.GetTableReference(AcknowledgeCodeTableName);

            var statusResult = await insight.WatchAsync(
                async () => await table.RetrieveAsync<AcknowledgeCodeStatusEntity>(request.Code, 0L.ToString("X16")),
                (r, e) => e.Status = r.HttpStatusCode.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

            if (statusResult.HttpStatusCode == 404) return UserAuthenticationCode.BadRequest;
            if (statusResult.HttpStatusCode >= 400) return UserAuthenticationCode.Error(statusResult.HttpStatusCode, "Error retrieving acknowledge code status");

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var statusEntity = statusResult.Entity;

            if (statusEntity.AvailableAfter < nowTicks) return UserAuthenticationCode.Expired;

            var rowKey = statusEntity.CurrentRowKey;

            var codeResult = await insight.WatchAsync(
                async () => await table.RetrieveAsync<AcknowledgeCodeEntity>(request.Code, rowKey),
                (r, e) => e.Status = r.HttpStatusCode.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

            if (codeResult.HttpStatusCode >= 400) return UserAuthenticationCode.Error(codeResult.HttpStatusCode, "Error retrieving acknowledge code");

            var codeEntity = codeResult.Entity;
            if (codeEntity.ExpireIn < nowTicks) return UserAuthenticationCode.Expired;
            if (codeEntity.Status != (int)StatusCode.Pending) return UserAuthenticationCode.BadRequest;
            if (!string.IsNullOrEmpty(codeEntity.TargetUser) && string.Compare(codeEntity.TargetUser, request.AcknowledgeUser, true) != 0) return UserAuthenticationCode.UnAuthorized;

            codeEntity.AckBy = request.AcknowledgeUser;
            codeEntity.Ackime = nowTicks;
            codeEntity.AckVia = identity.Name;
            codeEntity.Status = (int)StatusCode.Confirmed;

            var result = await insight.WatchAsync(
                async () => await table.MergeAsync(codeEntity),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "MergeAsync", "TableName", table.Name);

            if (result >= 400) return UserAuthenticationCode.Error(result, "Error updating acknowledge code");

            return new UserAuthenticationCode
            {
                Code = codeEntity.PartitionKey,
                ExpireIn = codeEntity.ExpireIn,
                Status = (StatusCode)codeEntity.Status,
                TargetUser = codeEntity.AckBy
            };
        }

        private async Task<IUserAuthenticationCode> AcknowledgeDirectLoginCodeAsync(ISessionIdentity identity, IAcknowledgeUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.App) return UserAuthenticationCode.UnAuthorized;
            if (string.IsNullOrEmpty(request.Code) || request.Code.Length != 40) return UserAuthenticationCode.BadRequest;

            var table = tableClient.GetTableReference(DirectLoginCodeTableName);

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var result = await insight.WatchAsync(
                async () => await table.RetrieveAsync<DirectLoginCodeEntity>(request.Code.Substring(0, 8), request.Code),
                (r, e) => e.Status = r.HttpStatusCode.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

            if (result.HttpStatusCode == 404) return UserAuthenticationCode.BadRequest;
            if (result.HttpStatusCode >= 400) return UserAuthenticationCode.Error(result.HttpStatusCode, "Error retrieving directlogin code");

            var codeEntity = result.Entity;

            if (codeEntity.ExpireIn < nowTicks) return UserAuthenticationCode.Expired;
            if (codeEntity.Status != (int)StatusCode.Pending) return UserAuthenticationCode.BadRequest;

            codeEntity.AckDeviceId = identity.ClientDeviceId;
            codeEntity.AckSessionId = identity.ClientSessionId;
            codeEntity.AckTime = nowTicks;
            codeEntity.Status = (int)StatusCode.Confirmed;

            var mergeResult = await insight.WatchAsync(
                async () => await table.MergeAsync(codeEntity),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "MergeAsync", "TableName", table.Name);

            if (mergeResult >= 400) return UserAuthenticationCode.Error(mergeResult, "Error updating directlogin code");

            return new UserAuthenticationCode
            {
                Code = codeEntity.RowKey,
                ExpireIn = codeEntity.ExpireIn,
                Status = (StatusCode)codeEntity.Status,
                TargetUser = codeEntity.TargetUser
            };
        }

        #endregion

        #region TryGetTokenByCodeAsync
        public async Task<IUserAuthenticationToken> TryGetTokenByCodeAsync(ISessionIdentity identity, IGetUserAuthenticationCodeRequest request)
        {
            if (identity == null || !identity.IsAuthenticated) return UserAuthenticationToken.UnAuthorized;
            if (identity.IdentityType != IdentityType.App) return UserAuthenticationToken.UnAuthorized;

            if (request.CodeType == UserAuthenticationCodeType.AcknowledgeCode)
            {
                return await TryGetTokenByAcknowledgeCodeAsync(identity, request);
            }
            else if (request.CodeType == UserAuthenticationCodeType.DirectLoginCode)
            {
                return await TryGetTokenByDirectLoginCodeAsync(identity, request);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task<IUserAuthenticationToken> TryGetTokenByAcknowledgeCodeAsync(ISessionIdentity identity, IGetUserAuthenticationCodeRequest request)
        {
            if (identity == null || !identity.IsAuthenticated || identity.IdentityType != IdentityType.App) return UserAuthenticationToken.UnAuthorized;

            var table = tableClient.GetTableReference(AcknowledgeCodeTableName);

            var statusResult = await insight.WatchAsync(
                async () => await table.RetrieveAsync<AcknowledgeCodeStatusEntity>(request.Code, 0L.ToString("X16")),
                (r, e) => e.Status = r.HttpStatusCode.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

            if (statusResult.HttpStatusCode == 404) return UserAuthenticationToken.BadRequest;
            if (statusResult.HttpStatusCode >= 400) return UserAuthenticationToken.Error(statusResult.HttpStatusCode, "Error retrieving acknowledge code status");

            var statusEntity = statusResult.Entity;

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            if (statusEntity.AvailableAfter < nowTicks) return UserAuthenticationToken.Expired;

            var codeResult = await insight.WatchAsync(
                async () => await table.RetrieveAsync<AcknowledgeCodeEntity>(request.Code, statusEntity.CurrentRowKey),
                (r, e) => e.Status = r.HttpStatusCode.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

            if (codeResult.HttpStatusCode >= 400) return UserAuthenticationToken.Error(codeResult.HttpStatusCode, "Error retrieving acknowledge code");

            var codeEntity = codeResult.Entity;

            if (codeEntity.ReqDeviceId != identity.ClientDeviceId || codeEntity.ReqSessionId != identity.ClientSessionId) return UserAuthenticationToken.UnAuthorized;
            if (codeEntity.ExpireIn < nowTicks) return UserAuthenticationToken.Expired;
            if (codeEntity.Status == (int)StatusCode.Pending) return UserAuthenticationToken.Pending;
            if (codeEntity.Status != (int)StatusCode.Confirmed) return UserAuthenticationToken.BadRequest;

            codeEntity.Status = (int)StatusCode.Expired;

            var mergeResult = await insight.WatchAsync(
                async () => await table.MergeAsync(codeEntity),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "MergeAsync", "TableName", table.Name);

            if (mergeResult >= 400) return UserAuthenticationToken.Error(mergeResult, "Error updating acknowledge code");

            return await CreateUserTokenAsync(identity, codeEntity.AckBy);
        }

        private async Task<IUserAuthenticationToken> TryGetTokenByDirectLoginCodeAsync(ISessionIdentity identity, IGetUserAuthenticationCodeRequest request)
        {
            if (string.IsNullOrEmpty(request.Code) || request.Code.Length != 40) return UserAuthenticationToken.BadRequest;

            var table = tableClient.GetTableReference(DirectLoginCodeTableName);

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var codeResult = await insight.WatchAsync(
                async () => await table.RetrieveAsync<DirectLoginCodeEntity>(request.Code.Substring(0, 8), request.Code),
                (r, e) => e.Status = r.HttpStatusCode.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

            if (codeResult.HttpStatusCode == 404) return UserAuthenticationToken.BadRequest;
            if (codeResult.HttpStatusCode >= 400) return UserAuthenticationToken.Error(codeResult.HttpStatusCode, "Error retrieving directlogin code");

            var codeEntity = codeResult.Entity;

            if (codeEntity.ExpireIn < nowTicks) return UserAuthenticationToken.Expired;
            if (codeEntity.Status == (int)StatusCode.Pending) return UserAuthenticationToken.Pending;
            if (codeEntity.Status != (int)StatusCode.Confirmed) return UserAuthenticationToken.BadRequest;

            codeEntity.Status = (int)StatusCode.Expired;
            var mergeResult = await insight.WatchAsync(
                async () => await table.MergeAsync(codeEntity),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "MergeAsync", "TableName", table.Name);

            if (mergeResult >= 400) return UserAuthenticationToken.Error(mergeResult, "Error updating directlogin code");

            return await CreateUserTokenAsync(identity, codeEntity.TargetUser);
        }
        #endregion


        public async Task<IUserAuthenticationToken> RefreshTokenAsync(ISessionIdentity identity, IRefreshUserAuthenticationTokenRequest request)
        {
            if (identity == null || !identity.IsAuthenticated || identity.IdentityType != IdentityType.App) return UserAuthenticationToken.UnAuthorized;

            var token = request.RefreshToken;
            if (string.IsNullOrEmpty(token) || token.Length != 32) return UserAuthenticationToken.BadRequest;

            var partitionKey = token.Substring(0, 8);

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var table = tableClient.GetTableReference(RefreshTokenTableName);

            var tokenResult = await insight.WatchAsync(
                async () => await table.RetrieveAsync<RefreshTokenEntity>(partitionKey, token),
                (r, e) => e.Status = r.HttpStatusCode.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "RetrieveAsync", "TableName", table.Name);

            if (tokenResult.HttpStatusCode == 404) return UserAuthenticationToken.BadRequest;
            if (tokenResult.HttpStatusCode >= 400) return UserAuthenticationToken.Error(tokenResult.HttpStatusCode, "Error retrieving refresh token");

            var tokenEntity = tokenResult.Entity;

            if (tokenEntity.ClaimTime != 0L || tokenEntity.ExpireTime < nowTicks) return UserAuthenticationToken.Expired;

            tokenEntity.ClaimTime = nowTicks;
            var mergeResult = await insight.WatchAsync(
                async () => await table.MergeAsync(tokenEntity),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "MergeAsync", "TableName", table.Name);

            if (mergeResult >= 400) return UserAuthenticationToken.Error(mergeResult, "Error updating refresh token");

            return await CreateUserTokenAsync(identity, tokenEntity.TargetUser);
        }

        public async Task<ISessionIdentity> ValidateTokenAsync(ISession session, string scheme, string token)
        {
            var anonymous = AnonymousIdentity.Anonymous(session.ClientDeviceId, session.ClientSessionId);
            if (string.IsNullOrEmpty(token)) return anonymous;

            var parts = token.Split('.');
            if (parts.Length != 3) return anonymous;

            throw new NotImplementedException();
        }

        private char[] codeCandidates = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();

        /// <summary>
        /// generate code 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        private string GenerateCode(int length)
        {
            var code = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                lock (rnd) code.Append(codeCandidates[rnd.Next(codeCandidates.Length)]);
            }

            return code.ToString();
        }

        private async Task<IUserAuthenticationToken> CreateUserTokenAsync(ISession session, string name)
        {
            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var user = new UserIdentity
            {
                ClientDeviceId = session.ClientDeviceId,
                ClientSessionId = session.ClientSessionId,
                IdentityType = IdentityType.User,
                IsAuthenticated = true,
                Name = name,
                ExpireIn = now.AddMinutes(60).Ticks
            };

            var accessToken = await CreateAccessToken(user);

            var table = tableClient.GetTableReference(RefreshTokenTableName);

            var refreshToken = Guid.NewGuid().ToString("N");
            var refreshTokenExpire = now.AddDays(90).Ticks;

            var insertResult = await insight.WatchAsync(
                async () => await table.InsertAsync(
                    new RefreshTokenEntity
                    {
                        CreateTime = nowTicks,
                        DeviceId = user.ClientDeviceId,
                        ExpireTime = refreshTokenExpire,
                        PartitionKey = refreshToken.Substring(0, 8),
                        RowKey = refreshToken,
                        TargetUser = user.Name,
                        ClaimTime = 0L
                    }),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertAsync", "TableName", table.Name);

            if (insertResult >= 400) return UserAuthenticationToken.Error(insertResult, "Error inserting refresh token");

            return new UserAuthenticationToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpireIn = now.AddMinutes(60).Ticks,
                Status = StatusCode.Confirmed
            };
        }

        private async Task<string> CreateAccessToken(ISessionIdentity identity)
        {
            if (encryptionKey == null)
            {
                await semaphoreForInitialize.WaitAsync();

                try
                {
                    if (encryptionKey == null) await InitializeSecrets();
                }
                finally
                {
                    semaphoreForInitialize.Release();
                }
            }

            var tokenBytes = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(identity));

            var encryptedToken = Aes.Encrypt(tokenBytes, encryptionKey);
            var hash = SHA256Hashing.Hash(tokenBytes);

            var tokenSignature = await insight.WatchAsync(
                async () => (await keyVaultClient.SignAsync(signKeyIdentifier, JsonWebKeySignatureAlgorithm.RS256, hash)).Result,
                null,
                ApplicationInsightEventNames.EventCallAzureKeyVaultSource, "Method", "SignAsync", "KeyIdentifier", signKeyIdentifier, "Algorithm", "RS256");

            return identity.IdentityType.ToString()
                + "." + Convert.ToBase64String(encryptedToken)
                + "." + Convert.ToBase64String(tokenSignature);
        }

        private async Task InitializeSecrets()
        {
            jwk = (await keyVaultClient.GetKeyAsync(signKeyIdentifier)).Key;
            encryptionKey = Convert.FromBase64String((await keyVaultClient.GetSecretAsync(encryptionKeyIdentifier)).Value);
        }
    }
}
