using System;
using System.Collections.Generic;
using FFS.Libraries.StaticPack;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace FFS.Libraries.StaticEcs.Tests {
    public struct TW : IWorldType { }
    public abstract class W : World<TW> { }

    public struct Pos : IComponent { public int X; }
    public struct Vel : IComponent { public int V; }
    public struct Hp : IComponent, IDisableable { public int Value; }
    public struct Tracked : IComponent { public int V; }

    public struct Name : IComponent {
        public string Value;
        public void Write<TWorld>(ref BinaryPackWriter writer, World<TWorld>.Entity self) where TWorld : struct, IWorldType => writer.WriteString16(Value);
        public void Read<TWorld>(ref BinaryPackReader reader, World<TWorld>.Entity self, byte version, bool disabled) where TWorld : struct, IWorldType => Value = reader.ReadString16();
    }

    public struct Hooked : IComponent {
        public int V;
        public static readonly List<string> Calls = new();
        public void OnAdd<TWorld>(World<TWorld>.Entity self) where TWorld : struct, IWorldType => Calls.Add($"add {V}");
        public void OnDelete<TWorld>(World<TWorld>.Entity self, HookReason reason) where TWorld : struct, IWorldType => Calls.Add($"delete {V} {reason}");
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
