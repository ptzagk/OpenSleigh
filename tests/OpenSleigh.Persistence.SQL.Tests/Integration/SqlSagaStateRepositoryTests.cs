using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OpenSleigh.Core.Exceptions;
using OpenSleigh.Core.Utils;
using OpenSleigh.Persistence.SQL.Tests.Fixtures;
using Xunit;

namespace OpenSleigh.Persistence.SQL.Tests.Integration
{
    [Category("Integration")]
    [Trait("Category", "Integration")]
    public class SqlSagaStateRepositoryTests : IClassFixture<DbFixture>
    {
        private readonly DbFixture _fixture;

        public SqlSagaStateRepositoryTests(DbFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task LockAsync_should_create_and_return_locked_item_if_not_existing()
        {
            var sut = CreateSut();

            var newState = DummyState.New();

            var (state, lockId) = await sut.LockAsync(newState.Id, newState, CancellationToken.None);
            state.Should().NotBeNull();
            state.Id.Should().Be(newState.Id);
            state.Bar.Should().Be(newState.Bar);
            state.Foo.Should().Be(newState.Foo);
        }

        [Fact]
        public async Task LockAsync_should_throw_if_item_locked()
        {
            var sut = CreateSut();

            var newState = DummyState.New();

            await sut.LockAsync(newState.Id, newState, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<LockException>(async () => await sut.LockAsync(newState.Id, newState, CancellationToken.None));
            ex.Message.Should().Contain($"saga state '{newState.Id}' is already locked");
        }

        [Fact]
        public async Task LockAsync_should_return_state_if_lock_expired()
        {
            var options = new SqlSagaStateRepositoryOptions(TimeSpan.Zero);
            var sut = CreateSut(options);

            var newState = DummyState.New();

            var (firstState, firstLockId) = await sut.LockAsync(newState.Id, newState, CancellationToken.None);

            await Task.Delay(500);

            var (secondState, secondLockId) = await sut.LockAsync(newState.Id, newState, CancellationToken.None);
            secondLockId.Should().NotBe(firstLockId);
            secondState.Should().NotBeNull();
        }

        [Fact]
        public async Task LockAsync_should_allow_different_saga_state_types_to_share_the_correlation_id()
        {
            var sut = CreateSut();

            var correlationId = Guid.NewGuid();

            var newState = new DummyState(correlationId, "lorem", 42);

            var (state, lockId) = await sut.LockAsync(correlationId, newState, CancellationToken.None);

            var newState2 = new DummyState2(state.Id);
            newState2.Id.Should().Be(newState.Id);

            var (state2, lockId2) = await sut.LockAsync(correlationId, newState2, CancellationToken.None);
            state2.Should().NotBeNull();
            state2.Id.Should().Be(correlationId);
        }

        private SqlSagaStateRepository CreateSut(SqlSagaStateRepositoryOptions options = null)
        {
            var serializer = new JsonSerializer();
            var sut = new SqlSagaStateRepository(_fixture.DbContext, serializer, options ?? SqlSagaStateRepositoryOptions.Default);
            return sut;
        }
    }
}
