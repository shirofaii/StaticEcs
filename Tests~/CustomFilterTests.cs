using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public struct EveryNth : IQueryFilter {
        public ulong Pattern;

        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType => ulong.MaxValue;

        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType => Pattern;

        #if DEBUG
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType { }
        #endif
    }

    public class CustomFilterTests : WorldTest {
        [Fact]
        public void AStatefulCustomFilterIsAppliedPerQuery() {
            for (var i = 0; i < 640; i++) {
                var e = W.NewEntity<Default>();
                if (i % 2 == 0) e.Set(new Pos { X = i });
            }
            var evenSlots = 0;
            foreach (var _ in W.Query(new EveryNth { Pattern = 0x5555555555555555UL }).Entities()) evenSlots++;
            var none = 0;
            foreach (var _ in W.Query(new EveryNth { Pattern = 0 }).Entities()) none++;
            Assert.Equal(320, evenSlots);
            Assert.Equal(0, none);
        }

        [Fact]
        public void ACustomFilterCombinedWithAllUsesBoth() {
            for (var i = 0; i < 640; i++) {
                var e = W.NewEntity<Default>();
                if (i % 4 == 0) e.Set(new Pos { X = i });
            }
            var filter = new And<EveryNth, All<Pos>>(new EveryNth { Pattern = 0x5555555555555555UL }, default);
            Assert.Equal(160, W.Query(filter).Count());
            var oddSlots = new And<EveryNth, All<Pos>>(new EveryNth { Pattern = 0xAAAAAAAAAAAAAAAAUL }, default);
            Assert.Equal(0, W.Query(oddSlots).Count());
        }
    }
}
