using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NJsonSchema.Tests.Schema
{
    [TestClass]
    public class JsonPathUtilitiesGetPropertyNameTests
    {       
        /// <summary>
        /// Tests workaround where PropertyInfo for base class property fails when obtained through derived class.
        /// See: https://github.com/dotnet/corefx/issues/5884
        /// </summary>
        [TestMethod]
        public void When_derived_class_then_property_names_should_be_resolved_correctly()
        {
            PropertyInfo[] propertyInfos = typeof(DerivedClass).GetProperties();
            IEnumerable<string> propertyNames = propertyInfos.Select(x => JsonPathUtilities.GetPropertyName(x, PropertyNameHandling.Default));

            Assert.AreEqual(3, propertyNames.Count());
            Assert.IsTrue(propertyNames.Any(x => x == "Base" || x == "Foo" || x == "Bar"));
        }

        private class BaseClass
        {
            public string Base { get; set; }
        }

        private class DerivedClass : BaseClass
        {
            public string Bar { get; set; }
            public string Foo { get; set; }
        }
    }
}