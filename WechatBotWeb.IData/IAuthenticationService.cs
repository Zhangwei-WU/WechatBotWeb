namespace WechatBotWeb.IData
{
    using System.Threading.Tasks;
    using WechatBotWeb.Common;

    public interface IAuthenticationService : IService
    {
        Task<ISessionIdentity> ValidateTokenAsync(ISession session, string scheme, string token);
    }

    public interface IAppAuthenticationService : IAuthenticationService
    {
        Task<IAppAuthenticationToken> CreateAppTokenAsync(ISession session);
    }

    public interface IUserAuthenticationService : IAuthenticationService
    {
        /// <summary>
        /// random create a authentication code, only accept targetUser if targetUser is not null
        /// </summary>
        /// <param name="targetUser">target user</param>
        /// <returns>code data</returns>
        Task<IUserAuthenticationCode> CreateCodeAsync(ISessionIdentity identity, ICreateUserAuthenticationCodeRequest request);
        /// <summary>
        /// user acknowledge code
        /// </summary>
        /// <param name="code">authentication code</param>
        /// <param name="acknowledgeUser">acknowledge user</param>
        /// <returns>token status</returns>
        Task<IUserAuthenticationCode> AcknowledgeCodeAsync(ISessionIdentity identity, IAcknowledgeUserAuthenticationCodeRequest request);
        /// <summary>
        /// query token status and get access/refresh token once confirmed
        /// </summary>
        /// <param name="code">authentication code returned by CreateAuthenticationCodeAsync</param>
        /// <returns>authentication result</returns>
        Task<IUserAuthenticationToken> TryGetTokenByCodeAsync(ISessionIdentity identity, IGetUserAuthenticationCodeRequest request);
        /// <summary>
        /// refresh token
        /// </summary>
        /// <param name="refreshToken">RefreshToken in UserAuthenticationResult</param>
        /// <returns>authentication result</returns>
        Task<IUserAuthenticationToken> RefreshTokenAsync(ISessionIdentity identity, IRefreshUserAuthenticationTokenRequest request);
        //Task<IUserAuthenticationToken> CreateUserTokenAsync(IUserIdentity user);
    }
}
