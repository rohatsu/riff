// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    /// <summary>
    /// Sends an e-mail notification whenever its input key has been updated
    /// </summary>
    /// <typeparam name="D"></typeparam>
    public class RFEntryNotification<D> : RFGraphProcessorWithConfig<D, RFEntryNotificationConfig> where D : RFEntryNotificationDomain, new()
    {
        public RFEntryNotification(RFEntryNotificationConfig config) : base(config)
        {
        }

        public override void Process(D domain)
        {
            if (!(_config.OncePerDay && domain.State))
            {
                var email = new RFEntryNotificationEmail(_config.EmailConfig, _config.Message, _config.Url);
                email.Send(_config.Subject, domain.Instance.Name ?? "", domain.Instance.ValueDate.HasValue ? domain.Instance.ValueDate.Value.ToString("d MMM yyyy") : "n/a");
                domain.State = true;
            }
        }
    }

    [DataContract]
    public class RFEntryNotification1Domain : RFEntryNotificationDomain
    {
        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.Input, true)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public object Entry1 { get; set; }
    }

    [DataContract]
    public class RFEntryNotification2Domain : RFEntryNotificationDomain
    {
        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.Input, true)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public object Entry1 { get; set; }

        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.Input, true)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public object Entry2 { get; set; }
    }

    [DataContract]
    public class RFEntryNotificationConfig : IRFGraphProcessorConfig
    {
        [DataMember]
        public RFEmailConfig EmailConfig { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public bool OncePerDay { get; set; }

        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public string Url { get; set; }
    }

    [DataContract]
    public class RFEntryNotificationDomain : RFGraphProcessorDomain
    {
        [DataMember]
        [RFIOBehaviour(RFIOBehaviour.State, false)]
        [RFDateBehaviour(RFDateBehaviour.Exact)]
        public bool State { get; set; }
    }

    public class RFEntryNotificationEmail : RFEmail<EntryNotificationEmail>
    {
        public string Message { get; set; }

        public string Url { get; set; }

        public RFEntryNotificationEmail(RFEmailConfig config, string message, string url) : base(config)
        {
            Message = message;
            Url = url;
        }
    }
}
