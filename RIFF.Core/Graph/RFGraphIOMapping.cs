// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFGraphIOMapping
    {
        [DataMember]
        public RFDateBehaviour DateBehaviour { get; set; }

        [DataMember]
        public RFCatalogKey Key { get; set; }

        [IgnoreDataMember]
        public PropertyInfo Property { get; set; }

        [DataMember]
        public string PropertyName { get { return Property.Name; } set { } }

        [IgnoreDataMember]
        public Func<RFGraphInstance, List<RFDate>> RangeRequestFunc { get; set; } // for a specific instruction instance, return all dates of inputs you require

        [IgnoreDataMember]
        public Func<RFGraphInstance, RFDate> RangeUpdateFunc { get; set; } // for a specific ranged input update, which process instance should we run?
    }
}
