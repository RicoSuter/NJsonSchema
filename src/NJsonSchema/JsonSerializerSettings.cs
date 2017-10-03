//-----------------------------------------------------------------------
// <copyright file="JsonSerializerSettings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;

namespace NJsonSchema.CodeGeneration.CSharp
{
    /// <summary>The JSON serializer settings for the generated code.</summary>
    public class JsonSerializerSettings
    {
        /// <summary>Gets or sets the DateParseHandling settings.</summary>
        public DateParseHandling? DateParseHandling { get; set; }

        /// <summary>Gets or sets the DateFormatString settings.</summary>
        public string DateFormatString { get; set; }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            var settingsString = DateParseHandling == null ? string.Empty :
                $"DateParseHandling = Newtonsoft.Json.DateParseHandling.{DateParseHandling.ToString()}, ";
            
            settingsString += string.IsNullOrEmpty(DateFormatString) ? string.Empty : 
                $"DateFormatString = \"{DateFormatString}\", ";

            return settingsString;
        }
    }
}
