//-----------------------------------------------------------------------
// <copyright file="ClassTemplateModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NJsonSchema.CodeGeneration.Models
{
    /// <summary>The class template base class.</summary>
    public abstract class ClassTemplateModelBase : TemplateModelBase
    {
        private readonly JsonSchema4 _schema;
        private readonly object _rootObject;
        private readonly TypeResolverBase _resolver;

        /// <summary>Initializes a new instance of the <see cref="ClassTemplateModelBase" /> class.</summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        protected ClassTemplateModelBase(TypeResolverBase resolver, JsonSchema4 schema, object rootObject)
        {
            _schema = schema;
            _rootObject = rootObject;
            _resolver = resolver;
        }

        /// <summary>Gets the class.</summary>
        public abstract string ClassName { get; }

        /// <summary>Gets the derived class names (discriminator key/type name).</summary>
        public IDictionary<string, string> DerivedClasses => _schema
            .GetDerivedSchemas(_rootObject)
            .Select(p => new { Discriminator = p.Value, ClassName = _resolver.GetOrGenerateTypeName(p.Key, p.Value), Schema = p.Value })
            .ToDictionary(s => !string.IsNullOrEmpty(s.Discriminator) ? s.Discriminator : s.ClassName, s => s.ClassName);
    }
}