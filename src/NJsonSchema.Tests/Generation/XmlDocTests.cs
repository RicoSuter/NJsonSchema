using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using NJsonSchema.Infrastructure;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
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

        [Fact]
        public async Task When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
        {
            //// Arrange


            //// Act
            var summary = await typeof(WithComplexXmlDoc).GetProperty("Foo").GetXmlSummaryAsync();

            //// Assert
            Assert.Contains("\n\n", summary);
            Assert.Contains("    * Users", summary);
            Assert.Equal(summary.Trim(), summary);
        }

        public class WithTagsInXmlDoc
        {
            /// <summary>Gets or sets the foo.</summary>
            /// <response code="201">Account created</response>
            /// <response code="400">Username already in use</response>
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_xml_doc_contains_xml_then_it_is_fully_read()
        {
            //// Arrange


            //// Act
            var element = await typeof(WithTagsInXmlDoc).GetProperty("Foo").GetXmlDocumentationAsync();
            var responses = element.Elements("response");

            //// Assert
            Assert.Equal(2, responses.Count());

            Assert.Equal("Account created", responses.First().Value);
            Assert.Equal("201", responses.First().Attribute("code").Value);

            Assert.Equal("Username already in use", responses.Last().Value);
            Assert.Equal("400", responses.Last().Attribute("code").Value);
        }

        public class WithSeeTagInXmlDoc
        {
            /// <summary><see langword="null"/> for the default <see cref="Record"/>. See <see cref="Record">this</see> and <see href="https://github.com/rsuter/njsonschema">this</see> at <see href="https://github.com/rsuter/njsonschema"/>.</summary>
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_summary_has_see_tag_then_it_is_converted()
        {
            //// Arrange


            //// Act
            var summary = await typeof(WithSeeTagInXmlDoc).GetProperty("Foo").GetXmlSummaryAsync();

            //// Assert
            Assert.Equal("null for the default Record. See this and this at https://github.com/rsuter/njsonschema.", summary);
        }

        public class WithGenericTagsInXmlDoc
        {
            /// <summary>This <c>are</c> <strong>some</strong> tags.</summary>
            public string Foo { get; set; }
        }

        [Fact]
        public async Task When_summary_has_generic_tags_then_it_is_converted()
        {
            //// Arrange


            //// Act
            var summary = await typeof(WithGenericTagsInXmlDoc).GetProperty("Foo").GetXmlSummaryAsync();

            //// Assert
            Assert.Equal("This are some tags.", summary);
        }

        [Fact]
        public async Task When_xml_doc_is_missing_then_summary_is_missing()
        {
            //// Arrange


            //// Act
            var summary = await typeof(Point).GetXmlSummaryAsync();
            var summary2 = await typeof(Point).GetXmlSummaryAsync();

            //// Assert
            Assert.Empty(summary);
        }
    }
}
