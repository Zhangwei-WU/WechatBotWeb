namespace WechatBotWeb.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;
    using WechatBotWeb.Models;

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
        public async Task<IActionResult> PutDeviceInfo([FromBody]DeviceInfo device)
        {
            device.ClientDeviceId = CallContext.ClientContext.ClientDeviceId;
            await clientMgnService.SaveDeviceInfo(device);
            return NoContent();
        }

        [HttpPut("session")]
        public async Task<IActionResult> PutSessionInfo([FromBody]SessionInfo session)
        {
            session.ClientDeviceId = CallContext.ClientContext.ClientDeviceId;
            session.ClientSessionId = CallContext.ClientContext.ClientSessionId;
            await clientMgnService.UpdateSessionInfo(session);
            return NoContent();
        }
    }
}
