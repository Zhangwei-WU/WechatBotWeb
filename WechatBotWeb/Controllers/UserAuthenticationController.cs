namespace WechatBotWeb.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;
    using WechatBotWeb.Models;

    [ApiController]
    [Route("api/userauth")]
    public class UserAuthenticationController : Controller
    {
        private IUserAuthenticationService userAuthService;
        public UserAuthenticationController(IUserAuthenticationService service)
        {
            userAuthService = service;
        }

        [HttpPost("create/acknowledge")]
        [Authorize()] // appidentity
        public async Task<IActionResult> CreateAcknowledgeCodeAsync([FromBody]CreateUserAuthenticationCodeRequest request)
        {
            return Ok(await userAuthService.CreateCodeAsync(User.Identity as ISessionIdentity, request));
        }

        [HttpPost("create/directlogin")]
        [Authorize()] // botidentity
        public async Task<IActionResult> CreateDirectLoginCodeAsync([FromBody]CreateUserAuthenticationCodeRequest request)
        {
            return Ok(await userAuthService.CreateCodeAsync(User.Identity as ISessionIdentity, request));
        }

        [HttpPost("ack/acknowledge")]
        [Authorize()] // botidentity
        public async Task<IActionResult> AcknowledgeAcknowledgeCodeAsync([FromBody]AcknowledgeUserAuthenticationCodeRequest request)
        {
            if (request.CodeType != UserAuthenticationCodeType.AcknowledgeCode) throw new HttpStatusException($"NotMatch:AcknowledgeUserAuthenticationCodeRequest.CodeType({request.CodeType})") { Status = Common.StatusCode.BadRequest };
            return Ok(await userAuthService.AcknowledgeCodeAsync(User.Identity as ISessionIdentity, request));
        }

        [HttpPost("ack/directlogin")]
        [Authorize()] // appidentity
        public async Task<IActionResult> AcknowledgeDirectLoginCodeAsync([FromBody]AcknowledgeUserAuthenticationCodeRequest request)
        {
            if (request.CodeType != UserAuthenticationCodeType.DirectLoginCode) throw new HttpStatusException($"NotMatch:AcknowledgeUserAuthenticationCodeRequest.CodeType({request.CodeType})") { Status = Common.StatusCode.BadRequest };
            return Ok(await userAuthService.AcknowledgeCodeAsync(User.Identity as ISessionIdentity, request));
        }

        [HttpPost("get")]
        [Authorize()] // appidentity
        public async Task<IActionResult> TryGetTokenByCodeAsync([FromBody]GetUserAuthenticationCodeRequest request)
        {
            return Ok(await userAuthService.TryGetTokenByCodeAsync(User.Identity as ISessionIdentity, request));
        }

        [HttpPost("refresh")]
        [Authorize()] // appidentity
        public async Task<IActionResult> RefreshTokenAsync([FromBody]RefreshUserAuthenticationTokenRequest request)
        {
            return Ok(await userAuthService.RefreshTokenAsync(User.Identity as ISessionIdentity, request));
        }
    }
}
