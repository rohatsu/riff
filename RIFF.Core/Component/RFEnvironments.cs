// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;
using System.Reflection;

namespace RIFF.Core
{
    public enum RFProcessingMode
    {
        RFContinuous = 1,
        RFSinglePass = 2
    };

    public static class RFEnvironments
    {
        /// <summary>
        /// Environment for a single request, underneath an optional parent environment.
        /// </summary>
        /// <param name="dbConnection">Database connection string.</param>
        /// <returns>Context for accessing the environment.</returns>
        public static IRFEnvironment StartConsole(string environment, RFEngineDefinition config, string dbConnection, IEnumerable<string> engineAssemblies = null)
        {
            LoadReferencedAssemblies();

            var se = new RFConsoleEnvironment(
                environment: environment,
                config: config,
                dbConnection: dbConnection
                );
            RFXMLSerializer.Initialize(engineAssemblies);
            RFRazor.Initialize(engineAssemblies);
            return se;
        }

        /// <summary>
        /// Start a simple one-process engine using default providers.
        /// </summary>
        /// <param name="config">Engine configuration.</param>
        /// <param name="dbConnection">Database connection string.</param>
        /// <returns>Context for accessing the environment.</returns>
        public static IRFEnvironment StartLocal(string environment, RFEngineDefinition config, string dbConnection, IEnumerable<string> engineAssemblies)
        {
            LoadReferencedAssemblies();

            var le = new RFServiceEnvironment(environment, config, dbConnection);
            RFXMLSerializer.Initialize(engineAssemblies);
            RFRazor.Initialize(engineAssemblies);
            return le;
        }

        /// <summary>
        /// Non-realtime context for Web processing.
        /// </summary>
        /// <param name="dbConnection">Database connection string.</param>
        /// <returns>Context for accessing the environment.</returns>
        public static IRFProcessingContext StartWeb(string environment, string dbConnection, IEnumerable<string> engineAssemblies)
        {
            LoadReferencedAssemblies();

            var se = new RFWebEnvironment(environment: environment, dbConnection: dbConnection);
            RFXMLSerializer.Initialize(engineAssemblies);
            RFRazor.Initialize(engineAssemblies);
            return se.Start();
        }

        /*
        /// <summary>
        /// Lightweight context for core processing (no engine)
        /// </summary>
        /// <param name="dbConnection">Database connection string.</param>
        /// <returns>Context for accessing the environment.</returns>
        public static IRFProcessingContext StartCore(string environment, string dbConnection, Dictionary<string, string> settings = null)
        {
            var se = new RFCoreEnvironment(environment: environment, dbConnection: dbConnection);
            if (settings != null)
            {
                RFSettings.Load(settings);
            }
            return se.Start();
        }*/

        internal static void LogLicenseInfo(RFEngineDefinition config)
        {
            var license = RIFF.Core.RFPublicRSA.GetHost(config.LicenseTokens.Key, config.LicenseTokens.Value);
            RFStatic.Log.Info(typeof(RFCore), ">>> RIFF Framework {0} | (c) rohatsu software studios limited | www.rohatsu.com", RFCore.sVersion);
            RFStatic.Log.Info(typeof(RFCore), ">>> Licensed to '{0}' ({1})", license.Key, license.Value.ToString(RFCore.sDateFormat));
            RFStatic.Log.Info(typeof(RFCore), ">>> Loaded engine {0} from {1} in environment {2}", config?.EngineName, config?.Assembly, config?.Environment);
        }

        private static void LoadReferencedAssemblies()
        {
            var parentAssembly = Assembly.GetEntryAssembly();
            if (parentAssembly == null)
            {
                parentAssembly = Assembly.GetCallingAssembly();
            }
            foreach (var assembly in parentAssembly.GetReferencedAssemblies())
            {
                Assembly.Load(assembly);
            }
        }
    }
}
