using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class EventCleanupTests : WorldTest {
        [Fact]
        public void BatchDeleteClearsAComponentFromEveryMatchingEntity() {
            for (var i = 0; i < 1000; i++) {
                var e = W.NewEntity<Default>();
                e.Set(new Pos { X = i });
                if (i % 3 == 0) e.Set(new Hooked { V = i });
            }
            Hooked.Calls.Clear();
            W.Query<All<Hooked>>().BatchDelete<Hooked>();
            Assert.Equal(0, Count<All<Hooked>>());
            Assert.Equal(1000, Count<All<Pos>>());
            Assert.Equal(334, Hooked.Calls.Count);
            Assert.All(Hooked.Calls, call => Assert.EndsWith("Default", call));
        }

        [Fact]
        public void BatchDeleteClearsATagAndOnlyWhereTheFilterMatches() {
            for (var i = 0; i < 1000; i++) {
                var e = W.NewEntity<Default>();
                e.Set<Red>();
                if (i % 2 == 0) e.Set<Blue>();
            }
            W.Query<All<Blue>>().BatchDelete<Red>();
            Assert.Equal(500, Count<All<Red>>());
            Assert.Equal(0, Count<All<Red, Blue>>());
            Assert.Equal(500, Count<All<Blue>>());
        }
    }
}
