using System;
using System.Collections.Generic;
using System.Text;

namespace WechatBotWeb.Common
{
    public enum StatusCode
    {
        #region general section
        OK = 200,
        Created = 201,
        Accepted = 202,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        Gone = 410,
        InternalServerError = 500,
        #endregion

        #region authentication alias
        /// <summary>
        /// token is yet to be answered, same as General:OK
        /// </summary>
        Pending = 200,
        /// <summary>
        /// token is confirmed, same as General:Created
        /// </summary>
        Confirmed = 201,
        /// <summary>
        /// token is expired, same as General:Gone
        /// </summary>
        Expired = 410,
        #endregion
    }

    public interface IStatus
    {
        StatusCode Status { get; set; }

    }
}
