// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public abstract class RFDataRow : IRFDataRow
    {
        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    /// <summary>
    /// Base class for datasets. Please note order of rows is not preserved.
    /// </summary>
    [DataContract]
    public abstract class RFDataSet<T> : IRFDataSet where T : class, IRFDataRow
    {
        [DataMember]
        public RFEnum DataSetCode { get; set; }

        [DataMember]
        public List<T> Rows { get; set; }

        protected RFDataSet()
        {
            Rows = new List<T>();
        }

        public IEnumerable<IRFDataRow> GetRows()
        {
            return Rows;
        }

        public Type GetRowType()
        {
            return typeof(T);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            // resort rows by serialized representation - slow but ensures dataset equality
            Rows = Rows?.Select(r => new { Key = RFXMLSerializer.SerializeContract(r), Row = r }).OrderBy(r => r.Key).Select(r => r.Row).ToList();
        }
    }

    [DataContract]
    public class RFDataSetKey : RFCatalogKey
    {
        [DataMember]
        public RFEnum DataSetCode { get; set; }

        [DataMember]
        public string Path { get; set; }

        public static RFDataSetKey Create(RFKeyDomain domain, RFEnum dataSetCode, RFGraphInstance instance, params string[] path)
        {
            return domain.Associate(new RFDataSetKey
            {
                Plane = RFPlane.User,
                DataSetCode = dataSetCode,
                StoreType = RFStoreType.Document,
                Path = path != null ? String.Join("/", path) : null,
                GraphInstance = instance
            }) as RFDataSetKey;
        }

        public override string FriendlyString()
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                return DataSetCode;
            }
            else
            {
                return string.Format("{0}/{1}", Path ?? String.Empty, DataSetCode);
            }
        }
    }
}
