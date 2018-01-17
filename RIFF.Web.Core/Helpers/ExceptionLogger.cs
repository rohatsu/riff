using RIFF.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.ExceptionHandling;

namespace RIFF.Web.Core.Helpers
{
    public class RFExceptionLogger : ExceptionLogger
    {
        public virtual void LogCore(ExceptionLoggerContext context)
        {
            RFStatic.Log.Info(this, "WebAPI exception: {0}" + (context?.Exception?.Message ?? string.Empty));
        }
    }
}
