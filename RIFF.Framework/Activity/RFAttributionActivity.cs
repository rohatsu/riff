// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;

namespace RIFF.Framework
{
    public abstract class RFAttributionActivity<D, R, K> : RFActivity, IRFAttributionActivity
        where K : RFMappingKey
        where R : RFMappingDataRow<K>, new()
        where D : RFMappingDataSet<K, R>
    {
        public Func<RFDate, RFGraphInstance> mGetInstanceFunc { get; private set; }

        public Func<RFEnum, RFCatalogKey> mKeyCreatorFunc { get; private set; }

        public RFEnum mLatestEnum { get; private set; }

        public RFEnum mTemplateEnum { get; private set; }

        protected RFAttributionActivity(IRFProcessingContext context,
            Func<RFDate, RFGraphInstance> getInstanceFunc,
            Func<RFEnum, RFCatalogKey> keyCreatorFunc,
            RFEnum latestEnum,
            RFEnum templateEnum,
            string userName) : base(context, userName)
        {
            mGetInstanceFunc = getInstanceFunc;
            mKeyCreatorFunc = keyCreatorFunc;
            mLatestEnum = latestEnum;
            mTemplateEnum = templateEnum;
        }

        // copy over template to final
        public RFProcessingTrackerHandle ApplyTemplate(RFDate valueDate)
        {
            var entry = LoadTemplateEntry(valueDate);
            var latestKey = LatestKey(valueDate).CreateForInstance(entry.Key.GetInstance());
            return Context.SaveDocumentAsync(latestKey, entry.Content, CreateUserLogEntry("Apply", "Applied attribution changes.", valueDate));
        }

        public RFDate? GetLatestDate(RFDate valueDate)
        {
            var entry = LoadTemplateEntry(valueDate);
            return entry != null ? entry.Key.GraphInstance.ValueDate : null;
        }

        public D GetLatestEntry(RFDate valueDate)
        {
            var entry = LoadLatestEntry(valueDate);
            if (entry != null)
            {
                return entry.GetContent<D>();
            }
            return null;
        }

        public IRFDataSet GetTemplate(RFDate valueDate)
        {
            return GetDataSet(valueDate);
        }

        public bool Replace(RFDate valueDate, IRFMappingDataRow row)
        {
            return Amend(valueDate, row as R);
        }

        public bool RequiresApply(RFDate valueDate)
        {
            try
            {
                var template = LoadTemplateEntry(valueDate);
                var latest = LoadLatestEntry(valueDate);
                if (template != null && latest != null)
                {
                    return RFXMLSerializer.SerializeContract(template.Content) != RFXMLSerializer.SerializeContract(latest.Content);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(this, ex.Message);
            }
            return false;
        }

        protected bool Amend(RFDate valueDate, R row)
        {
            var entry = LoadTemplateEntry(valueDate);
            if (entry != null)
            {
                var dataSet = entry.GetContent<D>();
                if (dataSet != null)
                {
                    dataSet.Replace(row);
                    Context.SaveDocument(TemplateKey(valueDate), dataSet, true, null);
                    return true;
                }
            }
            else
            {
                throw new RFSystemException(this, "Unable to load attribution for date {0}", valueDate);
            }
            return false;
        }

        protected RFCatalogKey AttributionKey(RFDate valueDate, RFEnum key)
        {
            return mKeyCreatorFunc(key).CreateForInstance(mGetInstanceFunc(valueDate));
        }

        protected D GetDataSet(RFDate valueDate)
        {
            var entry = LoadTemplateEntry(valueDate);
            return entry != null ? entry.GetContent<D>() : null;
        }

        protected RFCatalogKey LatestKey(RFDate valueDate)
        {
            return AttributionKey(valueDate, mLatestEnum);
        }

        protected RFDocument LoadLatestEntry(RFDate valueDate)
        {
            return Context.LoadEntry(LatestKey(valueDate), new RFCatalogOptions
            {
                DateBehaviour = RFDateBehaviour.Latest
            }) as RFDocument;
        }

        protected RFDocument LoadTemplateEntry(RFDate valueDate)
        {
            return Context.LoadEntry(TemplateKey(valueDate), new RFCatalogOptions
            {
                DateBehaviour = RFDateBehaviour.Latest
            }) as RFDocument;
        }

        protected RFCatalogKey TemplateKey(RFDate valueDate)
        {
            return AttributionKey(valueDate, mTemplateEnum);
        }
    }

    public interface IRFAttributionActivity
    {
        RFProcessingTrackerHandle ApplyTemplate(RFDate valueDate);

        RFDate? GetLatestDate(RFDate valueDate);

        IRFDataSet GetTemplate(RFDate valueDate);

        bool Replace(RFDate valueDate, IRFMappingDataRow row);

        bool RequiresApply(RFDate valueDate);
    }
}
