// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Core
{
    public interface IRFDataRow
    {
    }

    public interface IRFDataSet
    {
        IEnumerable<IRFDataRow> GetRows();

        Type GetRowType();
    }
}
