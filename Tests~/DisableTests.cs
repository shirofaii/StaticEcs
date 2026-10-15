using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class DisableTests : WorldTest {
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
