// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace RIFF.Core
{
    [Obsolete]
    internal static class RFJSONSerializer
    {
        public static List<Type> PotentialTypes;

        public static object DeserializeContract(string type, string xml)
        {
            var serializer = new DataContractJsonSerializer(RFReflectionHelpers.GetTypeByFullName(type));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return serializer.ReadObject(ms);
        }

        public static void Initialize(SortedSet<string> assemblyNames)
        {
            PotentialTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => assemblyNames.Contains(a.GetName().Name)))
            {
                PotentialTypes.AddRange(assembly.GetTypes().Where(t => t.IsPublic && !t.IsInterface && !t.IsAbstract && t.GetCustomAttribute(typeof(DataContractAttribute)) != null));
            }
        }

        public static string SerializeContract(object o)
        {
            var serializer = new DataContractJsonSerializer(o.GetType(), PotentialTypes);
            using (var sw = new MemoryStream())
            {
                serializer.WriteObject(sw, o);
                return Encoding.UTF8.GetString(sw.ToArray());
            }
        }
    }
}
