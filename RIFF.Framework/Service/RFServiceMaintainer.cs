// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.ServiceProcess;

namespace RIFF.Framework
{
    public static class RFServiceMaintainer
    {
        public static readonly string SUCCESS = "Service Restarted";

        public static string Restart(string requestingUser)
        {
            var serviceName = RFSettings.GetAppSetting("RFMaintainers.ServiceName");
            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                var serviceTimeout = TimeSpan.FromSeconds(RFSettings.GetAppSetting("RFMaintainers.ServiceTimeoutSeconds", 60));

                RFStatic.Log.Warning(typeof(RFServiceMaintainer), "Service is being restarted on request from {0}", requestingUser);
                var service = new ServiceController(serviceName);
                try
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, serviceTimeout);

                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, serviceTimeout);

                    return SUCCESS;
                }
                catch (Exception ex)
                {
                    return ex.InnerException != null ? String.Format("{0} : {1}", ex.Message, ex.InnerException.Message) : ex.Message;
                }
            }
            else
            {
                return "Maintenance not configured";
            }
        }
    }
}
