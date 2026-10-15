#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System;
using System.Runtime.CompilerServices;
using FFS.Libraries.StaticPack;
using static System.Runtime.CompilerServices.MethodImplOptions;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {

    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public abstract partial class World {
        #if ENABLE_IL2CPP
        [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
        #endif
        public readonly ref partial struct WorldQuery<TFilter> where TFilter : struct, IQueryFilter {

            /// <summary>
            /// Counts all entities matching this query's filter. Builds the full query bitmask and sums
            /// the popcount of each matched block. This is a full scan, not O(1).
            /// </summary>
            /// <param name="entities">Which entity lifecycle state to count (enabled, disabled, or any).</param>
            /// <param name="clusters">Optional cluster filter. If provided, only entities in these clusters are counted.</param>
            /// <returns>Total number of matching entities.</returns>
            [MethodImpl(AggressiveInlining)]
            public int Count(EntityStatusType entities = EntityStatusType.Enabled,
                                     ReadOnlySpan<ushort> clusters = default) {
                var count = 0;
                var def = QueryDefOf(Filter);
                if (Prepare(def, clusters, entities, true, out var queryData, out var firstGlobalBlockIdx)) {
                    while (firstGlobalBlockIdx >= 0) {
                        ref var cache = ref queryData.Blocks[firstGlobalBlockIdx];
                        count += cache.EntitiesMask.PopCnt();
                        firstGlobalBlockIdx = cache.NextGlobalBlock;
                    }

                    Dispose(def, entities, true, queryData);
                }

                return count;
            }

            /// <summary>
            /// Counts all entities matching this query's filter. Builds the full query bitmask and sums
            /// the popcount of each matched block. This is a full scan, not O(1).
            /// </summary>
            /// <param name="entities">Which entity lifecycle state to count (enabled, disabled, or any).</param>
            /// <param name="chunks">Chunks filter.</param>
            /// <returns>Total number of matching entities.</returns>
            [MethodImpl(AggressiveInlining)]
            public int Count(ReadOnlySpan<uint> chunks, EntityStatusType entities = EntityStatusType.Enabled) {
                var count = 0;
                var def = QueryDefOf(Filter);
                if (Prepare(def, chunks, entities, true, out var queryData, out var firstGlobalBlockIdx)) {
                    while (firstGlobalBlockIdx >= 0) {
                        ref var cache = ref queryData.Blocks[firstGlobalBlockIdx];
                        count += cache.EntitiesMask.PopCnt();
                        firstGlobalBlockIdx = cache.NextGlobalBlock;
                    }

                    Dispose(def, entities, true, queryData);
                }

                return count;
            }

            /// <summary>
            /// Destroys all entities matching this query's filter in a single batch operation.
            /// More efficient than iterating and destroying one by one. All matched entities are
            /// removed from the world, their components' OnDelete hooks are called, and their
            /// storage is released.
            /// </summary>
            /// <param name="entities">Which entity lifecycle state to destroy (enabled, disabled, or any).</param>
            /// <param name="mode">Query mode: Strict (default) blocks hooks from modifying filtered types on non-current entities that belong to the iteration snapshot (entities created mid-iteration or not matching the filter remain freely mutable); Flexible additionally tolerates entity-level destroy/disable/enable on other snapshot entities.</param>
            /// <param name="clusters">Optional cluster filter. If provided, only entities in these clusters are destroyed.</param>
            /// <param name="withDisabledClusters">If <c>true</c>, also includes entities in disabled (unloaded) clusters.</param>
            [MethodImpl(AggressiveInlining)]
            public void BatchDestroy(EntityStatusType entities = EntityStatusType.Enabled,
                                     QueryMode mode = QueryMode.Strict,
                                     ReadOnlySpan<ushort> clusters = default,
                                     bool withDisabledClusters = false) {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, clusters, entities, strict, out var queryData, out var firstGlobalBlockIdx, withDisabledClusters)) {
                    Data.Instance.DestroyEntitiesBatch(queryData, firstGlobalBlockIdx);
                    Dispose(def, entities, strict, queryData);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void BatchDestroyInternal(HookReason reason,
                                               EntityStatusType entities = EntityStatusType.Enabled,
                                               QueryMode mode = QueryMode.Strict,
                                               ReadOnlySpan<ushort> clusters = default,
                                               bool withDisabledClusters = false) {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, clusters, entities, strict, out var queryData, out var firstGlobalBlockIdx, withDisabledClusters)) {
                    Data.Instance.DestroyEntitiesBatch(queryData, firstGlobalBlockIdx, reason);
                    Dispose(def, entities, strict, queryData);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal bool BatchDestroyInternal(HookReason reason,
                                               ReadOnlySpan<uint> chunks,
                                               EntityStatusType entities = EntityStatusType.Enabled,
                                               QueryMode mode = QueryMode.Strict) {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, chunks, entities, strict, out var queryData, out var firstGlobalBlockIdx)) {
                    Data.Instance.DestroyEntitiesBatch(queryData, firstGlobalBlockIdx, reason);
                    Dispose(def, entities, strict, queryData);
                    return true;
                }

                return false;
            }

            /// <inheritdoc cref="BatchDestroy(EntityStatusType, QueryMode, ReadOnlySpan{ushort}, bool)"/>
            [MethodImpl(AggressiveInlining)]
            public void BatchDestroy(ReadOnlySpan<uint> chunks,
                                     EntityStatusType entities = EntityStatusType.Enabled,
                                     QueryMode mode = QueryMode.Strict) {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, chunks, entities, strict, out var queryData, out var firstGlobalBlockIdx)) {
                    Data.Instance.DestroyEntitiesBatch(queryData, firstGlobalBlockIdx);
                    Dispose(def, entities, strict, queryData);
                }
            }

            /// <summary>
            /// Unloads all entities matching this query's filter in a single batch operation.
            /// More efficient than iterating and unloading one by one. All matched entities have
            /// their components and tags removed via OnDelete hooks, and their loaded state is cleared,
            /// but the entities remain alive (active mask, disabled mask, and versions are preserved).
            /// </summary>
            /// <param name="entities">Which entity lifecycle state to unload (enabled, disabled, or any).</param>
            /// <param name="mode">Query mode: Strict (default) blocks hooks from modifying filtered types on non-current entities that belong to the iteration snapshot (entities created mid-iteration or not matching the filter remain freely mutable); Flexible additionally tolerates entity-level destroy/disable/enable on other snapshot entities.</param>
            /// <param name="clusters">Optional cluster filter. If provided, only entities in these clusters are unloaded.</param>
            /// <param name="withDisabledClusters">If <c>true</c>, also includes entities in disabled (unloaded) clusters.</param>
            [MethodImpl(AggressiveInlining)]
            public void BatchUnload(EntityStatusType entities = EntityStatusType.Enabled,
                                    QueryMode mode = QueryMode.Strict,
                                    ReadOnlySpan<ushort> clusters = default,
                                    bool withDisabledClusters = false) {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, clusters, entities, strict, out var queryData, out var firstGlobalBlockIdx, withDisabledClusters)) {
                    Data.Instance.UnloadEntitiesBatch(queryData, firstGlobalBlockIdx);
                    Dispose(def, entities, strict, queryData);
                }
            }

            /// <inheritdoc cref="BatchUnload(EntityStatusType, QueryMode, ReadOnlySpan{ushort}, bool)"/>
            [MethodImpl(AggressiveInlining)]
            public void BatchUnload(ReadOnlySpan<uint> chunks,
                                    EntityStatusType entities = EntityStatusType.Enabled,
                                    QueryMode mode = QueryMode.Strict) {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, chunks, entities, strict, out var queryData, out var firstGlobalBlockIdx)) {
                    Data.Instance.UnloadEntitiesBatch(queryData, firstGlobalBlockIdx);
                    Dispose(def, entities, strict, queryData);
                }
            }

            /// <summary>
            /// Removes components from all entities matching this query's filter in a single batch.
            /// OnDelete hooks are triggered for each entity that had the component.
            /// Returns <c>this</c> for method chaining.
            /// <para>Available with 1–5 type parameters.</para>
            /// </summary>
            /// <param name="entities">Which entity lifecycle state to target.</param>
            /// <param name="mode">Query mode: Strict (default) blocks hooks from modifying filtered types on non-current entities that belong to the iteration snapshot (entities created mid-iteration or not matching the filter remain freely mutable); Flexible additionally tolerates entity-level destroy/disable/enable on other snapshot entities.</param>
            /// <param name="clusters">Optional cluster filter.</param>
            /// <returns>This query instance for chaining.</returns>
            #if NET5_0_OR_GREATER
            [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
            #endif
            [MethodImpl(AggressiveInlining)]
            public WorldQuery<TFilter> BatchDelete<T1>(EntityStatusType entities = EntityStatusType.Enabled,
                                                       QueryMode mode = QueryMode.Strict,

                                                       ReadOnlySpan<ushort> clusters = default)
                where T1 : struct, IComponentOrTag {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, clusters, entities, strict, out var queryData, out var firstGlobalBlockIdx)) {
                    Components<T1>.Instance.BatchDelete(queryData, firstGlobalBlockIdx);
                    Dispose(def, entities, strict, queryData);
                }

                return this;
            }

            /// <inheritdoc cref="BatchDelete{T1}(EntityStatusType, QueryMode, ReadOnlySpan{ushort})"/>
            #if NET5_0_OR_GREATER
            [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Type metadata is preserved by the registration path.")]
            #endif
            [MethodImpl(AggressiveInlining)]
            public WorldQuery<TFilter> BatchDelete<T1>(EntityStatusType entities, ReadOnlySpan<uint> chunks,
                                                       QueryMode mode = QueryMode.Strict)
                where T1 : struct, IComponentOrTag {
                var strict = mode == QueryMode.Strict;
                var def = QueryDefOf(Filter);
                if (Prepare(def, chunks, entities, strict, out var queryData, out var firstGlobalBlockIdx)) {
                    Components<T1>.Instance.BatchDelete(queryData, firstGlobalBlockIdx);
                    Dispose(def, entities, strict, queryData);
                }

                return this;
            }

            [MethodImpl(AggressiveInlining)]
            internal void WriteEntitySnapshotData(ref BinaryPackWriter writer,
                                                  CustomSnapshotEntityDataWriter snapshotDataEntityWriter,
                                                  SnapshotWriteParams snapshotParams,
                                                  ReadOnlySpan<uint> chunks,
                                                  EntityStatusType entities) {
                var def = QueryDefOf(Filter);
                if (Prepare(def, chunks, entities, true, out var queryData, out var firstGlobalBlockIdx)) {
                    var entity = new Entity();
                    ref var eid = ref entity.IdWithOffset;

                    #if !NET6_0_OR_GREATER
                    var deBruijn = Utils.DeBruijn;
                    #endif

                    while (firstGlobalBlockIdx >= 0) {
                        ref var cache = ref queryData.Blocks[firstGlobalBlockIdx];
                        ref var entitiesMask = ref cache.EntitiesMask;
                        var mask = entitiesMask;
                        var chunkBlockEntityId = (uint)(firstGlobalBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT) + Const.ENTITY_ID_OFFSET;
                        firstGlobalBlockIdx = cache.NextGlobalBlock;

                        var runStarts = mask & ~(mask << 1);
                        var runEnds = mask & ~(mask >> 1);
                        do {
                            #if NET6_0_OR_GREATER
                            var runStart = (byte)System.Numerics.BitOperations.TrailingZeroCount(runStarts);
                            var runEnd = (byte)System.Numerics.BitOperations.TrailingZeroCount(runEnds);
                            #else
                            var runStart = deBruijn[(uint)(((runStarts & (ulong)-(long)runStarts) * 0x37E84A99DAE458FUL) >> 58)];
                            var runEnd = deBruijn[(uint)(((runEnds & (ulong)-(long)runEnds) * 0x37E84A99DAE458FUL) >> 58)];
                            #endif
                            runStarts &= runStarts - 1UL;
                            runEnds &= runEnds - 1UL;
                            eid = chunkBlockEntityId + runStart;
                            var eidEnd = chunkBlockEntityId + runEnd;
                            while (eid <= eidEnd) {
                                snapshotDataEntityWriter(ref writer, entity, snapshotParams);
                                eid++;
                            }
                        } while (runStarts != 0);
                    }
                    Dispose(def, entities, true, queryData);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void ReadEntitySnapshotData(ref BinaryPackReader reader,
                                                 CustomSnapshotEntityDataReader snapshotDataEntityReader,
                                                 ushort version,
                                                 SnapshotReadParams snapshotParams,
                                                 ReadOnlySpan<uint> chunks,
                                                 EntityStatusType entities) {
                var def = QueryDefOf(Filter);
                if (Prepare(def, chunks, entities, true, out var queryData, out var firstGlobalBlockIdx)) {
                    var entity = new Entity();
                    ref var eid = ref entity.IdWithOffset;

                    #if !NET6_0_OR_GREATER
                    var deBruijn = Utils.DeBruijn;
                    #endif

                    while (firstGlobalBlockIdx >= 0) {
                        ref var cache = ref queryData.Blocks[firstGlobalBlockIdx];
                        ref var entitiesMask = ref cache.EntitiesMask;
                        var mask = entitiesMask;
                        var chunkBlockEntityId = (uint)(firstGlobalBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT) + Const.ENTITY_ID_OFFSET;
                        firstGlobalBlockIdx = cache.NextGlobalBlock;

                        var runStarts = mask & ~(mask << 1);
                        var runEnds = mask & ~(mask >> 1);
                        do {
                            #if NET6_0_OR_GREATER
                            var runStart = (byte)System.Numerics.BitOperations.TrailingZeroCount(runStarts);
                            var runEnd = (byte)System.Numerics.BitOperations.TrailingZeroCount(runEnds);
                            #else
                            var runStart = deBruijn[(uint)(((runStarts & (ulong)-(long)runStarts) * 0x37E84A99DAE458FUL) >> 58)];
                            var runEnd = deBruijn[(uint)(((runEnds & (ulong)-(long)runEnds) * 0x37E84A99DAE458FUL) >> 58)];
                            #endif
                            runStarts &= runStarts - 1UL;
                            runEnds &= runEnds - 1UL;
                            eid = chunkBlockEntityId + runStart;
                            var eidEnd = chunkBlockEntityId + runEnd;
                            while (eid <= eidEnd) {
                                snapshotDataEntityReader(ref reader, entity, version, snapshotParams);
                                eid++;
                            }
                        } while (runStarts != 0);
                    }
                    Dispose(def, entities, true, queryData);
                }
            }
        }
    }
}