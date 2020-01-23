using System.Threading.Tasks;
using System.Runtime.Serialization;
using NJsonSchema.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
  public class PrivateFieldTests
  {
    [DataContract]
    public class ClassWithPrivateField
    {
      [DataMember]
      private int FooField;

      [DataMember]
      private int FooProp { get; set; }

      public int Get() => FooField + FooProp; // to avoid not-used warning
    }

    [Fact]
    public async Task When_property_or_field_is_private_then_schema_still_creates_properties()
    {
      //// Act
      var schema = JsonSchema.FromType<ClassWithPrivateField>(new JsonSchemaGeneratorSettings { SchemaType = SchemaType.OpenApi3 });
      var json = schema.ToJson();

      //// Assert
      Assert.Equal(2, schema.ActualProperties.Count);
      var field = schema.ActualProperties["FooField"].ActualTypeSchema;
      Assert.Equal(JsonObjectType.Integer, field.Type);
      var prop = schema.ActualProperties["FooProp"].ActualTypeSchema;
      Assert.Equal(JsonObjectType.Integer, prop.Type);
    }
  }
}