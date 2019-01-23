using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NJsonSchema.Infrastructure
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public interface IXmlDocumentationService
    {
        Task<string> GetDescriptionAsync(MemberInfo memberInfo, IEnumerable<Attribute> attributes);

        Task<string> GetDescriptionAsync(ParameterInfo parameter, IEnumerable<Attribute> attributes);

        Task<XElement> GetXmlDocumentationAsync(MemberInfo member);

        Task<string> GetXmlDocumentationAsync(ParameterInfo parameter);

        Task<string> GetXmlDocumentationAsync(MemberInfo member, string tagName);

        Task<string> GetXmlRemarksAsync(MemberInfo member);

        Task<string> GetXmlSummaryAsync(MemberInfo member);

        Task ClearCacheAsync();
    }
}