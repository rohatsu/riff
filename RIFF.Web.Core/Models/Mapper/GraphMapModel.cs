// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;

namespace RIFF.Web.Core.Models.Mapper
{
    public class GraphMapModel
    {
        public Dictionary<string, string> Graphs { get; set; }
        public IEnumerable<string> GraphsList { get { return Graphs.Keys; } }
        public DateTime LicenseDate { get; set; }

        public string LicenseName { get; set; }
        public string MapCytoscapeCrossGraph { get; set; }
    }
}
