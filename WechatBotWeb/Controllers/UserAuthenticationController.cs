namespace WechatBotWeb.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
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
            var result = await userAuthService.CreateCodeAsync(User.Identity as ISessionIdentity, request);
            return StatusCode((int)result.Status, result);
        }

        [HttpPost("create/directlogin")]
        [Authorize()] // botidentity
        public async Task<IActionResult> CreateDirectLoginCodeAsync([FromBody]CreateUserAuthenticationCodeRequest request)
        {
            var result = await userAuthService.CreateCodeAsync(User.Identity as ISessionIdentity, request);
            return StatusCode((int)result.Status, result);
        }

        [HttpPost("acknowledge")]
        [Authorize()] // appidentity
        public async Task<IActionResult> AcknowledgeCodeAsync([FromBody]AcknowledgeUserAuthenticationCodeRequest request)
        {
            var result = await userAuthService.AcknowledgeCodeAsync(User.Identity as ISessionIdentity, request);
            return StatusCode((int)result.Status, result);
        }

        [HttpPost("get")]
        [Authorize()] // appidentity
        public async Task<IActionResult> TryGetTokenByCodeAsync([FromBody]GetUserAuthenticationCodeRequest request)
        {
            var result = await userAuthService.TryGetTokenByCodeAsync(User.Identity as ISessionIdentity, request);
            return StatusCode((int)result.Status, result);
        }

        [HttpPost("refresh")]
        [Authorize()] // appidentity
        public async Task<IActionResult> RefreshTokenAsync([FromBody]RefreshUserAuthenticationTokenRequest request)
        {
            var result = await userAuthService.RefreshTokenAsync(User.Identity as ISessionIdentity, request);
            return StatusCode((int)result.Status, result);
        }
    }
}
