using System;
using System.Collections.Generic;
using System.Text;

namespace WechatBotWeb.IData
{
    public interface ITenant
    {
        string TenantId { get; set; }
    }

    public interface IUser
    {
        string UserId { get; set; }
    }

    public interface IUserRole
    {
        string TenantId { get; set; }
        string UserId { get; set; }
        IDictionary<string, string> Roles { get; set; }

    }
}
