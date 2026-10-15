#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {

    /// <summary>
    /// A no-op query filter that matches all entities unconditionally. All <see cref="IQueryFilter"/>
    /// methods are empty — no chunk or entity bitmask bits are ever cleared.
    /// <para>
    /// Used as the default filter when <see cref="World{TWorld}.Query()"/> is called without
    /// type parameters. Iterates over every alive entity (subject to <see cref="EntityStatusType"/>).
    /// </para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct Nothing : IQueryFilter {
        /// <inheritdoc/>
        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType { return ulong.MaxValue; }

        /// <inheritdoc/>
        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType { return ulong.MaxValue; }

        #if FFS_ECS_DEBUG
        /// <inheritdoc/>
        public void Assert<TWorld>() where TWorld : struct, IWorldType { }

        /// <inheritdoc/>
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType { }
        #endif
    }

    /// <summary>
    /// Composite query filter that combines two <see cref="IQueryFilter"/> instances with AND semantics.
    /// During filtering, each sub-filter's <see cref="IQueryFilter.FilterChunk{TWorld}"/> and
    /// <see cref="IQueryFilter.FilterEntities{TWorld}"/> are called sequentially, progressively
    /// narrowing the bitmask. An entity matches only if ALL sub-filters accept it.
    /// <para>
    /// This is the most commonly used composite filter, created implicitly by
    /// <see cref="World{TWorld}.Query{TFilter0, TFilter1}"/> or explicitly via
    /// <see cref="And.By{TFilter0,TFilter1}"/>.
    /// Example: <c>And&lt;All&lt;Position&gt;, None&lt;Dead&gt;&gt;</c> matches entities
    /// that have Position AND do not have Dead.
    /// </para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public struct And<TFilter0, TFilter1> : IQueryFilter
        where TFilter0 : struct, IQueryFilter
        where TFilter1 : struct, IQueryFilter {
        private TFilter0 _filter0;
        private TFilter1 _filter1;

        /// <summary>
        /// Initializes the composite filter with two sub-filter instances.
        /// </summary>
        public And(TFilter0 filter0,
                    TFilter1 filter1) {
            _filter0 = filter0;
            _filter1 = filter1;
        }

        /// <inheritdoc/>
        [MethodImpl(AggressiveInlining)]
        public ulong FilterChunk<TWorld>(uint chunkIdx) where TWorld : struct, IWorldType {
            return _filter0.FilterChunk<TWorld>(chunkIdx)
                 & _filter1.FilterChunk<TWorld>(chunkIdx);
        }

        /// <inheritdoc/>
        [MethodImpl(AggressiveInlining)]
        public ulong FilterEntities<TWorld>(uint segmentIdx, byte segmentBlockIdx) where TWorld : struct, IWorldType {
            return _filter0.FilterEntities<TWorld>(segmentIdx, segmentBlockIdx)
                 & _filter1.FilterEntities<TWorld>(segmentIdx, segmentBlockIdx);
        }

        #if FFS_ECS_DEBUG
        /// <inheritdoc/>
        public void Block<TWorld>(int val) where TWorld : struct, IWorldType {
            _filter0.Block<TWorld>(val);
            _filter1.Block<TWorld>(val);
        }
        #endif
    }

}