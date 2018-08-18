namespace WechatBotWeb.IData
{
    using System.Threading.Tasks;
    using WechatBotWeb.Common;

    public interface IClientManagementService : IService
    {
        Task SaveDeviceInfo(IDeviceInfo device);
        Task<bool> IsDeviceExist(IDevice device);
        Task UpdateSessionInfo(ISessionInfo session);
        Task<bool> IsSessionExist(ISession session);
    }
}
