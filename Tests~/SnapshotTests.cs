using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class SnapshotTests : WorldTest {
        [Fact]
        public void AWorldSnapshotRestoresComponentsTagsAndDisabledState() {
            var gids = new EntityGID[3000];
            for (var i = 0; i < gids.Length; i++) {
                var e = W.NewEntity<Default>();
                e.Set(new Pos { X = i });
                if (i % 2 == 0) e.Set<Red>();
                if (i % 3 == 0) {
                    e.Set(new Hp { Value = i });
                    if (i % 9 == 0) e.Disable<Hp>();
                }
                if (i % 10 == 0) e.Set(new Name { Value = $"n{i}" });
                gids[i] = e.GID;
            }
            var snapshot = W.Serializer.CreateWorldSnapshot();
            W.Destroy();
            Create();
            W.Serializer.LoadWorldSnapshot(snapshot);

            Assert.Equal(gids.Length, Count<All<Pos>>());
            Assert.Equal(gids.Length / 2, Count<All<Red>>());
            Assert.Equal(1000 - 334, Count<All<Hp>>());
            Assert.Equal((gids.Length + 8) / 9, Count<AllOnlyDisabled<Hp>>());
            for (var i = 0; i < gids.Length; i += 37) {
                Assert.True(gids[i].TryUnpack<TW>(out var e));
                Assert.Equal(i, e.Ref<Pos>().X);
                Assert.Equal(i % 2 == 0, e.Has<Red>());
                Assert.Equal(i % 3 == 0, e.Has<Hp>());
                if (i % 3 == 0) {
                    Assert.Equal(i, e.Read<Hp>().Value);
                    Assert.Equal(i % 9 == 0, e.HasDisabled<Hp>());
                }
                Assert.Equal(i % 10 == 0, e.Has<Name>());
                if (i % 10 == 0) Assert.Equal($"n{i}", e.Read<Name>().Value);
            }
        }
    }
}
