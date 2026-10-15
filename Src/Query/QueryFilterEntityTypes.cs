#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

#pragma warning disable CS0649

namespace FFS.Libraries.StaticEcs {

    #region IS
    /// <summary>
    /// Query filter that matches only entities whose entity type is <typeparamref name="T0"/>.
    /// </summary>
    /// <typeparam name="T0">Required entity type.</typeparam>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct EntityIs<T0> : IQueryFilter
        where T0 : struct, IEntityType {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk(uint chunkIdx) {
            return World.Data.Instance.EntityTypes[World.EntityTypeInfo<T0>.Instance.Id].HeuristicChunks[chunkIdx].Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities(uint segmentIdx, byte segmentBlockIdx) {
            return ulong.MaxValue;
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Assert() {
            World.AssertEntityTypeIsRegistered(typeof(T0).Name, World.EntityTypeInfo<T0>.Instance.Id);
        }

        [MethodImpl(AggressiveInlining)]
        public void Block(int val) { }
        #endif

    }
    #endregion

}