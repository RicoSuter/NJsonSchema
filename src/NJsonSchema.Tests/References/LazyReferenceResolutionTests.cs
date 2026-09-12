using System.Reflection;

namespace NJsonSchema.Tests.References
{
#pragma warning disable SYSLIB0012

    public class LazyReferenceResolutionTests
    {
        [Fact]
        public async Task When_two_files_have_circular_refs_then_loading_does_not_hang()
        {
            // Arrange - CircularA refs CircularB, CircularB refs CircularA (#616)
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/CircularA.json";

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var schema = await JsonSchema.FromFileAsync(path, cts.Token);

            // Assert
            Assert.NotNull(schema);
            Assert.True(schema.Properties.ContainsKey("name"));
            Assert.True(schema.Properties.ContainsKey("b"));

            var bSchema = schema.Properties["b"].ActualTypeSchema;
            Assert.True(bSchema.Properties.ContainsKey("value"));
            Assert.True(bSchema.Properties.ContainsKey("a"));
        }

        [Fact]
        public async Task When_three_files_have_circular_refs_then_loading_does_not_hang()
        {
            // Arrange - A -> B -> C -> A (#616)
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/ThreeWayA.json";

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var schema = await JsonSchema.FromFileAsync(path, cts.Token);

            // Assert
            Assert.NotNull(schema);
            Assert.True(schema.Properties.ContainsKey("name"));
            Assert.True(schema.Properties.ContainsKey("b"));

            var bSchema = schema.Properties["b"].ActualTypeSchema;
            Assert.True(bSchema.Properties.ContainsKey("value"));
            Assert.True(bSchema.Properties.ContainsKey("c"));

            var cSchema = bSchema.Properties["c"].ActualTypeSchema;
            Assert.True(cSchema.Properties.ContainsKey("flag"));
            Assert.True(cSchema.Properties.ContainsKey("a"));
        }

        [Fact]
        public async Task When_root_has_fan_out_with_back_references_then_all_refs_resolve()
        {
            // Arrange - Root refs Child1/2/3, each refs back to root and siblings (#588)
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/FanOutRoot.json";

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var schema = await JsonSchema.FromFileAsync(path, cts.Token);

            // Assert
            Assert.NotNull(schema);
            Assert.True(schema.Properties.ContainsKey("child1"));
            Assert.True(schema.Properties.ContainsKey("child2"));
            Assert.True(schema.Properties.ContainsKey("child3"));

            var child1 = schema.Properties["child1"].ActualTypeSchema;
            Assert.True(child1.Properties.ContainsKey("data"));
            Assert.True(child1.Properties.ContainsKey("root"));
            Assert.True(child1.Properties.ContainsKey("sibling"));

            var child2 = schema.Properties["child2"].ActualTypeSchema;
            Assert.True(child2.Properties.ContainsKey("count"));

            var child3 = schema.Properties["child3"].ActualTypeSchema;
            Assert.True(child3.Properties.ContainsKey("active"));
        }

        [Fact]
        public async Task When_external_file_has_nested_sub_references_then_transitive_chain_resolves()
        {
            // Arrange - Root -> External -> SubExternal (#566)
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/TransitiveRoot.json";

            // Act
            var schema = await JsonSchema.FromFileAsync(path);

            // Assert
            Assert.NotNull(schema);
            var externalSchema = schema.Properties["external"].ActualTypeSchema;
            Assert.True(externalSchema.Properties.ContainsKey("name"));
            Assert.True(externalSchema.Properties.ContainsKey("sub"));

            var subSchema = externalSchema.Properties["sub"].ActualTypeSchema;
            Assert.True(subSchema.Properties.ContainsKey("value"));
        }

        [Fact]
        public async Task When_external_file_ref_has_definition_fragment_then_it_resolves()
        {
            // Arrange - Root refs "external.json#/definitions/Foo", Foo refs local #/definitions/Bar (#566)
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/FragmentRoot.json";

            // Act
            var schema = await JsonSchema.FromFileAsync(path);

            // Assert
            Assert.NotNull(schema);
            var fooSchema = schema.Properties["foo"].ActualTypeSchema;
            Assert.True(fooSchema.Properties.ContainsKey("name"));
            Assert.True(fooSchema.Properties.ContainsKey("bar"));

            var barSchema = fooSchema.Properties["bar"].ActualTypeSchema;
            Assert.True(barSchema.Properties.ContainsKey("value"));
        }

        [Fact]
        public async Task When_definitions_use_nested_grouping_then_deep_ref_resolves()
        {
            // Arrange - $ref: "#/definitions/group/nested/item" (#450)
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/NestedDefsGrouping.json";

            // Act
            var schema = await JsonSchema.FromFileAsync(path);

            // Assert
            Assert.NotNull(schema);
            var dataSchema = schema.Properties["data"].ActualTypeSchema;
            Assert.True(dataSchema.Properties.ContainsKey("name"));
            Assert.Equal(JsonObjectType.String, dataSchema.Properties["name"].Type);
        }

        [Fact]
        public async Task When_nested_definitions_have_schema_with_internal_ref_then_resolution_works()
        {
            // Arrange - homes/myHome is a schema with a property that refs types/standard (#450)
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/NestedDefsChained.json";

            // Act
            var schema = await JsonSchema.FromFileAsync(path);

            // Assert
            Assert.NotNull(schema);
            var homeSchema = schema.Properties["home"].ActualTypeSchema;
            Assert.True(homeSchema.Properties.ContainsKey("address"));
            Assert.Equal(JsonObjectType.String, homeSchema.Properties["address"].Type);
        }

        [Fact]
        public async Task When_circular_file_refs_use_definition_fragments_then_resolution_works()
        {
            // Arrange - A refs B#/definitions/Foo, B's Foo refs A#/definitions/Bar
            var path = GetTestDirectory() + "/References/LazyReferenceResolutionTests/CircularFragmentA.json";

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var schema = await JsonSchema.FromFileAsync(path, cts.Token);

            // Assert
            Assert.NotNull(schema);
            Assert.True(schema.Properties.ContainsKey("foo"));

            var fooSchema = schema.Properties["foo"].ActualTypeSchema;
            Assert.True(fooSchema.Properties.ContainsKey("value"));
            Assert.True(fooSchema.Properties.ContainsKey("bar"));

            var barSchema = fooSchema.Properties["bar"].ActualTypeSchema;
            Assert.True(barSchema.Properties.ContainsKey("name"));
        }

        private string GetTestDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase.Replace("#", "%23");
            var uri = new UriBuilder(codeBase);
            return Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path))!;
        }
    }
}
