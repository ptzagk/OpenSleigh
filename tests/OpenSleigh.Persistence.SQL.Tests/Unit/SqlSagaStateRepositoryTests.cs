using System;
using OpenSleigh.Core.Utils;
using Xunit;

namespace OpenSleigh.Persistence.SQL.Tests.Unit
{
    public class SqlSagaStateRepositoryTests
    {
        
        [Fact]
        public void ctor_should_throw_when_input_null()
        {
            var serializer = NSubstitute.Substitute.For<ISerializer>();
            var dbContext = NSubstitute.Substitute.For<ISagaDbContext>();
            var options = SqlSagaStateRepositoryOptions.Default;
            Assert.Throws<ArgumentNullException>(() => new SqlSagaStateRepository(null, null, options));
            Assert.Throws<ArgumentNullException>(() => new SqlSagaStateRepository(null, serializer, null));
            Assert.Throws<ArgumentNullException>(() => new SqlSagaStateRepository(dbContext, null, null));
        }
    }
}
