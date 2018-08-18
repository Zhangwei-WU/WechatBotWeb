using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WechatBotWeb.IData
{
    public interface ITenantAndUserService : IService
    {
        Task<ITenant> TryGetTenantAsync(string tenantId);
        Task<IUser> TryGetUserAsync(string userId);
        Task<IEnumerable<IUserRole>> GetUserRoles(string userId);
    }
}
