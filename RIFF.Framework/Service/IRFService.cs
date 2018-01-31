// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace RIFF.Framework
{
    [DataContract]
    public class RFCatalogEntryDTO
    {
        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string Content { get; set; }

        public RFCatalogEntryDTO(RFCatalogEntry e)
        {
            if (e != null)
            {
                TypeName = e.GetType().FullName;
                Content = RFXMLSerializer.SerializeContract(e);
            }
        }

        public RFCatalogEntry Deserialize()
        {
            return RFXMLSerializer.DeserializeContract(TypeName, Content) as RFCatalogEntry;
        }
    }

    [DataContract]
    public class RFServiceStatus
    {
        [DataMember]
        public DateTime LastRequestTime { get; set; }

        [DataMember]
        public int NumThreads { get; set; }

        [DataMember]
        public long RequestsServed { get; set; }

        [DataMember]
        public bool Running { get; set; }

        [DataMember]
        public DateTime StartTime { get; set; }

        [DataMember]
        public long WorkingSet { get; set; }
    }

    [ServiceContract]
    //[ServiceKnownType(typeof(RFProcessingTracker))]
    //[ServiceKnownType("GetKnownTypes", typeof(RFXMLSerializer))]
    public interface IRFService
    {
        [OperationContract]
        RFProcessingTracker GetProcessStatus(RFProcessingTrackerHandle trackerHandle);

        [OperationContract]
        RFProcessingTrackerHandle RetryError(string dispatchKey, RFUserLogEntry userLogEntry);

        [OperationContract]
        RFProcessingTrackerHandle RunProcess(bool isGraph, string processName, RFGraphInstance instance, RFUserLogEntry userLogEntry);

        [OperationContract]
        RFServiceStatus Status();

        [OperationContract]
        RFProcessingTrackerHandle SubmitAndProcess(IEnumerable<RFCatalogEntryDTO> inputs, RFUserLogEntry userLogEntry);

        [OperationContract]
        void ServiceCommand(string serviceName, string command, string param);
    }
}
