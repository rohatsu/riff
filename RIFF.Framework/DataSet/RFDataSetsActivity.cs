// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Linq;

namespace RIFF.Framework
{
    public class RFDataSetsActivity : RFActivity
    {
        public RFDataSetsActivity(IRFProcessingContext context) : base(context, null)
        {
        }

        public IRFDataSet GetDataSet(long keyReference)
        {
            var document = GetDataSetDocument(keyReference);
            if (document != null)
            {
                return document.GetContent<IRFDataSet>();
            }
            return null;
        }

        public RFDocument GetDataSetDocument(long keyReference)
        {
            var inputFileKey = Context.GetKeysByType<RFDataSetKey>().FirstOrDefault(f => f.Key == keyReference).Value;
            if (inputFileKey != null)
            {
                var dataSetEntry = Context.LoadEntry(inputFileKey);
                return dataSetEntry as RFDocument;
            }
            return null;
        }
    }
}
