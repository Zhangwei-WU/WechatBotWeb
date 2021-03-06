﻿using System;
using System.Collections.Generic;

namespace WechatBotWeb.Common
{
    public class HttpStatusException : Exception, IStatus
    {
        public HttpStatusException(string message)
            : base(message)
        {
        }

        public HttpStatusException(string message, int hresult)
            : base(message)
        {
            HResult = hresult;
        }

        public HttpStatusException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public StatusCode Status { get; set; } = StatusCode.InternalServerError;

    }
}
