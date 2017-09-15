using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema.Infrastructure;

namespace NJsonSchema.Tests.Infrastructure
{
    [TestClass]
    public class ReflectionCacheTests
    {
        public class TestClass
        {
            public string field;

            public static string staticField;

            public string property { get; set; }

            public static string staticProperty { get; set; }
        }

        [TestMethod]
        public void Static_members_are_not_returned_from_ReflectionCache()
        {
            //// Act
            var propertiesAndFields = ReflectionCache.GetPropertiesAndFields(typeof(TestClass));

            //// Assert
            Assert.IsTrue(propertiesAndFields.Any(p => p.MemberInfo.Name == "field"));
            Assert.IsFalse(propertiesAndFields.Any(p => p.MemberInfo.Name == "staticField"));
            Assert.IsTrue(propertiesAndFields.Any(p => p.MemberInfo.Name == "property"));
            Assert.IsFalse(propertiesAndFields.Any(p => p.MemberInfo.Name == "staticProperty"));
        }
   }
}