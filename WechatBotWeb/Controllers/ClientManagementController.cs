namespace WechatBotWeb.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using WechatBotWeb.IData;

    [ApiController]
    [Route("api/client")]
    public class ClientManagementController : Controller
    {
        private IClientManagementService clientMgnService;

        public ClientManagementController(IClientManagementService service)
        {
            clientMgnService = service;
        }

        [HttpPut("device")]
        public async Task<IActionResult> PutDeviceInfo(IDeviceInfo device)
        {
            await clientMgnService.SaveDeviceInfo(device);
            return Ok();
        }

        [HttpPut("session")]
        public async Task<IActionResult> PutSessionInfo(ISessionInfo session)
        {
            await clientMgnService.UpdateSessionInfo(session);
            return Ok();
        }
    }
}
