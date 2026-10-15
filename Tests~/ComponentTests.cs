using System.Collections.Generic;
using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class ComponentTests : WorldTest {
        [Fact]
        public void AddSetReadDeleteRoundTrip() {
            var e = W.NewEntity<Default>();
            Assert.False(e.Has<Pos>());

            ref var p = ref e.Add<Pos>();
            Assert.True(e.Has<Pos>());
            Assert.Equal(0, p.X);
            p.X = 5;
            Assert.Equal(5, e.Read<Pos>().X);

            e.Add<Pos>(out var isNew);
            Assert.False(isNew);
            Assert.Equal(5, e.Ref<Pos>().X);

            e.Set(new Pos { X = 9 });
            Assert.Equal(9, e.Ref<Pos>().X);

            Assert.True(e.Delete<Pos>());
            Assert.False(e.Has<Pos>());
            Assert.False(e.Delete<Pos>());

            e.Add<Pos>();
            Assert.Equal(0, e.Ref<Pos>().X);
        }

        [Fact]
        public void HasCombinationsAndCounts() {
            var e = W.NewEntity<Default>();
            e.Set(new Pos { X = 1 });
            e.Set(new Vel { V = 2 });
            Assert.True(e.Has<Pos, Vel>());
            Assert.False(e.Has<Pos, Hp>());
            Assert.True(e.HasAny<Hp, Vel>());
            Assert.False(e.HasAny<Hp, Tracked>());
            Assert.Equal(2, e.ComponentsCount());

            var list = new List<IComponent>();
            e.GetAllComponents(list);
            Assert.Equal(2, list.Count);
            Assert.Contains(list, c => c is Pos pos && pos.X == 1);
            Assert.Contains(list, c => c is Vel vel && vel.V == 2);
        }

        [Fact]
        public void ComponentsAreIndependentPerEntity() {
            var a = W.NewEntity<Default>();
            var b = W.NewEntity<Default>();
            a.Set(new Pos { X = 1 });
            b.Set(new Pos { X = 2 });
            b.Set(new Vel { V = 3 });
            Assert.Equal(1, a.Ref<Pos>().X);
            Assert.Equal(2, b.Ref<Pos>().X);
            Assert.False(a.Has<Vel>());
            a.Delete<Pos>();
            Assert.Equal(2, b.Ref<Pos>().X);
        }

        [Fact]
        public void DestroyedEntityLeavesNothingForTheNextOneInItsSlot() {
            var e = W.NewEntity<Default>();
            e.Set(new Pos { X = 7 });
            e.Set(new Hp { Value = 3 });
            e.Set<Red>();
            var id = e.ID;
            e.Destroy();

            var reused = W.NewEntity<Default>();
            Assert.Equal(id, reused.ID);
            Assert.False(reused.Has<Pos>());
            Assert.False(reused.Has<Hp>());
            Assert.False(reused.Has<Red>());
            Assert.Equal(0, reused.ComponentsCount());
            Assert.Equal(0, reused.TagsCount());
        }

        [Fact]
        public void QueriesFilterByComponents() {
            for (var i = 0; i < 300; i++) {
                var e = W.NewEntity<Default>();
                if (i % 2 == 0) e.Set(new Pos { X = i });
                if (i % 3 == 0) e.Set(new Vel { V = i });
            }
            Assert.Equal(150, Count<All<Pos>>());
            Assert.Equal(100, Count<All<Vel>>());
            Assert.Equal(50, Count<All<Pos, Vel>>());
            Assert.Equal(100, CountWith<All<Pos>, None<Vel>>());
            Assert.Equal(200, Count<Any<Pos, Vel>>());

            var sum = 0;
            foreach (var e in W.Query<All<Pos, Vel>>().Entities()) sum += e.Ref<Pos>().X;
            var expected = 0;
            for (var i = 0; i < 300; i += 6) expected += i;
            Assert.Equal(expected, sum);
        }

        [Fact]
        public void ManyEntitiesAcrossChunksGrowStorageAndStayCorrect() {
            const int n = 70_000;
            var entities = new W.Entity[n];
            for (var i = 0; i < n; i++) {
                entities[i] = W.NewEntity<Default>();
                entities[i].Set(new Pos { X = i });
                if (i % 7 == 0) entities[i].Set<Red>();
            }
            for (var i = 0; i < n; i += 5) entities[i].Delete<Pos>();

            Assert.Equal(n - n / 5, Count<All<Pos>>());
            Assert.Equal((n + 6) / 7, Count<All<Red>>());
            for (var i = 0; i < n; i += 997) {
                Assert.Equal(i % 5 != 0, entities[i].Has<Pos>());
                if (i % 5 != 0) Assert.Equal(i, entities[i].Ref<Pos>().X);
                Assert.Equal(i % 7 == 0, entities[i].Has<Red>());
            }

            for (var i = 0; i < n; i += 2) entities[i].Destroy();
            Assert.Equal(n / 2, Count<Nothing>());
            Assert.Equal(CountExpected(n), Count<All<Pos>>());
        }

        private static int CountExpected(int n) {
            var c = 0;
            for (var i = 1; i < n; i += 2) if (i % 5 != 0) c++;
            return c;
        }

        private static int CountWith<TA, TB>() where TA : struct, IQueryFilter where TB : struct, IQueryFilter {
            var n = 0;
            foreach (var _ in W.Query<TA, TB>().Entities()) n++;
            return n;
        }

        [Fact]
        public void DestroyingAndRecreatingTheWorldStartsClean() {
            for (var i = 0; i < 100; i++) {
                var e = W.NewEntity<Default>();
                e.Set(new Pos { X = i });
                e.Set<Blue>();
            }
            W.Destroy();
            Create();
            Assert.Equal(0, Count<Nothing>());
            Assert.Equal(0, Count<All<Pos>>());
            Assert.Equal(0, Count<All<Blue>>());
            var e2 = W.NewEntity<Default>();
            Assert.False(e2.Has<Pos>());
            Assert.False(e2.Has<Blue>());
            e2.Set(new Pos { X = 1 });
            Assert.Equal(1, Count<All<Pos>>());
        }
    }
}
