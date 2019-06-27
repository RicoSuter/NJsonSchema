//-----------------------------------------------------------------------
// <copyright file="DataConversionParameters.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NJsonSchema.CodeGeneration.TypeScript
{
    /// <summary>The template parameters.</summary>
    public class DataConversionParameters
    {
        /// <summary>Gets the variable.</summary>
        public string Variable { get; set; }

        /// <summary>Gets the value.</summary>
        public string Value { get; set; }

        /// <summary>Gets the schema.</summary>
        public JsonSchema Schema { get; set; }

        /// <summary>Gets a value indicating whether the property is nullable.</summary>
        public bool IsPropertyNullable { get; set; }

        /// <summary>Gets or sets a value indicating whether to check the newable data object for null.</summary>
        public bool CheckNewableObject { get; set; } = true;

        /// <summary>Gets the type name hint.</summary>
        public string TypeNameHint { get; set; }

        /// <summary>Gets the resolver.</summary>
        public TypeScriptTypeResolver Resolver { get; set; }

        /// <summary>Gets or sets the null value.</summary>
        public TypeScriptNullValue NullValue { get; set; }

        /// <summary>Gets or sets the settings.</summary>
        public TypeScriptGeneratorSettings Settings { get; set; }
    }
}