using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Generation
{
    [TestClass]
    public class XmlDocTests
    {
        public class WithComplexXmlDoc
        {
            /// <summary>
            /// Query and manages users.
            /// 
            /// Please note:
            /// * Users ...
            /// * Users ...
            /// * Users ...
            /// * Users ...
            ///
            /// You need one of the following role: Owner, Editor, use XYZ to manage permissions.
            /// </summary>
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
        {
            //// Arrange


            //// Act
            var summary = await typeof(WithComplexXmlDoc).GetProperty("Foo").GetXmlSummaryAsync();

            //// Assert
            Assert.IsTrue(summary.Contains("\n\n"));
        }

        //public class WithTagsInXmlDoc
        //{
        //    /// <summary>Gets or sets the foo.</summary>
        //    /// <response code="201">Account created</response>
        //    /// <response code="400">Username already in use</response>
        //    public string Foo { get; set; }
        //}

        //[TestMethod]
        //public async Task When_xml_doc_contains_xml_then_it_is_fully_read()
        //{
        //    //// Arrange


        //    //// Act
        //    var fullDoc = await typeof(WithTagsInXmlDoc).GetProperty("Foo").GetXmlDocumentationAsync("");

        //    //// Assert
        //    Assert.IsTrue(fullDoc.Contains("\n\n"));
        //}
    }
}
