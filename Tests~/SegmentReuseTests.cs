using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class SegmentReuseTests : WorldTest {
        [Fact]
        public void AComponentAddedAfterItsSegmentWasFreedStartsAtDefault() {
            var e = W.NewEntity<Default>();
            for (var i = 1; i <= 50; i++) {
                ref var pos = ref e.Add<Pos>();
                Assert.Equal(0, pos.X);
                pos.X = i;
                e.Delete<Pos>();
            }
            Assert.False(e.Has<Pos>());
            Assert.Equal(0, W.With<Pos>().Count());
        }

        [Fact]
        public void ChurnAcrossManySegmentsKeepsDataAndQueriesExact() {
            const int n = 3000;
            var entities = new W.Entity[n];
            for (var i = 0; i < n; i++) entities[i] = W.NewEntity<Default>();
            for (var round = 0; round < 4; round++) {
                for (var i = round; i < n; i += 3) {
                    entities[i].Set(new Vel { V = i * 10 + round });
                    entities[i].Set<Green>();
                }
                var expected = 0;
                for (var i = 0; i < n; i++) {
                    var has = (i - round) % 3 == 0 && i >= round;
                    Assert.Equal(has, entities[i].Has<Vel>());
                    Assert.Equal(has, entities[i].Has<Green>());
                    if (has) {
                        expected++;
                        Assert.Equal(i * 10 + round, entities[i].Ref<Vel>().V);
                    }
                }
                Assert.Equal(expected, W.With<Vel, Green>().Count());
                for (var i = 0; i < n; i++) {
                    entities[i].Delete<Vel>();
                    entities[i].Delete<Green>();
                }
                Assert.Equal(0, W.With<Vel>().Count());
                Assert.Equal(0, W.With<Green>().Count());
            }
        }

        [Fact]
        public void AnOnDeleteHookThatReaddsKeepsTheSegment() {
            var e = W.NewEntity<Default>();
            e.Set(new Hooked { V = 1 });
            e.Delete<Hooked>();
            e.Set(new Hooked { V = 2 });
            Assert.True(e.Has<Hooked>());
            Assert.Equal(2, e.Ref<Hooked>().V);
            Assert.Equal(1, W.With<Hooked>().Count());
        }
    }
}
