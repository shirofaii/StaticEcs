using System;
using System.Collections.Generic;
using FFS.Libraries.StaticPack;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace FFS.Libraries.StaticEcs.Tests {
    public abstract class W : World { }

    public struct Pos : IComponent { public int X; }
    public struct Vel : IComponent { public int V; }
    public struct Hp : IComponent { public int Value; }
    public struct Tracked : IComponent { public int V; }

    public struct Name : IComponent {
        public string Value;
        public void Write(ref BinaryPackWriter writer, World.Entity self) => writer.WriteString16(Value);
        public void Read(ref BinaryPackReader reader, World.Entity self, byte version) => Value = reader.ReadString16();
    }

    public struct Hooked : IComponent {
        public int V;
        public static readonly List<string> Calls = new();
        public void OnAdd(World.Entity self) => Calls.Add($"add {V}");
        public void OnDelete(World.Entity self, HookReason reason) => Calls.Add($"delete {V} {reason}");
    }

    public struct Red : ITag { }
    public struct Blue : ITag { }
    public struct Green : ITag { }

    public abstract class WorldTest : IDisposable {
        protected WorldTest() {
            if (W.Status != WorldStatus.NotCreated) W.Destroy();
            Hooked.Calls.Clear();
            Create();
        }

        protected static void Create() {
            W.Create(WorldConfig.Default());
            W.Types().RegisterAll();
            W.Initialize();
        }

        public void Dispose() {
            if (W.Status != WorldStatus.NotCreated) W.Destroy();
        }

        protected static int Count<TFilter>() where TFilter : struct, IQueryFilter {
            var n = 0;
            foreach (var _ in W.Query<TFilter>().Entities()) n++;
            return n;
        }

        protected static int CountAny<TFilter>() where TFilter : struct, IQueryFilter {
            var n = 0;
            foreach (var _ in W.Query<TFilter>().Entities(EntityStatusType.Any)) n++;
            return n;
        }
    }
}
