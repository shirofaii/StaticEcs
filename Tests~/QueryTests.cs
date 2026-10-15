using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public struct Npc : IEntityType {
        public byte Id() => 1;
    }

    public struct Spawned : IEntityType {
        public int Start;
        public byte Id() => 2;
        public void OnCreate(World.Entity entity) => entity.Set(new Pos { X = Start });
    }

    public class QueryTests : WorldTest {
        [Fact]
        public void WithWithoutCountsAndIterates() {
            for (var i = 0; i < 600; i++) {
                var e = W.NewEntity<Default>();
                if (i % 2 == 0) e.Set(new Pos { X = i });
                if (i % 3 == 0) e.Set(new Vel { V = i });
                if (i % 5 == 0) e.Set<Red>();
                if (i % 7 == 0) e.Set<Blue>();
            }
            Assert.Equal(300, W.With<Pos>().Count());
            Assert.Equal(100, W.With<Pos, Vel>().Count());
            Assert.Equal(200, W.With<Pos>().Without<Vel>().Count());
            Assert.Equal(160, W.With<Pos>().Without<Vel, Red>().Count());

            var expected = 0;
            for (var i = 0; i < 600; i += 2) if (i % 3 != 0 && i % 5 != 0 && i % 7 != 0) expected++;
            var seen = 0;
            foreach (var e in W.With<Pos>().Without<Vel>().Without<Red, Blue>()) {
                Assert.True(e.Has<Pos>());
                Assert.False(e.Has<Vel>() || e.Has<Red>() || e.Has<Blue>());
                seen++;
            }
            Assert.Equal(expected, seen);
            Assert.Equal(expected, W.With<Pos>().Without<Vel>().Without<Red, Blue>().Count());
        }

        [Fact]
        public void ForeachSkipsDisabledEntitiesButEntitiesAnyKeepsThem() {
            var a = W.NewEntity<Default>();
            a.Set(new Pos { X = 1 });
            var b = W.NewEntity<Default>();
            b.Set(new Pos { X = 2 });
            b.Disable();
            var enabled = 0;
            foreach (var _ in W.With<Pos>()) enabled++;
            var any = 0;
            foreach (var _ in W.With<Pos>().Entities(EntityStatusType.Any)) any++;
            Assert.Equal(1, enabled);
            Assert.Equal(2, any);
            Assert.Equal(2, W.With<Pos>().Count(EntityStatusType.Any));
        }

        [Fact]
        public void IsFiltersByEntityTypeAndChainsWithout() {
            for (var i = 0; i < 90; i++) {
                var e = i % 3 == 0 ? W.NewEntity<Npc>() : W.NewEntity<Default>();
                if (i % 2 == 0) e.Set<Red>();
            }
            Assert.Equal(30, W.Is<Npc>().Count());
            Assert.Equal(15, W.Is<Npc>().Without<Red>().Count());
            Assert.Equal(60, W.Is<Default>().Count());
        }

        [Fact]
        public void IsMatchAndContainsFollowTheFilter() {
            var a = W.NewEntity<Default>();
            a.Set(new Pos { X = 1 });
            var b = W.NewEntity<Default>();
            b.Set(new Pos { X = 2 });
            b.Set<Red>();
            var c = W.NewEntity<Npc>();
            c.Set(new Pos { X = 3 });

            Assert.True(W.With<Pos>().Without<Red>().Contains(a));
            Assert.False(W.With<Pos>().Without<Red>().Contains(b));
            Assert.True(W.With<Pos, Red>().Contains(b));
            Assert.True(W.Is<Npc>().Contains(c));
            Assert.False(W.Is<Npc>().Contains(a));
            Assert.True(a.IsMatch<All<Pos>>());
            Assert.False(a.IsMatch<All<Red>>());
            b.Disable();
            Assert.False(W.With<Pos>().Contains(b));
        }

        [Fact]
        public void AnEntityTypeOnCreateHookRunsWithItsData() {
            var e = W.NewEntity(new Spawned { Start = 7 });
            Assert.True(e.Has<Pos>());
            Assert.Equal(7, e.Ref<Pos>().X);
            Assert.Equal(1, W.Is<Spawned>().Count());
        }

        [Fact]
        public void DeletingTheFilteredComponentInsideForeachIsAllowedForTheCurrentEntity() {
            for (var i = 0; i < 300; i++) W.NewEntity<Default>().Set(new Pos { X = i });
            foreach (var e in W.With<Pos>()) e.Delete<Pos>();
            Assert.Equal(0, W.With<Pos>().Count());
            Assert.Equal(300, W.Query().Count());
        }
    }
}
