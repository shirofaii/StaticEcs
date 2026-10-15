using Xunit;

namespace FFS.Libraries.StaticEcs.Tests {
    public class HookAndCopyTests : WorldTest {
        [Fact]
        public void HooksRunOnAddOverwriteDeleteAndDestroy() {
            var e = W.NewEntity<Default>();
            e.Set(new Hooked { V = 1 });
            e.Add<Hooked>();
            e.Set(new Hooked { V = 2 });
            e.Delete<Hooked>();
            e.Set(new Hooked { V = 3 });
            e.Destroy();
            Assert.Equal(new[] { "add 1", "delete 1 Default", "add 2", "delete 2 Default", "add 3", "delete 3 Default" }, Hooked.Calls);
        }

        [Fact]
        public void WorldDestroyRunsDeleteHooksWithItsReason() {
            W.NewEntity<Default>().Set(new Hooked { V = 5 });
            W.Destroy();
            Assert.Equal(new[] { "add 5", "delete 5 WorldDestroy" }, Hooked.Calls);
            Create();
        }

        [Fact]
        public void CopyAndMoveSelectedComponentsAndTags() {
            var a = W.NewEntity<Default>();
            var b = W.NewEntity<Default>();
            a.Set(new Pos { X = 4 });
            a.Set(new Vel { V = 6 });
            a.Set<Red>();

            Assert.True(a.CopyTo<Pos>(b));
            Assert.Equal(4, b.Ref<Pos>().X);
            Assert.True(a.Has<Pos>());
            Assert.False(b.Has<Vel>());

            Assert.True(a.MoveTo<Vel>(b));
            Assert.False(a.Has<Vel>());
            Assert.Equal(6, b.Ref<Vel>().V);

            a.CopyTo<Red>(b);
            Assert.True(b.Has<Red>());
            Assert.True(a.Has<Red>());
            a.MoveTo<Red>(b);
            Assert.False(a.Has<Red>());
            Assert.True(b.Has<Red>());
        }

        [Fact]
        public void CloneCopiesComponentsTagsAndDisabledState() {
            var a = W.NewEntity<Default>();
            a.Set(new Pos { X = 3 });
            a.Set(new Hp { Value = 8 });
            a.Disable<Hp>();
            a.Set<Blue>();
            var c = a.Clone();
            Assert.NotEqual(a.ID, c.ID);
            Assert.Equal(3, c.Ref<Pos>().X);
            Assert.True(c.HasDisabled<Hp>());
            Assert.Equal(8, c.Read<Hp>().Value);
            Assert.True(c.Has<Blue>());
            c.Ref<Pos>().X = 10;
            Assert.Equal(3, a.Ref<Pos>().X);
        }

        [Fact]
        public void WholeEntityCopyToAndMoveTo() {
            var a = W.NewEntity<Default>();
            var b = W.NewEntity<Default>();
            a.Set(new Pos { X = 1 });
            a.Set<Green>();
            b.Set(new Pos { X = 9 });
            a.CopyTo(b);
            Assert.Equal(1, b.Ref<Pos>().X);
            Assert.True(b.Has<Green>());

            var c = W.NewEntity<Default>();
            b.MoveTo(c);
            Assert.True(b.IsDestroyed);
            Assert.Equal(1, c.Ref<Pos>().X);
            Assert.True(c.Has<Green>());
        }
    }
}
