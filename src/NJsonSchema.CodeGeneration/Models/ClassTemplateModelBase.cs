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
    public abstract class ClassTemplateModelBase
    {
        private readonly JsonSchema4 _schema;
        private readonly object _rootObject;
        private readonly ITypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="ClassTemplateModelBase" /> class.</summary>
        /// <param name="resolver">The resolver.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="rootObject">The root object.</param>
        protected ClassTemplateModelBase(ITypeResolver resolver, JsonSchema4 schema, object rootObject)
        {
            _schema = schema;
            _rootObject = rootObject;
            _resolver = resolver;
        }

        /// <summary>Gets the class.</summary>
        public abstract string Class { get; }

        /// <summary>Gets the derived class names.</summary>
        public List<string> DerivedClassNames => _schema.GetDerivedSchemas(_rootObject, _resolver)
            .Where(s => s.Value.Inherits(_schema))
            .Select(s => s.Key)
            .ToList();
    }
}