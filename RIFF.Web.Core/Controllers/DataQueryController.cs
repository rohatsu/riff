// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Framework;
using RIFF.Web.Core.Helpers;
using DevExtreme.AspNet.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ModelBinding;

namespace RIFF.Web.Core.Controllers
{
    [RFControllerAuthorize(AccessLevel = RFAccessLevel.NotSet, Permission = null)]
    public class DataQueryController : RIFFApiController
    {
        public DataQueryController(IRFProcessingContext context, RFEngineDefinition engineConfig) : base(context, engineConfig)
        {
        }

        [HttpGet]
        public HttpResponseMessage Get(DataSourceLoadOptions loadOptions)
        {
            var result = new List<object>();
            /*loadOptions.PrimaryKey = new[] { "OrderID" };

            var orders = from o in _db.Orders
                         select new
                         {
                             o.OrderID,
                             o.CustomerID,
                             CustomerName = o.Customer.ContactName,
                             o.EmployeeID,
                             EmployeeName = o.Employee.FirstName + " " + o.Employee.LastName,
                             o.OrderDate,
                             o.RequiredDate,
                             o.ShippedDate,
                             o.ShipVia,
                             ShipViaName = o.Shipper.CompanyName,
                             o.Freight,
                             o.ShipName,
                             o.ShipAddress,
                             o.ShipCity,
                             o.ShipRegion,
                             o.ShipPostalCode,
                             o.ShipCountry
                         };*/

            return Request.CreateResponse(DataSourceLoader.Load(result, loadOptions));
        }
    }
}
