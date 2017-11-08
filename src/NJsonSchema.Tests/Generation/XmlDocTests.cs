using System.Linq;
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
            ///     * Users ...
            ///     * Users ...
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
            Assert.IsTrue(summary.Contains("    * Users"));
            Assert.AreEqual(summary.Trim(), summary);
        }

        public class WithTagsInXmlDoc
        {
            /// <summary>Gets or sets the foo.</summary>
            /// <response code="201">Account created</response>
            /// <response code="400">Username already in use</response>
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_xml_doc_contains_xml_then_it_is_fully_read()
        {
            //// Arrange


            //// Act
            var element = await typeof(WithTagsInXmlDoc).GetProperty("Foo").GetXmlDocumentationAsync();
            var responses = element.Elements("response");

            //// Assert
            Assert.AreEqual(2, responses.Count());

            Assert.AreEqual("Account created", responses.First().Value);
            Assert.AreEqual("201", responses.First().Attribute("code").Value);

            Assert.AreEqual("Username already in use", responses.Last().Value);
            Assert.AreEqual("400", responses.Last().Attribute("code").Value);
        }

        public class WithSeeTagInXmlDoc
        {
            /// <summary><see langword="null"/> for the default <see cref="Record"/>.</summary>
            public string Foo { get; set; }
        }

        [TestMethod]
        public async Task When_summary_has_see_tag_then_it_is_converted()
        {
            //// Arrange


            //// Act
            var summary = await typeof(WithSeeTagInXmlDoc).GetProperty("Foo").GetXmlSummaryAsync();

            //// Assert
            Assert.AreEqual("null for the default Record.", summary);
        }
    }
}
