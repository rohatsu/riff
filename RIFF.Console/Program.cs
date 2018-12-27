// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using log4net.Config;
using RIFF.Core;
using RIFF.Framework;
using System;
using System.Linq;
using System.Text;

namespace RIFF
{
    public static class ConsoleApp
    {
        public static void Start(string[] args)
        {
            log4net.GlobalContext.Properties["LogName"] = "Console";
            XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net.config"));
            var engine = RIFFSection.GetDefaultEngine();
            var config = engine.BuildEngineConfiguration();

            var environment = RFEnvironments.StartConsole(engine.Environment, config, engine.Database, new string[] { engine.Assembly });
            var context = environment.Start();

            var engineConsole = config.Console;
            if (engineConsole != null)
            {
                engineConsole.Initialize(context, config, engine.Database);
            }

            var executor = new RFConsoleExecutor(config, context, engine, engineConsole);
            Console.WriteLine(">>> Loaded engine {0} from {1} in environment {2}", engine?.EngineName, engine?.Assembly, engine.Environment);

            if (args.Length > 0)
            {
                // batch mode
                executor.ExecuteCommand(String.Join(" ", args));
            }
            else
            {
                // interactive mode
                do
                {
                    try
                    {
                        System.Console.Write("> ");
                        var input = ReadLine();
                        executor.ExecuteCommand(input);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EXCEPTION: {0}", ex.Message);
                    }
                } while (!executor._isExiting);
            }
            environment.Stop();
        }

        private static void Main(string[] args)
        {
            Start(args);
        }

        private static string ReadLine()
        {
            var inputStream = Console.OpenStandardInput(8192);
            var bytes = new byte[8192];
            var outputLength = inputStream.Read(bytes, 0, 8192);
            //Console.WriteLine(outputLength);
            var chars = Encoding.UTF7.GetChars(bytes, 0, outputLength);
            return new string(chars);
        }
    }
}
