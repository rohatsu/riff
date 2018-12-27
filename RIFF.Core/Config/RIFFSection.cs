// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Configuration;

namespace RIFF.Core
{
    public class EngineCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override string ElementName
        {
            get { return "engine"; }
        }

        public EngineCollection()
        {
            var details = new EngineConfigElement();
            if (details.EngineName != "")
            {
                BaseAdd(details, false);
            }
        }

        public EngineConfigElement this[int index]
        {
            get
            {
                return (EngineConfigElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public EngineConfigElement this[string name]
        {
            get
            {
                return (EngineConfigElement)BaseGet(name);
            }
        }

        public void Add(EngineConfigElement details)
        {
            BaseAdd(details);
        }

        public void Clear()
        {
            BaseClear();
        }

        public int IndexOf(EngineConfigElement details)
        {
            return BaseIndexOf(details);
        }

        public void Remove(EngineConfigElement details)
        {
            if (BaseIndexOf(details) >= 0)
                BaseRemove(details.EngineName);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new EngineConfigElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((EngineConfigElement)element).EngineName;
        }
    }

    public class EngineConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("assembly", IsRequired = true)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string Assembly
        {
            get { return (string)this["assembly"]; }
            set { this["assembly"] = value; }
        }

        [ConfigurationProperty("class", IsRequired = true)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string Class
        {
            get { return (string)this["class"]; }
            set { this["class"] = value; }
        }

        [ConfigurationProperty("database", IsRequired = true)]
        public string Database
        {
            get { return (string)this["database"]; }
            set { this["database"] = value; }
        }

        [ConfigurationProperty("enginename", IsRequired = true, IsKey = true)]
        public string EngineName
        {
            get { return (string)this["enginename"]; }
            set { this["enginename"] = value; }
        }

        [ConfigurationProperty("environment", IsRequired = true)]
        public string Environment
        {
            get { return (string)this["environment"]; }
            set { this["environment"] = value; }
        }

        public KeyValuePair<string, string> LicenseTokens
        {
            get; set;
        }

        public RFEngineDefinition BuildEngineConfiguration()
        {
            RFEngineDefinition engine = null;
            if (Assembly.NotBlank())
            {
                var engineAssembly = System.Reflection.Assembly.LoadFrom(Assembly);
                var configurator = engineAssembly.CreateInstance(Class) as IRFEngineBuilder;
                if (RFStatic.Log == null)
                {
                    RFStatic.Log = new RFLog4NetLog(Database);
                }
                try
                {
                    engine = configurator.BuildEngine(Database, Environment);
                }
                catch (Exception ex)
                {
                    RFStatic.Log.Exception(this, "Fatal error initializing engine", ex);
                }
            }
            if(engine == null)
            {
                engine = RFEngineDefinition.Create("Empty", new RFSimpleKeyDomain("null"));
            }
            engine.LicenseTokens = LicenseTokens;
            engine.Assembly = Assembly;
            engine.Environment = Environment;
            return engine;
        }
    }

    public class LicenseConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("token1", IsRequired = true, IsKey = true)]
        public string Token1
        {
            get { return (string)this["token1"]; }
            set { this["token1"] = value; }
        }

        [ConfigurationProperty("token2", IsRequired = true, IsKey = true)]
        public string Token2
        {
            get { return (string)this["token2"]; }
            set { this["token2"] = value; }
        }
    }

    public class RIFFSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public EngineCollection Engines
        {
            get
            {
                var engineCollection = (EngineCollection)base[""];
                return engineCollection;
            }
        }

        [ConfigurationProperty("license")]
        public LicenseConfigElement License
        {
            get
            {
                var license = (LicenseConfigElement)base["license"];
                return license;
            }
        }

        public static EngineConfigElement GetDefaultEngine()
        {
            var config = ConfigurationManager.GetSection("RIFFSection") as RIFFSection;
            if (config == null || config.Engines.Count == 0)
            {
                return new EngineConfigElement { EngineName = "Empty", Environment = "NULL", LicenseTokens = new KeyValuePair<string, string>(
                    "R2VuZXJhbCBQdWJsaWMgZm9yIHRlc3RpbmcgYW5kIGVkdWNhdGlvbmFsIHB1cnBvc2VzfDIwMTktMTItMzE=",
                    "KIZ7MARAAp7ey6w6//sr2npSCL+iH2AMjbeOvL1HYwjnNzjmFJutCUl6aVUG4zqXyfVyttOvk7QOzLWKnS4O5jpIk865KgcfPKex03H4QRz1IJJiweT0FhfWyqilv4ub6xrFQWYfzCNkY1LA6FXFeBgVo1l+onPkZNOwKTEZt8g=") };
            }
            if (config == null || config.Engines.Count != 1)
            {
                throw new ConfigurationErrorsException(String.Format("Expecting 1 engine in config, found {0}", config?.Engines?.Count ?? 0));
            }
            else
            {
                RFPublicRSA.GetHost(config.License.Token1, config.License.Token2);
                (config.Engines[0] as EngineConfigElement).LicenseTokens = new KeyValuePair<string, string>(config.License.Token1, config.License.Token2);
            }
            return config.Engines[0] as EngineConfigElement;
        }
    }
}
