// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace RIFF.Interfaces.Formats
{
    public interface IRFFormatLoader
    {
        List<DataTable> Load(MemoryStream data);
    }
}
