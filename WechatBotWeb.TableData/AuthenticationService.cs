namespace WechatBotWeb.TableData
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.WebKey;
    using Microsoft.WindowsAzure.Storage;
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

        private IApplicationInsights insight;
        private CloudTableClient tableClient;
        private KeyVaultClient keyVaultClient;
        private string tableIdentifier;
        private string signKeyIdentifier;
        private string encryptionKeyIdentifier;
        private bool initialized = false;

        private JsonWebKey jwk;
        private byte[] encryptionKey;
        private Random rnd;


        public AuthenticationService(IApplicationInsights insight, KeyVaultClient keyVault, string tableIdentifier, string signKeyIdentifier, string encryptionKeyIdentifier)
        {
            this.insight = insight ?? throw new ArgumentNullException("insight");
            this.keyVaultClient = keyVault ?? throw new ArgumentNullException("keyVault");

            this.tableIdentifier = tableIdentifier;
            this.signKeyIdentifier = signKeyIdentifier;
            this.encryptionKeyIdentifier = encryptionKeyIdentifier;
        }

        public async Task<IAppAuthenticationToken> CreateAppTokenAsync(ISession session)
        {
            if (session == null || string.IsNullOrEmpty(session.ClientDeviceId) || string.IsNullOrEmpty(session.ClientSessionId)) throw new HttpStatusException("BadRequest:Session") { Status = StatusCode.BadRequest };
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
                AccessToken = accssToken
            };
        }

        #region CreateCodeAsync
        public async Task<IUserAuthenticationCode> CreateCodeAsync(ISessionIdentity identity, ICreateUserAuthenticationCodeRequest request)
        {
            if (identity == null || !identity.IsAuthenticated) throw new HttpStatusException("Unauthorized:Identity") { Status = StatusCode.Unauthorized };
            
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
                throw new HttpStatusException($"Unknown:CreateUserAuthenticationCodeRequest.CodeType({request.CodeType})") { Status = StatusCode.BadRequest };
            }
        }

        private async Task<IUserAuthenticationCode> CreateAcknowledgeCodeAsync(ISessionIdentity identity, ICreateUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.App) throw new HttpStatusException($"NotApp:Identity({identity.IdentityType})") { Status = StatusCode.Unauthorized };

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

                var result = await table.RetrieveAsync<AcknowledgeCodeStatusEntity>(code, 0L.ToString("X16"));

                if (result.HttpStatusCode >= 400 && result.HttpStatusCode != 404) throw new HttpStatusException("EntityErrorRetrieve:AcknowledgeCodeStatus", result.HttpStatusCode);

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

                    var status = await table.InsertOrMergeAsync(statusEntity);

                    if (status >= 400) throw new HttpStatusException("EntityErrorUpdate:AcknowledgeCodeStatus", status);
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

                var insertStatus = await table.InsertAsync(codeEntity);

                if (insertStatus >= 400) throw new HttpStatusException("EntityErrorInsert:AcknowledgeCode", insertStatus);

                return new UserAuthenticationCode
                {
                    Code = code,
                    ExpireIn = expireIn,
                    TargetUser = codeEntity.TargetUser
                };
            }

            throw new NotImplementedException();
        }

        private async Task<IUserAuthenticationCode> CreateDirectLoginCodeAsync(ISessionIdentity identity, ICreateUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.Bot) throw new HttpStatusException($"NotBot:Identity{identity.IdentityType}") { Status = StatusCode.Unauthorized };
            if (string.IsNullOrEmpty(request.TargetUser)) throw new HttpStatusException("Empty:CreateUserAuthenticationCodeRequest.TargetUser") { Status = StatusCode.BadRequest };

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

                var status = await table.InsertAsync(codeEntity);
                if (status == 409)
                {
                    retry = true;
                    continue;
                }

                if (status >= 400) throw new HttpStatusException("EntityErrorInsert:DirectLoginCode", status);

                return new UserAuthenticationCode
                {
                    Code = code,
                    ExpireIn = expireIn,
                    TargetUser = codeEntity.TargetUser
                };
            }

            throw new NotImplementedException();
        }

        #endregion

        #region AcknowledgeCodeAsync
        public async Task<IUserAuthenticationCode> AcknowledgeCodeAsync(ISessionIdentity identity, IAcknowledgeUserAuthenticationCodeRequest request)
        {
            if (identity == null || !identity.IsAuthenticated) throw new HttpStatusException("Unauthorized:Identity") { Status = StatusCode.Unauthorized };

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
                throw new HttpStatusException($"Unknown:AcknowledgeUserAuthenticationCodeRequest.CodeType({request.CodeType})") { Status = StatusCode.BadRequest };
            }
        }

        private async Task<IUserAuthenticationCode> AcknowledgeAcknowledgeCodeAsync(ISessionIdentity identity, IAcknowledgeUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.Bot) throw new HttpStatusException($"NotBot:Identity{identity.IdentityType}") { Status = StatusCode.Unauthorized };
            if (string.IsNullOrEmpty(request.Code)) throw new HttpStatusException("Empty:AcknowledgeUserAuthenticationCodeRequest.Code") { Status = StatusCode.BadRequest };

            var table = tableClient.GetTableReference(AcknowledgeCodeTableName);

            var statusResult = await table.RetrieveAsync<AcknowledgeCodeStatusEntity>(request.Code, 0L.ToString("X16"));

            if (statusResult.HttpStatusCode == 404) throw new HttpStatusException("EntityNotFound:AcknowledgeCodeStatus", statusResult.HttpStatusCode) { Status = StatusCode.NotFound };
            if (statusResult.HttpStatusCode >= 400) throw new HttpStatusException("EntityErrorRetrieve:AcknowledgeCodeStatus", statusResult.HttpStatusCode);

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var statusEntity = statusResult.Entity;

            if (statusEntity.AvailableAfter < nowTicks) throw new HttpStatusException("Expired:AcknowledgeCodeStatus") { Status = StatusCode.NotFound };

            var rowKey = statusEntity.CurrentRowKey;

            var codeResult = await table.RetrieveAsync<AcknowledgeCodeEntity>(request.Code, rowKey);

            if (codeResult.HttpStatusCode >= 400) throw new HttpStatusException("EntityErrorRetrieve:AcknowledgeCode", codeResult.HttpStatusCode);

            var codeEntity = codeResult.Entity;
            if (codeEntity.ExpireIn < nowTicks) throw new HttpStatusException("Expired:AcknowledgeCode") { Status = StatusCode.NotFound };
            if (codeEntity.Status != (int)StatusCode.Pending) throw new HttpStatusException("EntityErrorStatus:AcknowledgeCode", codeEntity.Status) { Status = StatusCode.NotFound };
            if (!string.IsNullOrEmpty(codeEntity.TargetUser) && string.Compare(codeEntity.TargetUser, request.AcknowledgeUser, true) != 0) throw new HttpStatusException($"NotMatch:AcknowledgeUserAuthenticationCodeRequest.AcknowledgeUser({codeEntity.TargetUser}, {request.AcknowledgeUser})") { Status = StatusCode.NotFound };

            codeEntity.AckBy = request.AcknowledgeUser;
            codeEntity.Ackime = nowTicks;
            codeEntity.AckVia = identity.Name;
            codeEntity.Status = (int)StatusCode.Confirmed;

            var result = await table.MergeAsync(codeEntity);

            if (result >= 400) throw new HttpStatusException("EntityErrorUpdate:AcknowledgeCode", result);

            return new UserAuthenticationCode
            {
                Code = codeEntity.PartitionKey,
                ExpireIn = codeEntity.ExpireIn,
                TargetUser = codeEntity.AckBy
            };
        }

        private async Task<IUserAuthenticationCode> AcknowledgeDirectLoginCodeAsync(ISessionIdentity identity, IAcknowledgeUserAuthenticationCodeRequest request)
        {
            if (identity.IdentityType != IdentityType.App) throw new HttpStatusException($"NotApp:Identity({identity.IdentityType})") { Status = StatusCode.Unauthorized };
            if (string.IsNullOrEmpty(request.Code) || request.Code.Length != 40) throw new HttpStatusException($"Invalid:AcknowledgeUserAuthenticationCodeRequest.Code({request.Code})") { Status = StatusCode.BadRequest };

            var table = tableClient.GetTableReference(DirectLoginCodeTableName);

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var result = await table.RetrieveAsync<DirectLoginCodeEntity>(request.Code.Substring(0, 8), request.Code);

            if (result.HttpStatusCode == 404) throw new HttpStatusException("EntityNotFound:DirectLoginCode", result.HttpStatusCode) { Status = StatusCode.NotFound };
            if (result.HttpStatusCode >= 400) throw new HttpStatusException("EntityErrorRetrieve:DirectLoginCode", result.HttpStatusCode);

            var codeEntity = result.Entity;

            if (codeEntity.ExpireIn < nowTicks) throw new HttpStatusException("Expired:DirectLoginCode") { Status = StatusCode.NotFound };
            if (codeEntity.Status != (int)StatusCode.Pending) throw new HttpStatusException($"EntityErrorStatus:DirectLoginCode(Status={codeEntity.Status})", codeEntity.Status) { Status = StatusCode.NotFound };

            codeEntity.AckDeviceId = identity.ClientDeviceId;
            codeEntity.AckSessionId = identity.ClientSessionId;
            codeEntity.AckTime = nowTicks;
            codeEntity.Status = (int)StatusCode.Confirmed;

            var mergeResult = await table.MergeAsync(codeEntity);

            if (mergeResult >= 400) throw new HttpStatusException("EntityErrorUpdate:DirectLoginCode", mergeResult);

            return new UserAuthenticationCode
            {
                Code = codeEntity.RowKey,
                ExpireIn = codeEntity.ExpireIn,
                TargetUser = codeEntity.TargetUser
            };
        }

        #endregion

        #region TryGetTokenByCodeAsync
        public async Task<IUserAuthenticationToken> TryGetTokenByCodeAsync(ISessionIdentity identity, IGetUserAuthenticationCodeRequest request)
        {
            if (identity == null || !identity.IsAuthenticated) throw new HttpStatusException("Unauthorized:Identity") { Status = StatusCode.Unauthorized };
            if (identity.IdentityType != IdentityType.App) throw new HttpStatusException($"NotApp:Identity({identity.IdentityType})") { Status = StatusCode.BadRequest };
            
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
                throw new HttpStatusException($"GetUserAuthenticationCodeRequest.CodeType({request.CodeType})") { Status = StatusCode.BadRequest };
            }
        }

        private async Task<IUserAuthenticationToken> TryGetTokenByAcknowledgeCodeAsync(ISessionIdentity identity, IGetUserAuthenticationCodeRequest request)
        {
            var table = tableClient.GetTableReference(AcknowledgeCodeTableName);

            var statusResult = await table.RetrieveAsync<AcknowledgeCodeStatusEntity>(request.Code, 0L.ToString("X16"));

            if (statusResult.HttpStatusCode == 404) throw new HttpStatusException("EntityNotFound:AcknowledgeCodeStatus", statusResult.HttpStatusCode) { Status = StatusCode.NotFound };
            if (statusResult.HttpStatusCode >= 400) throw new HttpStatusException("EntityErrorRetrieve:AcknowledgeCodeStatus", statusResult.HttpStatusCode);

            var statusEntity = statusResult.Entity;

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            if (statusEntity.AvailableAfter < nowTicks) throw new HttpStatusException("Expired:AcknowledgeCodeStatus") { Status = StatusCode.NotFound };

            var codeResult = await table.RetrieveAsync<AcknowledgeCodeEntity>(request.Code, statusEntity.CurrentRowKey);

            if (codeResult.HttpStatusCode >= 400) throw new HttpStatusException("EntityErrorRetrieve:AcknowledgeCode", codeResult.HttpStatusCode);

            var codeEntity = codeResult.Entity;

            if (codeEntity.ReqDeviceId != identity.ClientDeviceId || codeEntity.ReqSessionId != identity.ClientSessionId)
                throw new HttpStatusException(
                    $"NotMatch:Session(OriginalRequestDeviceId={codeEntity.ReqDeviceId}, IdentityDeviceId={identity.ClientDeviceId}, OriginalRequestSessionId={codeEntity.ReqSessionId}, IdentitySessionId={identity.ClientSessionId})")
                    { Status = StatusCode.NotFound };

            if(codeEntity.ExpireIn < nowTicks) throw new HttpStatusException("Expired:AcknowledgeCode") { Status = StatusCode.NotFound };

            if (codeEntity.Status == (int)StatusCode.Pending) return new UserAuthenticationToken { Validated = false };
            if (codeEntity.Status != (int)StatusCode.Confirmed) throw new HttpStatusException($"EntityErrorStatus:AcknowledgeCode(Status={codeEntity.Status})", codeEntity.Status) { Status = StatusCode.NotFound };

            codeEntity.Status = (int)StatusCode.Expired;

            var mergeResult = await table.MergeAsync(codeEntity);

            if (mergeResult >= 400) throw new HttpStatusException("EntityErrorUpdate:AcknowledgeCode", mergeResult);

            return await CreateUserTokenAsync(identity, codeEntity.AckBy, UserVerificationLevel.StrongSignIn);
        }

        private async Task<IUserAuthenticationToken> TryGetTokenByDirectLoginCodeAsync(ISessionIdentity identity, IGetUserAuthenticationCodeRequest request)
        {
            if (string.IsNullOrEmpty(request.Code) || request.Code.Length != 40) throw new HttpStatusException($"BadRequest:GetUserAuthenticationCodeRequest.Code({request.Code})") { Status = StatusCode.BadRequest };

            var table = tableClient.GetTableReference(DirectLoginCodeTableName);

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var codeResult = await table.RetrieveAsync<DirectLoginCodeEntity>(request.Code.Substring(0, 8), request.Code);

            if (codeResult.HttpStatusCode == 404) throw new HttpStatusException("EntityNotFound:DirectLoginCode", codeResult.HttpStatusCode) { Status = StatusCode.NotFound };
            if (codeResult.HttpStatusCode >= 400) throw new HttpStatusException("EntityErrorRetrieve:DirectLoginCode", codeResult.HttpStatusCode);

            var codeEntity = codeResult.Entity;

            if (codeEntity.ExpireIn < nowTicks) throw new HttpStatusException("Expired:DirectLoginCode") { Status = StatusCode.NotFound };
            if (codeEntity.Status != (int)StatusCode.Confirmed) throw new HttpStatusException($"EntityErrorStatus:DirectLoginCode(Status={codeEntity.Status})", codeEntity.Status) { Status = StatusCode.NotFound };

            codeEntity.Status = (int)StatusCode.Expired;
            var mergeResult = await table.MergeAsync(codeEntity);

            if (mergeResult >= 400) throw new HttpStatusException("EntityErrorUpdate:DirectLoginCode", mergeResult);

            return await CreateUserTokenAsync(identity, codeEntity.TargetUser, UserVerificationLevel.StrongSignIn);
        }
        #endregion


        public async Task<IUserAuthenticationToken> RefreshTokenAsync(ISessionIdentity identity, IRefreshUserAuthenticationTokenRequest request)
        {
            if (identity == null || !identity.IsAuthenticated) throw new HttpStatusException("Unauthorized:Identity") { Status = StatusCode.Unauthorized };
            if (identity.IdentityType != IdentityType.App) throw new HttpStatusException($"NotApp:Identity({identity.IdentityType})") { Status = StatusCode.Unauthorized };
            
            var token = request.RefreshToken;
            if (string.IsNullOrEmpty(token) || token.Length != 32) throw new HttpStatusException($"BadRequest:RefreshUserAuthenticationTokenRequest.RefreshToken({token})") { Status = StatusCode.BadRequest };

            var partitionKey = token.Substring(0, 8);

            var now = DateTime.UtcNow;
            var nowTicks = now.Ticks;

            var table = tableClient.GetTableReference(RefreshTokenTableName);

            var tokenResult = await table.RetrieveAsync<RefreshTokenEntity>(partitionKey, token);

            if (tokenResult.HttpStatusCode == 404) throw new HttpStatusException("EntityNotFound:RefreshToken", tokenResult.HttpStatusCode) { Status = StatusCode.NotFound };
            if (tokenResult.HttpStatusCode >= 400) throw new HttpStatusException("EntityErrorRetrieve:RefreshToken", tokenResult.HttpStatusCode);

            var tokenEntity = tokenResult.Entity;

            if (tokenEntity.ClaimTime != 0L) throw new HttpStatusException($"EntityErrorStatus:RefreshToken(ClaimTime={tokenEntity.ClaimTime})") { Status = StatusCode.NotFound };
            if (tokenEntity.ExpireTime < nowTicks) throw new HttpStatusException("Expired:RefreshToken") { Status = StatusCode.NotFound };

            tokenEntity.ClaimTime = nowTicks;
            var mergeResult = await table.MergeAsync(tokenEntity);

            if (mergeResult >= 400) throw new HttpStatusException("EntityErrorUpdate:RefreshToken", mergeResult);

            
            return await CreateUserTokenAsync(identity, tokenEntity.TargetUser, identity.ClientSessionId == tokenEntity.SessionId ? UserVerificationLevel.StrongSignIn : UserVerificationLevel.AutoSignIn);
        }

        public async Task<ISessionIdentity> ValidateTokenAsync(ISession session, string scheme, string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            switch (scheme)
            {
                case "Bearer":
                    return ValidateTokenBearer(session, token);

                default:
                    throw new HttpStatusException($"Invalid:Scheme{scheme}") { Status = StatusCode.Unauthorized };
            }
        }

        private ISessionIdentity ValidateTokenBearer(ISession session, string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3) throw new HttpStatusException($"Invalid:Token(Token={token})") { Status = StatusCode.Unauthorized };

            var identityType = IdentityType.Unknown;
            switch (parts[0])
            {
                case "App":
                    identityType = IdentityType.App;
                    break;
                case "Usr":
                    identityType = IdentityType.User;
                    break;
                case "Bot":
                    identityType = IdentityType.Bot;
                    break;
                default:
                    throw new HttpStatusException($"Invalid:Token.IdentityType(Token={token})") { Status = StatusCode.Unauthorized };
            }

            var encryptedToken = Convert.FromBase64String(parts[1]);
            var decryptedToken = default(byte[]);
            try
            {
                decryptedToken = Aes.Decrypt(encryptedToken, encryptionKey);
            }
            catch (Exception e)
            {
                throw new HttpStatusException($"UnableDecrypt:Token(Token={token})", e) { Status = StatusCode.Unauthorized };
            }
            
            var hash = Sha256.Hash(decryptedToken);
            var sign = Convert.FromBase64String(parts[2]);

            if (!Rsa.VerifySha256Hash(hash, sign, jwk.N, jwk.E)) throw new HttpStatusException($"ValidateFail:Signature(Token={token})") { Status = StatusCode.Unauthorized };

            var tokenString = Encoding.UTF8.GetString(decryptedToken);

            ISessionIdentity identity = default(AnonymousIdentity);
            switch (identityType)
            {
                case IdentityType.App:
                    identity = Newtonsoft.Json.JsonConvert.DeserializeObject<AppIdentity>(tokenString);
                    break;
                case IdentityType.User:
                    identity = Newtonsoft.Json.JsonConvert.DeserializeObject<UserIdentity>(tokenString);
                    break;
                case IdentityType.Bot:
                    //identity = Newtonsoft.Json.JsonConvert.DeserializeObject<BotI>
                    break;
                default:
                    throw new HttpStatusException($"Invalid:Token.IdentityType(Token={token})") { Status = StatusCode.Unauthorized };
            }

            //if (identity == null) throw new HttpStatusException("Null:Identity") { Status = StatusCode.Unauthorized };

            if (identity.ClientDeviceId != session.ClientDeviceId || identity.ClientSessionId != session.ClientSessionId)
                throw new HttpStatusException(
                    $"NotMatch:Session(IdentityDeviceId={identity.ClientDeviceId}, CurrentDeviceId={session.ClientDeviceId}, IdentitySessionId={identity.ClientSessionId}, CurrentSessionId={session.ClientSessionId})")
                    { Status = StatusCode.Unauthorized };

            if (identity.ExpireIn < DateTime.UtcNow.Ticks) throw new HttpStatusException("Expired:Identity") { Status = StatusCode.Unauthorized };

            return identity;
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

        private async Task<IUserAuthenticationToken> CreateUserTokenAsync(ISession session, string name, UserVerificationLevel level)
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
                ExpireIn = now.AddMinutes(60).Ticks,
                VerificationLevel = level
            };

            var accessToken = await CreateAccessToken(user);

            var table = tableClient.GetTableReference(RefreshTokenTableName);

            var refreshToken = Guid.NewGuid().ToString("N");
            var refreshTokenExpire = now.AddDays(90).Ticks;

            var insertResult = await table.InsertAsync(
                    new RefreshTokenEntity
                    {
                        CreateTime = nowTicks,
                        DeviceId = user.ClientDeviceId,
                        ExpireTime = refreshTokenExpire,
                        PartitionKey = refreshToken.Substring(0, 8),
                        RowKey = refreshToken,
                        TargetUser = user.Name,
                        ClaimTime = 0L
                    });

            if (insertResult >= 400) throw new HttpStatusException("EntityErrorInsert:RefreshToken", insertResult);

            return new UserAuthenticationToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpireIn = now.AddMinutes(60).Ticks
            };
        }

        private async Task<string> CreateAccessToken(ISessionIdentity identity)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(identity));

            var encryptedToken = Aes.Encrypt(tokenBytes, encryptionKey);
            var hash = Sha256.Hash(tokenBytes);

            var tokenSignature = (await keyVaultClient.SignAsync(signKeyIdentifier, JsonWebKeySignatureAlgorithm.RS256, hash)).Result;

            return identity.IdentityType.ToString()
                + "." + Convert.ToBase64String(encryptedToken)
                + "." + Convert.ToBase64String(tokenSignature);
        }

        public async Task InitializeAsync()
        {
            if (initialized) return;

            await semaphoreForInitialize.WaitAsync();

            if (initialized) return;

            try
            {
                jwk = (await keyVaultClient.GetKeyAsync(signKeyIdentifier)).Key;
                encryptionKey = Convert.FromBase64String((await keyVaultClient.GetSecretAsync(encryptionKeyIdentifier)).Value);
                rnd = new Random();
                tableClient = CloudStorageAccount.Parse((await keyVaultClient.GetSecretAsync(tableIdentifier)).Value).CreateCloudTableClient();

                await tableClient.GetTableReference(AcknowledgeCodeTableName).CreateIfNotExistsAsync();
                await tableClient.GetTableReference(DirectLoginCodeTableName).CreateIfNotExistsAsync();
                await tableClient.GetTableReference(RefreshTokenTableName).CreateIfNotExistsAsync();

                initialized = true;
            }
            finally
            {
                semaphoreForInitialize.Release();
            }
        }
    }
}
