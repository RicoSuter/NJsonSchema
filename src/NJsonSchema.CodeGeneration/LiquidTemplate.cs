//-----------------------------------------------------------------------
// <copyright file="LiquidTemplate.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

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
            var hash = Hash.FromAnonymousObject(new { Model = Hash.FromAnonymousObject(_model) });
            return tpl.Render(hash);
        }
    }
}