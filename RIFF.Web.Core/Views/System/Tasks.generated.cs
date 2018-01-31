﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Optimization;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using RIFF.Web.Core;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/System/Tasks.cshtml")]
    public partial class _Views_System_Tasks_cshtml : System.Web.Mvc.WebViewPage<dynamic>
    {
        public _Views_System_Tasks_cshtml()
        {
        }
        public override void Execute()
        {
            
            #line 1 "..\..\Views\System\Tasks.cshtml"
  
    ViewBag.Title = "Tasks";
    Layout = "~/Views/Core/_RIFFPage.cshtml";

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n<div");

WriteLiteral(" class=\"dx-fieldset\"");

WriteLiteral(" style=\"width: 300px;\"");

WriteLiteral(">\r\n    <div");

WriteLiteral(" class=\"dx-field\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"dx-field-label\"");

WriteLiteral(">Graph Date</div>\r\n        <div");

WriteLiteral(" class=\"dx-field-value\"");

WriteLiteral(" id=\"instancedate\"");

WriteLiteral("></div>\r\n    </div>\r\n    <div");

WriteLiteral(" class=\"dx-field\"");

WriteLiteral(">\r\n        <div");

WriteLiteral(" class=\"dx-field-label\"");

WriteLiteral(">Graph Instance</div>\r\n        <div");

WriteLiteral(" class=\"dx-field-value\"");

WriteLiteral(" id=\"instancename\"");

WriteLiteral("></div>\r\n    </div>\r\n</div>\r\n\r\n<div");

WriteLiteral(" id=\"gridContainer\"");

WriteLiteral(" style=\"width:1550px; margin: 0 auto; height: 750px;\"");

WriteLiteral("></div>\r\n");

WriteLiteral("\r\n");

DefineSection("RIFFPageScripts", () => {

WriteLiteral("\r\n    <script");

WriteLiteral(" type=\"text/javascript\"");

WriteLiteral(">\r\n\r\n        var initializeGrid = function (data) {\r\n            $(\"#gridContaine" +
"r\").dxDataGrid({\r\n                hoverStateEnabled: true,\r\n                data" +
"Source: data,\r\n\r\n                columns: [\r\n                    { dataField: \'G" +
"raphName\', caption: \'Graph\', width: \"80px\", dataType: \'string\', sortOrder: \'asc\'" +
", sortIndex: 1 },\r\n                    { dataField: \'TaskName\', caption: \'Name\'," +
" width: \"250px\", dataType: \'string\', sortOrder: \'asc\', sortIndex: 2 },\r\n        " +
"            { dataField: \'Description\', caption: \'Description\', width: \"350px\", " +
"dataType: \'string\' },\r\n\r\n                    { dataField: \'Schedule\', caption: \'" +
"Schedule / Trigger\', width: \"400px\"/*, alignment: \"center\"*/, dataType: \'string\'" +
" },\r\n                    //{ dataField: \'SchedulerRange\', caption: \'Range\', widt" +
"h: \"175px\", alignment: \"center\"  },\r\n                    //{ dataField: \'Trigger" +
"\', caption: \'Trigger\', width: \"100px\" },\r\n\r\n                    { dataField: \'Is" +
"Enabled\', caption: \'Enabled\', dataType: \'boolean\', width: \"80px\", visible: true " +
"},\r\n                    { dataField: \'IsSystem\', caption: \'Sys?\', dataType: \'boo" +
"lean\', width: \"80px\", visible: false, sortOrder: \'asc\', sortIndex: 0 },\r\n\r\n     " +
"               { dataField: \'Status\', caption: \'Status\', width: \"80px\", dataType" +
": \'string\', alignment: \"center\" },\r\n                    { dataField: \'LastRun\', " +
"caption: \'Last Run\', width: \"150px\", dataType: \'date\', format: \"dd/MM/yyyy HH:mm" +
":ss\", alignment: \"center\" },\r\n                    {\r\n                        // " +
"                       dataField: \'FullName\',\r\n                        caption: " +
"\'Execute\',\r\n                        width: \"85px\",\r\n                        alig" +
"nment: \"center\",\r\n                        cellTemplate: function (container, opt" +
"ions) {\r\n                            $(\'<a />\').addClass(\'dx-link\')\r\n           " +
"                     .text(\'Run\')\r\n                                .attr(\'href\'," +
" \'");

            
            #line 62 "..\..\Views\System\Tasks.cshtml"
                                          Write(Url.Action("RunProcess", "Process"));

            
            #line default
            #line hidden
WriteLiteral(@"?isGraph=' + options.data.IsGraph +
                                    '&processName=' + escape(options.data.FullName) +
                                    '&instanceDate=' + RIFFWebCore.Helpers.getYMD($('#instancedate').dxDateBox('instance').option(""value"")) +
                                    '&instanceName=' + escape($('#instancename').dxTextBox('instance').option(""value"")) +
                                    '&returnUrl=' + escape('");

            
            #line 66 "..\..\Views\System\Tasks.cshtml"
                                                       Write(Html.Raw(Url.Action("Tasks")));

            
            #line default
            #line hidden
WriteLiteral("\'))\r\n                                .appendTo(container);\r\n                     " +
"   }\r\n                    },\r\n                    { dataField: \'Message\', captio" +
"n: \'Message\', width: \"500px\", dataType: \'string\' },\r\n                    { dataF" +
"ield: \'LastDuration\', caption: \'[ms]\', width: \"75px\", format: \"fixedPoint\", data" +
"Type: \'number\' },\r\n                ],\r\n                columnChooser: { enabled:" +
" false },\r\n                allowColumnReordering: false,\r\n                sortin" +
"g: { mode: \'single\' },\r\n                onCellPrepared: function (e) {\r\n        " +
"            if (e !== undefined && e.rowType === \'data\' && e.column.caption === " +
"\'Status\') {\r\n                        if (e.data.Status === \'Finished\' || e.data." +
"Status === \'OK\')\r\n                            e.cellElement.addClass(\'greencell\'" +
");\r\n                        else if (e.data.Status === \'Started\' || e.data.Statu" +
"s === \'Ignored\')\r\n                            e.cellElement.addClass(\'yellowcell" +
"\');\r\n                        else if (e.data.Status === \'Error\')\r\n              " +
"              e.cellElement.addClass(\'redcell\');\r\n                    }\r\n       " +
"         },\r\n                groupPanel: { visible: false, emptyPanelText: \'Drag" +
" a column header here to group grid records\' },\r\n                columnAutoWidth" +
": false,\r\n                pager: { visible: false },\r\n                paging: { " +
"enabled: false },\r\n                headerFilter: { visible: true },\r\n           " +
"     showRowLines: true,\r\n                rowAlternationEnabled: false,\r\n       " +
"         //height: \"100%\",\r\n                // paging: { pageSize: 15 },\r\n      " +
"          scrolling: {\r\n                    mode: \'virtual\',\r\n                  " +
"  preloadEnabled: true\r\n                },\r\n                remoteOperations: fa" +
"lse,\r\n                loadPanel: {\r\n                    enabled: false\r\n        " +
"        },\r\n                filterRow: { visible: false },\r\n                sear" +
"chPanel: { visible: true },\r\n                selection: { mode: \'single\' }\r\n    " +
"        });\r\n        }\r\n\r\n        var refreshGrid = function()\r\n        {\r\n     " +
"       RIFFWebCore.RIFFPage.populateGrid(\"");

            
            #line 111 "..\..\Views\System\Tasks.cshtml"
                                          Write(Url.Action("GetTasks", "System"));

            
            #line default
            #line hidden
WriteLiteral(@""", {
                instanceName: $('#instancename').dxTextBox('instance').option(""value""),
                instanceDate: RIFFWebCore.Helpers.getYMD($('#instancedate').dxDateBox('instance').option(""value"")),
            }, initializeGrid);
        }

        var RFinitialize = function () {
           $('#instancedate').dxDateBox({
                min: null,
                max: new Date(),
                displayFormat: 'yyyy-MM-dd',
                value: new Date(");

            
            #line 122 "..\..\Views\System\Tasks.cshtml"
                            Write(RIFF.Core.RFDate.Today().Year);

            
            #line default
            #line hidden
WriteLiteral(", ");

            
            #line 122 "..\..\Views\System\Tasks.cshtml"
                                                              Write(RIFF.Core.RFDate.Today().Month);

            
            #line default
            #line hidden
WriteLiteral(" - 1, ");

            
            #line 122 "..\..\Views\System\Tasks.cshtml"
                                                                                                     Write(RIFF.Core.RFDate.Today().Day);

            
            #line default
            #line hidden
WriteLiteral("),\r\n                onValueChanged: function (e) { refreshGrid(); }\r\n            " +
"});\r\n            $(\'#instancename\').dxTextBox({\r\n                placeholder: \'G" +
"raph Instance Name\',\r\n                value: \'");

            
            #line 127 "..\..\Views\System\Tasks.cshtml"
                   Write(RIFF.Core.RFGraphInstance.DEFAULT_INSTANCE);

            
            #line default
            #line hidden
WriteLiteral("\'\r\n            });\r\n            refreshGrid();\r\n        };\r\n    </script>\r\n");

});

        }
    }
}
#pragma warning restore 1591
