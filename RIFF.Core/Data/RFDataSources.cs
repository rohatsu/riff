using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RIFF.Core.Data
{
    [DataContract]
    public class ColumnDefinition
    {
        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Caption { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Format { get; set; }

        [DataMember]
        public bool CanGroup { get; set; }

        [DataMember]
        public string Aggregator { get; set; }

        [DataMember]
        public bool Mandatory { get; set; }
    }

    [DataContract]
    public class SourceDefinition
    {
        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string ConnectionCode { get; set; }

        [DataMember]
        public ColumnDefinition[] Columns { get; set; }
    }

    [DataContract]
    public class ConnectionDefinition
    {
        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string ConnectionString { get; set; }
    }

    [DataContract]
    public class RFDataSources
    {
        public static RFCatalogKey GetKey(RFKeyDomain domain)
        {
            return RFGenericCatalogKey.Create(domain, "RFDataSources", "Global", null);
        }

        [DataMember]
        public ConnectionDefinition[] Connections { get; set; }

        [DataMember]
        public SourceDefinition[] Sources { get; set; }
    }
}
