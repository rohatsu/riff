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

namespace RIFF.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class EntryNotificationEmail : RazorEngine.Templating.TemplateBase<RIFF.Framework.RFEntryNotificationEmail>
    {
#line hidden

        public override void Execute()
        {


            
            #line 2 "..\..\Email\EntryNotificationEmail.cshtml"
  
    Layout = typeof(RIFF.Framework._EmailLayout).FullName;


            
            #line default
            #line hidden
WriteLiteral("<p style=\"font-family: Calibri; font-size: 11pt; color: #444;\">\r\n    ");


            
            #line 6 "..\..\Email\EntryNotificationEmail.cshtml"
Write(Model.Message);

            
            #line default
            #line hidden
WriteLiteral("\r\n</p>\r\n");


            
            #line 8 "..\..\Email\EntryNotificationEmail.cshtml"
 if (!string.IsNullOrWhiteSpace(Model.Url))
{

            
            #line default
            #line hidden
WriteLiteral("    <a style=\"font-family: Calibri; font-size: 11pt; color: #444;\" href=\"");


            
            #line 10 "..\..\Email\EntryNotificationEmail.cshtml"
                                                                    Write(Raw(Model.Url));

            
            #line default
            #line hidden
WriteLiteral("\">");


            
            #line 10 "..\..\Email\EntryNotificationEmail.cshtml"
                                                                                     Write(Model.Url);

            
            #line default
            #line hidden
WriteLiteral("</a>\r\n");


            
            #line 11 "..\..\Email\EntryNotificationEmail.cshtml"
}

            
            #line default
            #line hidden

        }
    }
}
#pragma warning restore 1591
