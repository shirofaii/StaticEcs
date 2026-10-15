using System.Collections.Generic;
using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class TagTests : WorldTest {
        [Fact]
        public void SetHasDeleteToggleApply() {
            var e = W.NewEntity<Default>();
            Assert.False(e.Has<Red>());
            Assert.True(e.Set<Red>());
            Assert.False(e.Set<Red>());
            Assert.True(e.Has<Red>());
            Assert.True(e.Delete<Red>());
            Assert.False(e.Delete<Red>());
            Assert.False(e.Has<Red>());

            e.Toggle<Red>();
            Assert.True(e.Has<Red>());
            e.Toggle<Red>();
            Assert.False(e.Has<Red>());

            e.Apply<Blue>(true);
            Assert.True(e.Has<Blue>());
            e.Apply<Blue>(true);
            Assert.True(e.Has<Blue>());
            e.Apply<Blue>(false);
            Assert.False(e.Has<Blue>());
        }

        [Fact]
        public void SeveralTagsCountAndListPerEntity() {
            var e = W.NewEntity<Default>();
            e.Set<Red, Blue>();
            Assert.True(e.Has<Red, Blue>());
            Assert.False(e.Has<Red, Green>());
            Assert.True(e.HasAny<Green, Blue>());
            Assert.Equal(2, e.TagsCount());
            var tags = new List<ITag>();
            e.GetAllTags(tags);
            Assert.Equal(2, tags.Count);
            Assert.Contains(tags, t => t is Red);
            Assert.Contains(tags, t => t is Blue);

            var other = W.NewEntity<Default>();
            Assert.False(other.Has<Red>());
            Assert.Equal(0, other.TagsCount());
        }

        [Fact]
        public void TagsAndComponentsMixInQueries() {
            for (var i = 0; i < 640; i++) {
                var e = W.NewEntity<Default>();
                if (i % 2 == 0) e.Set<Red>();
                if (i % 4 == 0) e.Set<Blue>();
                if (i % 5 == 0) e.Set(new Pos { X = i });
            }
            Assert.Equal(320, Count<All<Red>>());
            Assert.Equal(160, Count<All<Red, Blue>>());
            Assert.Equal(64, Count<All<Red, Pos>>());
            Assert.Equal(32, Count<All<Blue, Pos>>());
            Assert.Equal(320, Count<None<Red>>());
            Assert.Equal(320, Count<Any<Red, Blue>>());
            Assert.Equal(384, Count<Any<Red, Pos>>());
            var redNoBlue = 0;
            foreach (var _ in W.Query<All<Red>, None<Blue>>().Entities()) redNoBlue++;
            Assert.Equal(160, redNoBlue);
        }

        [Fact]
        public void TagsAcrossManyChunks() {
            const int n = 70_000;
            var entities = new W.Entity[n];
            for (var i = 0; i < n; i++) {
                entities[i] = W.NewEntity<Default>();
                if (i % 3 == 0) entities[i].Set<Green>();
            }
            Assert.Equal((n + 2) / 3, Count<All<Green>>());
            for (var i = 0; i < n; i += 6) entities[i].Delete<Green>();
            Assert.Equal((n + 2) / 3 - (n + 5) / 6, Count<All<Green>>());
            for (var i = 0; i < n; i += 4999) Assert.Equal(i % 3 == 0 && i % 6 != 0, entities[i].Has<Green>());
        }
    }
}
