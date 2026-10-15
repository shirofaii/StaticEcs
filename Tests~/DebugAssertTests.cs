#if DEBUG
using System;
using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class DebugAssertTests : WorldTest {
        [Fact]
        public void RefOfAMissingComponentThrowsNamingIt() {
            var e = W.NewEntity<Default>();
            var ex = Assert.ThrowsAny<InvalidOperationException>(() => e.Ref<Pos>());
            Assert.Contains("Pos", ex.Message);
            Assert.Contains("missing", ex.Message);
        }

        [Fact]
        public void RefOnADestroyedEntityThrows() {
            var e = W.NewEntity<Default>();
            e.Set(new Pos { X = 1 });
            e.Destroy();
            var ex = Assert.ThrowsAny<InvalidOperationException>(() => e.Ref<Pos>());
            Assert.Contains("destroyed", ex.Message);
        }

        [Fact]
        public void HasOnADestroyedEntityThrows() {
            var e = W.NewEntity<Default>();
            e.Destroy();
            Assert.ThrowsAny<InvalidOperationException>(() => e.Has<Pos>());
        }

        [Fact]
        public void DeletingAComponentOfAnotherEntityInsideAStrictQueryThrows() {
            var a = W.NewEntity<Default>();
            a.Set(new Pos { X = 1 });
            var b = W.NewEntity<Default>();
            b.Set(new Pos { X = 2 });
            var thrown = 0;
            foreach (var e in W.With<Pos>()) {
                var other = e == a ? b : a;
                try { other.Delete<Pos>(); } catch (InvalidOperationException) { thrown++; }
            }
            Assert.Equal(2, thrown);
        }
    }
}
#endif
