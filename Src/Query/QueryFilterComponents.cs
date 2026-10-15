#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace FFS.Libraries.StaticEcs {

    #region ALL
    /// <summary>
    /// Query filter that requires an entity to have ALL specified enabled component types.
    /// Entities missing any of the listed components, or having them in a disabled state, are excluded.
    /// <para>
    /// This is the most common filter. Use in query type parameters to select entities with a specific
    /// set of components: <c>world.Query&lt;All&lt;Position, Velocity&gt;&gt;()</c> iterates only
    /// entities that have both <c>Position</c> AND <c>Velocity</c> enabled.
    /// </para>
    /// <para>
    /// Available with 1–8 type parameters. For single-component queries, <c>All&lt;T&gt;</c> is valid.
    /// Combine with other filters via query type composition:
    /// <c>world.Query&lt;All&lt;Pos, Vel&gt;, None&lt;Frozen&gt;&gt;()</c>.
    /// </para>
    /// <para>
    /// During strict-mode iteration, deleting or disabling any of the listed component types on other
    /// entities that belong to the iteration snapshot is forbidden (asserted in debug mode) because it
    /// would invalidate the iteration bitmask. Entities outside the snapshot — created during the loop
    /// or not matching the filter — are not blocked.
    /// </para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0> : IQueryFilter
        where T0 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0, T1> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T1>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0, T1, T2> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T1>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T2>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0, T1, T2, T3> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T1>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T2>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T3>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T1>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T2>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T3>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T4>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4, T5> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T5>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T5>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T1>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T2>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T3>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T4>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T5>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4, T5, T6> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T5>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T6>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T5>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T6>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T1>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T2>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T3>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T4>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T5>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T6>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4, T5, T6, T7> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag
        where T7 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T5>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T6>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value
                         & World<TWorld>.Components<T7>.Instance.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T5>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T6>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & World<TWorld>.Components<T7>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T1>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T2>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T3>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T4>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T5>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T6>.Instance.BlockDeleteDisable(val);
            World<TWorld>.Components<T7>.Instance.BlockDeleteDisable(val);
        }
        #endif

    }
    #endregion

    #region NONE
    /// <summary>
    /// Query filter that excludes entities having ANY of the specified enabled component types.
    /// An entity passes this filter only if it does NOT have any of the listed components in an enabled state.
    /// <para>
    /// Use for exclusion patterns: <c>world.Query&lt;All&lt;Position&gt;, None&lt;Frozen&gt;&gt;()</c>
    /// iterates entities with Position but without an enabled Frozen component.
    /// </para>
    /// <para>
    /// During strict-mode iteration, adding or enabling any of the listed component types on other
    /// entities that belong to the iteration snapshot is forbidden (asserted in debug mode) because it
    /// would invalidate the exclusion bitmask. Entities outside the snapshot — created during the loop
    /// or not matching the filter — are not blocked.
    /// </para>
    /// <para>Available with 1–8 type parameters.</para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0> : IQueryFilter
        where T0 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
        }
        #endif

    }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0, T1> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T1>.Instance.BlockAddEnable(val);
        }
        #endif

    }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0, T1, T2> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T1>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T2>.Instance.BlockAddEnable(val);
        }
        #endif

    }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0, T1, T2, T3> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T1>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T2>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T3>.Instance.BlockAddEnable(val);
        }
        #endif

    }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T1>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T2>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T3>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T4>.Instance.BlockAddEnable(val);
        }
        #endif

    }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4, T5> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T5>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T5>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T1>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T2>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T3>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T4>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T5>.Instance.BlockAddEnable(val);
        }
        #endif

    }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4, T5, T6> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T5>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T6>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T5>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T6>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T1>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T2>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T3>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T4>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T5>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T6>.Instance.BlockAddEnable(val);
        }
        #endif

    }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    #if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4, T5, T6, T7> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag
        where T7 : struct, IComponentOrTag {

        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T1>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T2>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T3>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T4>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T5>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T6>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value
                         & ~World<TWorld>.Components<T7>.Instance.HeuristicChunks[chunkIdx].FullBlocks.Value;
        }

        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return ~World<TWorld>.Components<T0>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T1>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T2>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T3>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T4>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T5>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T6>.Instance.EnabledMask(segmentIdx, segmentBlockIdx)
                            & ~World<TWorld>.Components<T7>.Instance.EnabledMask(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        [MethodImpl(AggressiveInlining)]
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            World<TWorld>.Components<T0>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T1>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T2>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T3>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T4>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T5>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T6>.Instance.BlockAddEnable(val);
            World<TWorld>.Components<T7>.Instance.BlockAddEnable(val);
        }
        #endif

    }
    #endregion

}