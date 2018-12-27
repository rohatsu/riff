// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using log4net.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RIFF.Core;
using RIFF.Framework;
using RIFF.Framework.Preferences;
using RIFF.Web.Core.Config;
using RIFF.Web.Core.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Mvc;

[assembly: WebActivatorEx.PreApplicationStartMethodAttribute(typeof(RIFF.Web.Core.App_Start.RIFFStart), "Start")]

namespace RIFF.Web.Core.App_Start
{
    public static class RIFFStart
    {
        public static IRFWebConfig Config { get; private set; }

        public static IContainer Container { get; private set; }

        public static RFConsoleExecutor ConsoleExecutor { get; private set; }

        public static IRFProcessingContext Context { get; private set; }
        public static IRFUserRole UserRole { get; private set; }

        public static EngineConfigElement EngineConfig { get; private set; }

        public static void CheckMenuItemPermission(RFMenuItem item, List<RFUserPermission> permissions)
        {
            var area = item.Area;
            var controller = item.Controller;

            if (string.IsNullOrWhiteSpace(controller) && !string.IsNullOrWhiteSpace(area))
            {
                item.Disabled = item.Disabled || !permissions.Any(p => p.Area == item.Area);
            }
            else if (!string.IsNullOrWhiteSpace(controller) && !string.IsNullOrWhiteSpace(area))
            {
                item.Disabled = item.Disabled || !permissions.Any(p => p.Area == item.Area && p.Controller == item.Controller);
            }
            if (item.SubMenu != null)
            {
                foreach (var child in item.SubMenu)
                {
                    CheckMenuItemPermission(child, permissions);
                }
            }
        }

        public static RFMenu GetMenu(string username, bool presentationMode)
        {
            var permissions = UserRole.GetPermissions(username, RFAccessLevel.Read.ToString());

            // user menus + system menus
            var menu = Config.GetMenu(UserRole, username, presentationMode);

            var administrationMenu = new RFMenuItem
            {
                Text = "Administration",
                Action = null,
                Area = null,
                Disabled = UserRole.RequiresPermission(username) && !UserRole.HasPermission(username, "core", typeof(RIFF.Web.Core.Controllers.SystemController).Name, "Admin"), // special permission
                Controller = null,
                SubMenu = new List<RFMenuItem> {
                            new RFMenuItem {
                                        Text = "Processes",
                                        SubMenu = null,
                                        Action = "Processes",
                                        Area = null,
                                        Controller = "System",
                            },
                            new RFMenuItem {
                                        Text = "Input Files",
                                        SubMenu = null,
                                        Action = "InputFiles",
                                        Area = null,
                                        Controller = "System",
                            },
                            new RFMenuItem {
                                        Text = "Data Editor",
                                        SubMenu = null,
                                        Action = "DataEditor",
                                        Area = null,
                                        Controller = "System",
                            },
                            new RFMenuItem {
                                        Text = "Console",
                                        SubMenu = null,
                                        Action = "Console",
                                        Area = null,
                                        Controller = "System",
                            },
                            new RFMenuItem {
                                        Text = "System Log",
                                        SubMenu = null,
                                        Action = "Logs",
                                        Area = null,
                                        Controller = "System",
                            },
                            new RFMenuItem {
                                        Text = "Maintenance",
                                        SubMenu = null,
                                        Action = "Maintenance",
                                        Area = null,
                                        Controller = "System",
                            }
                        }
            };

            var systemMenu = new RFMenuItem
            {
                Text = "System",
                Action = null,
                Area = "Core",
                Controller = "System",
                Icon = "preferences",
                SubMenu = new List<RFMenuItem> {
                                    new RFMenuItem {
                                        Text = "Configuration",
                                        Action = "Config",
                                        Area = null,
                                        Controller = "System",
                                    },
                                    new RFMenuItem {
                                        Text = "Access Control",
                                        SubMenu = null,
                                        Action = "Index",
                                        Area = null,
                                        Controller = "Role",
                                    },
                    new RFMenuItem {
                                Text = "Activity Log",
                                SubMenu = null,
                                Action = "UserLog",
                                Area = null,
                                Controller = "System",
                    },
                    new RFMenuItem {
                                Text = "Error Queue",
                                SubMenu = null,
                                Action = "ErrorQueue",
                                Area = null,
                                Controller = "System",
                    },
                    new RFMenuItem {
                                Text = "Input Reports",
                                SubMenu = null,
                                Action = "InputReports",
                                Area = null,
                                Controller = "System",
                    },
                    new RFMenuItem {
                                Text = "Received Files",
                                SubMenu = null,
                                Action = "Index",
                                Area = null,
                                Controller = "Mirror",
                    },
                    new RFMenuItem {
                                Text = "Scheduled Tasks",
                                SubMenu = null,
                                Action = "Tasks",
                                Area = null,
                                Controller = "System",
                    },
                    new RFMenuItem {
                                Text = "Data Browser",
                                SubMenu = null,
                                Action = "Index",
                                Area = null,
                                Controller = "Data",
                    },
                            new RFMenuItem {
                                        Text = "Graph Browser",
                                        SubMenu = null,
                                        Action = "GraphMap",
                                        Area = null,
                                        Controller = "Mapper",
                            },
                    administrationMenu
                }
            };

            menu.Items.Add(systemMenu);

            // disable non-permissioned controllers
            if (UserRole.RequiresPermission(username))
            {
                foreach (var item in menu.Items)
                {
                    CheckMenuItemPermission(item, permissions);
                }
            }

            return menu;
        }

        public static void InitializeEngine()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(Assembly.GetExecutingAssembly());
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            var clientControllerAssembly = RFSettings.GetAppSetting("ControllersAssembly", null);
            if (!string.IsNullOrWhiteSpace(clientControllerAssembly))
            {
                builder.RegisterControllers(AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetModules().First().Name == clientControllerAssembly));
                builder.RegisterApiControllers(AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetModules().First().Name == clientControllerAssembly));
            }

            var engine = RIFFSection.GetDefaultEngine();
            var config = engine.BuildEngineConfiguration();
            EngineConfig = engine;

            var enableWebProcessing = RFSettings.GetAppSetting("EnableWebProcessing", false);
            var context =
                enableWebProcessing ?
                RFEnvironments.StartConsole(engine.Environment, config, engine.Database, new string[] { "RIFF.Core", "RIFF.Framework", engine.Assembly }).Start() :
                RFEnvironments.StartWeb(engine.Environment, engine.Database, new string[] { "RIFF.Core", "RIFF.Framework", engine.Assembly });
            UserRole = context.UserRole;

            // this context is for read-only operations
            builder.RegisterInstance(context).As<IRFProcessingContext>();
            builder.RegisterInstance(context).As<IRFSystemContext>();
            builder.RegisterInstance(config).As<RFEngineDefinition>();

            var engineConsole = config.Console;
            if (config.Console == null)
            {
                throw new Exception("Error initializing engine. Check log for details.");
            }
            engineConsole.Initialize(context, config, engine.Database);
            ConsoleExecutor = new RFConsoleExecutor(config, context, engine, engineConsole);

            Context = context;

            var httpConfig = GlobalConfiguration.Configuration;
            builder.RegisterWebApiFilterProvider(httpConfig);

            Container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(Container));
            httpConfig.DependencyResolver = new AutofacWebApiDependencyResolver(Container);
        }

        public static void InitializeJSON()
        {
            // remove default implementation
            ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>().FirstOrDefault());
            // add our custom one
            ValueProviderFactories.Factories.Add(new JsonNetValueProviderFactory());
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new RFDateJsonConverter(), new SimpleEnumConverter() }
            };
        }

        public static void InitializeLogging()
        {
            log4net.GlobalContext.Properties["LogName"] = "Web";
            XmlConfigurator.ConfigureAndWatch(new FileInfo(@"log4net.config"));
        }

        public static void InitializeMVC()
        {
            ModelBinders.Binders.Add(typeof(DateTime), new DateTimeBinder());
            ModelBinders.Binders.Add(typeof(DateTime?), new DateTimeBinder());
            ModelBinders.Binders.Add(typeof(RFDate), new SimpleDateBinder());
            ModelBinders.Binders.Add(typeof(RFDate?), new SimpleDateBinder());
        }

        public static bool IsPresentationMode(string Username)
        {
            return UserPreferences.IsPresentationMode(Context, Username);
        }

        public static void SetConfig(IRFWebConfig config)
        {
            Config = config;
        }

        public static void Start()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.RelativeSearchPath;

            InitializeLogging();
            InitializeEngine();
            InitializeJSON();
            InitializeMVC();
        }
    }
}
