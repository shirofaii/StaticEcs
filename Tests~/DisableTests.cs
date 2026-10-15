using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class DisableTests : WorldTest {
        [Fact]
        public void ADisabledComponentKeepsItsDataAndLeavesDefaultQueries() {
            var e = W.NewEntity<Default>();
            e.Set(new Hp { Value = 4 });
            Assert.True(e.HasEnabled<Hp>());
            Assert.False(e.HasDisabled<Hp>());

            e.Disable<Hp>();
            Assert.True(e.Has<Hp>());
            Assert.False(e.HasEnabled<Hp>());
            Assert.True(e.HasDisabled<Hp>());
            Assert.Equal(4, e.Read<Hp>().Value);
            Assert.Equal(0, Count<All<Hp>>());
            Assert.Equal(1, W.Query().Count());

            e.Enable<Hp>();
            Assert.True(e.HasEnabled<Hp>());
            Assert.Equal(1, Count<All<Hp>>());
        }

        [Fact]
        public void DisabledThenDeletedThenAddedComesBackEnabled() {
            var e = W.NewEntity<Default>();
            e.Set(new Hp { Value = 1 });
            e.Disable<Hp>();
            e.Delete<Hp>();
            Assert.False(e.Has<Hp>());
            Assert.False(e.HasDisabled<Hp>());
            e.Set(new Hp { Value = 2 });
            Assert.True(e.HasEnabled<Hp>());
            Assert.Equal(1, Count<All<Hp>>());
        }

        [Fact]
        public void DisabledEntitiesNeedTheirStatusToBeQueried() {
            var a = W.NewEntity<Default>();
            var b = W.NewEntity<Default>();
            a.Set(new Pos());
            b.Set(new Pos());
            b.Disable();
            Assert.Equal(1, Count<All<Pos>>());
            Assert.Equal(2, CountAny<All<Pos>>());
            b.Enable();
            Assert.Equal(2, Count<All<Pos>>());
        }
    }
}
