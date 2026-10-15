using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class BatchTests : WorldTest {
        private static void Make(int n) {
            for (var i = 0; i < n; i++) {
                var e = W.NewEntity<Default>();
                e.Set(new Pos { X = i });
                if (i % 2 == 0) e.Set<Red>();
            }
        }

        [Fact]
        public void BatchSetAndDeleteComponents() {
            Make(1000);
            W.Query<All<Red>>().BatchSet(new Vel { V = 7 });
            Assert.Equal(500, Count<All<Vel>>());
            foreach (var e in W.Query<All<Vel>>().Entities()) Assert.Equal(7, e.Ref<Vel>().V);
            W.Query<All<Vel>>().BatchDelete<Vel>();
            Assert.Equal(0, Count<All<Vel>>());
            Assert.Equal(1000, Count<All<Pos>>());
        }

        [Fact]
        public void BatchTagOperations() {
            Make(1000);
            W.Query<All<Red>>().BatchSet<Blue>();
            Assert.Equal(500, Count<All<Blue>>());
            Assert.Equal(500, Count<All<Red, Blue>>());

            W.Query<All<Pos>>().BatchToggle<Blue>();
            Assert.Equal(500, Count<All<Blue>>());
            Assert.Equal(0, Count<All<Red, Blue>>());

            W.Query<All<Pos>>().BatchApply<Green>(true);
            Assert.Equal(1000, Count<All<Green>>());
            W.Query<All<Red>>().BatchApply<Green>(false);
            Assert.Equal(500, Count<All<Green>>());

            W.Query<All<Blue>>().BatchDelete<Blue>();
            Assert.Equal(0, Count<All<Blue>>());
        }

        [Fact]
        public void BatchDisableEnableAndDestroy() {
            for (var i = 0; i < 300; i++) W.NewEntity<Default>().Set(new Hp { Value = i });
            W.Query<All<Hp>>().BatchDisable<Hp>();
            Assert.Equal(0, Count<All<Hp>>());
            Assert.Equal(300, Count<AllOnlyDisabled<Hp>>());
            W.Query<AllOnlyDisabled<Hp>>().BatchEnable<Hp>();
            Assert.Equal(300, Count<All<Hp>>());

            Make(200);
            W.Query<All<Red>>().BatchDestroy();
            Assert.Equal(0, Count<All<Red>>());
            Assert.Equal(400, Count<Nothing>());
        }

        [Fact]
        public void ChainedBatch() {
            Make(256);
            W.Query<All<Pos>>().BatchSet(new Vel { V = 1 }).BatchSet<Blue>().BatchDelete<Red>();
            Assert.Equal(256, Count<All<Vel, Blue>>());
            Assert.Equal(0, Count<All<Red>>());
        }
    }
}
