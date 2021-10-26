using System;
using System.Collections.Generic;
using NJsonSchema.Generation;
using Xunit;
using Namotion.Reflection;

namespace NJsonSchema.Tests.Generation
{
    public class DefaultReflectionServiceTests
    {
        [Fact]
        public void When_ReferenceTypeNullHandling_is_Null_then_nullability_is_correct()
        {
            //// Arrange
            var checks = new Dictionary<Type, bool>
            {
                { typeof(bool), false },
                { typeof(int), false },
                { typeof(char), false },
                { typeof(short), false },
                { typeof(long), false },
                { typeof(DateTime), false },
                { typeof(NodaTime.Instant), false },

                { typeof(long?), true },

                { typeof(string), true },
                { typeof(Uri), true },
                { typeof(object), true },
                { typeof(JsonSchemaGeneratorSettings), true },
                { typeof(IReflectionService), true },
                { typeof(Type), true },
                { typeof(byte[]), true },
                { typeof(NodaTime.DateTimeZone), true },
                { typeof(Dictionary<string, string>), true },
            };

            //// Act
            var svc = new NewtonsoftJsonReflectionService();

            //// Assert
            foreach (var check in checks)
            {
                Assert.Equal(check.Value, svc.IsNullable(check.Key.ToContextualType(), ReferenceTypeNullHandling.Null));
            }
        }

        [Fact]
        public void When_ReferenceTypeNullHandling_is_NotNull_then_nullability_is_correct()
        {
            //// Arrange
            var checks = new Dictionary<Type, bool>
            {
                { typeof(bool), false },
                { typeof(int), false },
                { typeof(char), false },
                { typeof(short), false },
                { typeof(long), false },
                { typeof(DateTime), false },
                { typeof(NodaTime.Instant), false },

                { typeof(long?), true },

                { typeof(string), false },
                { typeof(Uri), false },
                { typeof(object), false },
                { typeof(JsonSchemaGeneratorSettings), false },
                { typeof(IReflectionService), false },
                { typeof(Type), false },
                { typeof(byte[]), false },
                { typeof(NodaTime.DateTimeZone), false },
                { typeof(Dictionary<string, string>), false },
            };

            //// Act
            var svc = new NewtonsoftJsonReflectionService();

            //// Assert
            foreach (var check in checks)
            {
                Assert.Equal(check.Value, svc.IsNullable(check.Key.ToContextualType(), ReferenceTypeNullHandling.NotNull));
            }
        }
    }
}
