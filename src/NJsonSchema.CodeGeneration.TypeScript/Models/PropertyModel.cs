//-----------------------------------------------------------------------
// <copyright file="PropertyModel.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Text.RegularExpressions;
using NJsonSchema.CodeGeneration.Models;

namespace NJsonSchema.CodeGeneration.TypeScript.Models
{
    /// <summary>The TypeScript property template model.</summary>
    /// <seealso cref="PropertyModelBase" />
    public class PropertyModel : PropertyModelBase
    {
        private static readonly string _validPropertyNameRegex = "^[a-zA-Z_$][0-9a-zA-Z_$]*$";

        private readonly string _parentTypeName;
        private readonly TypeScriptGeneratorSettings _settings;
        private readonly JsonProperty _property;
        private readonly TypeScriptTypeResolver _resolver;

        /// <summary>Initializes a new instance of the <see cref="PropertyModel"/> class.</summary>
        /// <param name="classTemplateModel">The class template model.</param>
        /// <param name="property">The property.</param>
        /// <param name="parentTypeName">Name of the parent type.</param>
        /// <param name="resolver">The resolver.</param>
        /// <param name="settings">The settings.</param>
        public PropertyModel(ClassTemplateModel classTemplateModel, JsonProperty property, string parentTypeName, TypeScriptTypeResolver resolver, TypeScriptGeneratorSettings settings)
            : base(property, classTemplateModel, new TypeScriptValueGenerator(resolver, settings), settings)
        {
            _property = property;
            _resolver = resolver;
            _parentTypeName = parentTypeName;
            _settings = settings;
        }

        /// <summary>Gets the name of the property in an interface.</summary>
        public string InterfaceName => Regex.IsMatch(_property.Name, _validPropertyNameRegex) ? _property.Name : $"\"{_property.Name}\"";

        /// <summary>Gets a value indicating whether the property has description.</summary>
        public bool HasDescription => !string.IsNullOrEmpty(Description);

        /// <summary>Gets the description.</summary>
        public string Description => _property.Description;

        /// <summary>Gets the type of the property.</summary>
        public override string Type => _resolver.Resolve(_property.ActualTypeSchema, _property.IsNullable(_settings.SchemaType), GetTypeNameHint());

        /// <summary>Gets the type of the property in the initializer interface.</summary>
        public string ConstructorInterfaceType => _settings.ConvertConstructorInterfaceData ?
            _resolver.ResolveConstructorInterfaceName(_property.ActualTypeSchema, _property.IsNullable(_settings.SchemaType), GetTypeNameHint()) :
            Type;

        /// <summary>Gets a value indicating whether the class or an inherited class has a discriminator property.</summary>
        public bool HasBaseDiscriminator => !string.IsNullOrEmpty(_property.BaseDiscriminator);

        /// <summary>Gets a value indicating whether constructor conversion is supported.</summary>
        public bool SupportsConstructorConversion
        {
            get
            {
                if (Type == ConstructorInterfaceType)
                    return false;

                if (IsArray)
                    return _property.ActualTypeSchema?.Item.ActualSchema.Type.HasFlag(JsonObjectType.Object) == true;

                if (IsDictionary)
                    return _property.ActualTypeSchema?.AdditionalPropertiesSchema.ActualSchema.Type.HasFlag(JsonObjectType.Object) == true;

                return !_property.ActualTypeSchema.IsTuple;
            }
        }

        /// <summary>Gets a value indicating whether the property type is an array.</summary>
        public bool IsArray => _property.ActualTypeSchema.IsArray;

        /// <summary>Gets a value indicating whether the property type is a dictionary.</summary>
        public bool IsDictionary => _property.ActualTypeSchema.IsDictionary;

        /// <summary>Gets the type of the array item.</summary>
        public string ArrayItemType => _resolver.TryResolve(_property.ActualTypeSchema?.Item, PropertyName) ?? "any";

        /// <summary>Gets the type of the dictionary item.</summary>
        public string DictionaryItemType => _resolver.TryResolve(_property.ActualTypeSchema?.AdditionalPropertiesSchema, PropertyName) ?? "any";

        /// <summary>Gets the type postfix (e.g. ' | null | undefined')</summary>
        public string TypePostfix
        {
            get
            {
                if (IsNullable && _settings.SupportsStrictNullChecks)
                    return " | " + _settings.NullValue.ToString().ToLowerInvariant();
                else
                    return string.Empty;
            }
        }

        /// <summary>Gets a value indicating whether the property is read only.</summary>
        public bool IsReadOnly => _property.IsReadOnly && _settings.TypeScriptVersion >= 2.0m;

        /// <summary>Gets a value indicating whether the property is optional.</summary>
        public bool IsOptional => !_property.IsRequired && _settings.MarkOptionalProperties;

        /// <summary>Gets a value indicating whether the property is nullable.</summary>
        public bool IsNullable => _property.IsNullable(_settings.SchemaType);

        /// <summary>Gets a value indicating whether the property is an inheritance discriminator.</summary>
        public bool IsDiscriminator => _property.IsInheritanceDiscriminator;

        /// <summary>Gets the convert to class code.</summary>
        public string ConvertToClassCode
        {
            get
            {
                var typeStyle = _settings.GetTypeStyle(_parentTypeName);
                if (typeStyle != TypeScriptTypeStyle.Interface)
                {
                    return DataConversionGenerator.RenderConvertToClassCode(new DataConversionParameters
                    {
                        Variable = typeStyle == TypeScriptTypeStyle.Class ?
                            (IsReadOnly ? "(<any>this)." : "this.") + PropertyName : PropertyName + "_",
                        Value = "data[\"" + _property.Name + "\"]",
                        Schema = _property.ActualTypeSchema,
                        IsPropertyNullable = _property.IsNullable(_settings.SchemaType),
                        TypeNameHint = PropertyName,
                        Resolver = _resolver,
                        NullValue = _settings.NullValue,
                        Settings = _settings
                    });
                }
                return string.Empty;
            }
        }

        /// <summary>Gets the convert to JavaScript code.</summary>
        public string ConvertToJavaScriptCode
        {
            get
            {
                var typeStyle = _settings.GetTypeStyle(_parentTypeName);
                if (typeStyle != TypeScriptTypeStyle.Interface)
                {
                    return DataConversionGenerator.RenderConvertToJavaScriptCode(new DataConversionParameters
                    {
                        Variable = "data[\"" + _property.Name + "\"]",
                        Value = typeStyle == TypeScriptTypeStyle.Class ? "this." + PropertyName : PropertyName + "_",
                        Schema = _property.ActualTypeSchema,
                        IsPropertyNullable = _property.IsNullable(_settings.SchemaType),
                        TypeNameHint = PropertyName,
                        Resolver = _resolver,
                        NullValue = _settings.NullValue,
                        Settings = _settings
                    });
                }
                return string.Empty;
            }
        }
    }
}