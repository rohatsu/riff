// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using log4net.Config;
using System;
using System.ServiceProcess;

namespace RIFF.Service
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                switch (args[0])
                {
                    case "/install":
                        {
                            try
                            {
                                System.Diagnostics.EventLog.CreateEventSource("RIFF", "Application");
                                Console.WriteLine("Created RIFF event log.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception: " + ex.Message);
                            }
                            return;
                        }
                    default:
                        Console.WriteLine("Unrecognized parameter, use /install");
                        return;
                }
            }
            else if (!Environment.UserInteractive)
            {
                ServiceBase.Run(new RFServiceHost());
            }
            else
            {
                Console.WriteLine("Starting local instance... press any key to exit.");
                XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net.config"));
                var service = new RFServiceHost();
                service.StartEnvironment();
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
    }
}
