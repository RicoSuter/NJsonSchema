//-----------------------------------------------------------------------
// <copyright file="ClassTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>The class template base class.</summary>
    public abstract class ClassTemplateModelBase
    {
        private readonly JsonSchema _schema;
        private readonly object _rootObject;
        private readonly TypeResolverBase _resolver;

        /// <summary>Initializes a new instance of the <see cref="ClassTemplateModelBase" /> class.</summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        protected ClassTemplateModelBase(TypeResolverBase resolver, JsonSchema schema, object rootObject)
        {
            _schema = schema;
            _rootObject = rootObject;
            _resolver = resolver;

            SchemaTitle = _schema.Title;
        }

        /// <summary>Gets the class.</summary>
        public abstract string ClassName { get; }
        
        /// <summary>
        /// Gets the original title of the class (schema title).
        /// </summary>
        public string? SchemaTitle { get; }

        /// <summary>Gets a value indicating whether this class represents a JSON object with fixed amount of properties.</summary>
        public bool IsObject => _schema.ActualTypeSchema.IsObject;

        /// <summary>Gets or sets a value indicating whether the type is abstract.</summary>
        public bool IsAbstract => _schema.ActualTypeSchema.IsAbstract;

        /// <summary>Gets the property extension data.</summary>
        public IDictionary<string, object?>? ExtensionData => _schema.ExtensionData;

        /// <summary>Gets the derived class names (discriminator key/type name).</summary>
        public ICollection<DerivedClassModel> DerivedClasses => _schema
            .GetDerivedSchemas(_rootObject)
            .Where(p => _schema.ActualSchema.ResponsibleDiscriminatorObject != null)
            .Select(p => new DerivedClassModel(p.Value!, p.Key, _schema.ActualSchema.ResponsibleDiscriminatorObject!, _resolver))
            .ToList();

        /// <summary>The model of a derived class.</summary>
        public class DerivedClassModel
        {
            internal DerivedClassModel(string? typeName, JsonSchema schema, OpenApiDiscriminator discriminator, TypeResolverBase resolver)
            {
                var mapping = discriminator.Mapping.SingleOrDefault(m => m.Value.ActualTypeSchema == schema.ActualTypeSchema);

                ClassName = resolver.GetOrGenerateTypeName(schema, typeName);
                IsAbstract = schema.ActualTypeSchema.IsAbstract;

                Discriminator =
                    mapping.Value != null ? mapping.Key :
                    !string.IsNullOrEmpty(typeName) ? typeName! :
                    ClassName;
            }

            /// <summary>Gets the discriminator.</summary>
            public string Discriminator { get; }

            /// <summary>Gets the class name.</summary>
            public string ClassName { get; }

            /// <summary>Gets a value indicating whether the class is abstract.</summary>
            public bool IsAbstract { get; }
        }
    }
}