using System.Collections.Generic;
using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public struct ProbeSystems : ISystemsType { }
    public abstract class Probes : W.Systems<ProbeSystems> { }

    public struct Probe : ISystem {
        public static readonly List<string> Seen = new();

        public void Update() {
            Seen.Add($"added {CountAny<AllAdded<Tracked>>()} changed {CountAny<AllChanged<Tracked>>()} deleted {CountAny<AllDeleted<Tracked>>()} tagAdded {CountAny<AllAdded<TrackedTag>>()} tagDeleted {CountAny<AllDeleted<TrackedTag>>()}");
        }

        private static int CountAny<TFilter>() where TFilter : struct, IQueryFilter {
            var n = 0;
            foreach (var _ in W.Query<TFilter>().Entities(EntityStatusType.Any)) n++;
            return n;
        }
    }

    public class TrackingTests : WorldTest {
        private static void Run(System.Action between) {
            between();
            Probes.Update();
            W.Tick();
        }

        private static List<string> Observe(params System.Action[] steps) {
            Probe.Seen.Clear();
            Probes.Create();
            Probes.Add(new Probe(), 0);
            Probes.Initialize();
            try {
                foreach (var step in steps) Run(step);
            }
            finally {
                Probes.Destroy();
            }
            return new List<string>(Probe.Seen);
        }

        [Fact]
        public void ComponentAddChangeAndDeleteAsSystemsSeeThem() {
            var e = default(W.Entity);
            var seen = Observe(
                () => { e = W.NewEntity<Default>(); e.Set(new Tracked { V = 1 }); },
                () => { },
                () => { },
                () => e.Mut<Tracked>().V = 2,
                () => { },
                () => { },
                () => e.Ref<Tracked>().V = 3,
                () => { },
                () => e.Delete<Tracked>(),
                () => { },
                () => { });
            Assert.Equal(string.Join(" | ", Expected.ComponentLifecycle), string.Join(" | ", seen));
        }

        [Fact]
        public void TagSetAndDeleteAsSystemsSeeThem() {
            var e = default(W.Entity);
            var seen = Observe(
                () => { e = W.NewEntity<Default>(); e.Set<TrackedTag>(); },
                () => { },
                () => { },
                () => e.Delete<TrackedTag>(),
                () => { },
                () => { });
            Assert.Equal(string.Join(" | ", Expected.TagLifecycle), string.Join(" | ", seen));
        }

        [Fact]
        public void TrackingIsPerEntityAcrossManyEntities() {
            var entities = new W.Entity[5000];
            var seen = Observe(
                () => {
                    for (var i = 0; i < entities.Length; i++) {
                        entities[i] = W.NewEntity<Default>();
                        entities[i].Set(new Tracked { V = i });
                    }
                },
                () => { },
                () => { },
                () => {
                    for (var i = 0; i < entities.Length; i += 3) entities[i].Mut<Tracked>().V++;
                    for (var i = 1; i < entities.Length; i += 3) entities[i].Delete<Tracked>();
                },
                () => { });
            Assert.Equal(string.Join(" | ", Expected.ManyEntities), string.Join(" | ", seen));
        }

        [Fact]
        public void EntityChecksSeeTheCurrentTick() {
            var e = W.NewEntity<Default>();
            e.Set(new Tracked { V = 1 });
            e.Set<TrackedTag>();
            Assert.True(e.HasAdded<Tracked>());
            Assert.True(e.HasAdded<TrackedTag>());
            e.Delete<TrackedTag>();
            Assert.True(e.HasDeleted<TrackedTag>());
        }
    }

    public static class Expected {
        public static readonly List<string> ComponentLifecycle = new() {
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 1 changed 1 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 1 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 1 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
        };
        public static readonly List<string> TagLifecycle = new() {
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 1 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 1",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
        };
        public static readonly List<string> ManyEntities = new() {
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 5000 changed 5000 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 0 deleted 0 tagAdded 0 tagDeleted 0",
            "added 0 changed 1667 deleted 1667 tagAdded 0 tagDeleted 0",
        };
    }
}
