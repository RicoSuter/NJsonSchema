using System;

namespace NJsonSchema.Generation.TypeMappers
{
    /// <summary>Maps .NET type to a generated JSON Schema describing an object.</summary>
    public class ObjectTypeMapper : ITypeMapper
    {
        private readonly Func<JsonSchemaGenerator, SchemaResolver, JsonSchema4> _schemaFactory;

        /// <summary>Initializes a new instance of the <see cref="ObjectTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="schema">The schema.</param>
        public ObjectTypeMapper(Type mappedType, JsonSchema4 schema)
            : this(mappedType, (schemaGenerator, schemaResolver) => schema)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ObjectTypeMapper"/> class.</summary>
        /// <param name="mappedType">Type of the mapped.</param>
        /// <param name="schemaFactory">The schema factory.</param>
        public ObjectTypeMapper(Type mappedType, Func<JsonSchemaGenerator, SchemaResolver, JsonSchema4> schemaFactory)
        {
            _schemaFactory = schemaFactory;
            MappedType = mappedType;
        }

        /// <summary>
        /// </summary>
        public Type MappedType { get; }

        /// <summary>Gets a value indicating whether to use a JSON Schema reference for the type.</summary>
        public bool UseReference { get; } = true;

        /// <summary></summary>
        /// <typeparam name="TSchemaType"></typeparam>
        /// <param name="schemaGenerator"></param>
        /// <param name="schemaResolver"></param>
        /// <returns></returns>
        public virtual TSchemaType GetSchema<TSchemaType>(JsonSchemaGenerator schemaGenerator, SchemaResolver schemaResolver) where TSchemaType : JsonSchema4, new()
        {
            if (!schemaResolver.HasSchema(MappedType, false))
                schemaResolver.AddSchema(MappedType, false, _schemaFactory(schemaGenerator, schemaResolver));

            return null;
        }
    }
}