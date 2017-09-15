//-----------------------------------------------------------------------
// <copyright file="LiquidTemplate.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Diagnostics;
using DotLiquid;

namespace NJsonSchema.CodeGeneration
{
    internal class LiquidTemplate : ITemplate
    {
        private readonly string _template;
        private readonly object _model;

        public LiquidTemplate(string template, object model)
        {
            _template = template;
            _model = model;
        }

        public string Render()
        {
            var tpl = Template.Parse(_template);
            var hash = LiquidHash.FromObject(_model);
            // TODO: Check models here
            return tpl.Render(new RenderParameters
            {
                LocalVariables = hash,
                Filters = new[] { typeof(LiquidFilters) }
            }).Trim('\r', '\n', ' ');
        }
    }

    public static class LiquidFilters
    {
        public static string CSharpDocs(string input)
        {
            // TODO: Check if this is really called!
            return ConversionUtilities.ConvertCSharpDocBreaks(input, 0);
        }
    }
}