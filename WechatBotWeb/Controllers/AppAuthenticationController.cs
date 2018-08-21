
namespace WechatBotWeb.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;

    [ApiController]
    [Route("api/appauth")]
    public class AppAuthenticationController : Controller
    {
        private IAppAuthenticationService appAuthService;
        public AppAuthenticationController(IAppAuthenticationService service)
        {
            appAuthService = service;
        }

        [HttpGet("")]
        public async Task<IActionResult> GenerateAuthenticationCodeAsync()
        {
            return Ok(await appAuthService.CreateAppTokenAsync(CallContext.ClientContext));
        }
    }
}
