#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {

    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public abstract partial class World<TWorld> {
        #region PREPARE AND DISPOSE
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [MethodImpl(NoInlining)]
        internal static bool Prepare(QueryDef filter, ReadOnlySpan<ushort> clusters, EntityStatusType entities,
                              bool strict, out QueryData queryData, out int firstGlobalBlockIdx, bool withDisabledClusters = false) {

            ref var world = ref Data.Instance;

            clusters = world.GetActiveClustersIfEmpty(clusters);
            queryData = default;
            BlockMaskCache[] filteredBlocks = null;

            #if !NET6_0_OR_GREATER
            var deBruijn = Utils.DeBruijn;
            #endif

            var previousGlobalBlockIdx = -1;
            firstGlobalBlockIdx = -1;

            for (var i = 0; i < clusters.Length; i++) {
                var clusterIdx = clusters[i];
                ref var cluster = ref world.Clusters[clusterIdx];
                if (!withDisabledClusters && cluster.Disabled) {
                    continue;
                }

                for (uint chunkMapIdx = 0; chunkMapIdx < cluster.LoadedChunksCount; chunkMapIdx++) {
                    var chunkIdx = cluster.LoadedChunks[chunkMapIdx];
                    var chunkMask = world.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
                    chunkMask &= FilterChunk(filter, chunkIdx);

                    if (chunkMask != 0) {
                        var segmentIdx = uint.MaxValue;

                        ulong[] worldMasks = null;

                        do {
                            #if NET6_0_OR_GREATER
                            var chunkBlockIdx = (uint)System.Numerics.BitOperations.TrailingZeroCount(chunkMask);
                            #else
                            var chunkBlockIdx = (uint)deBruijn[(uint)(((chunkMask & (ulong)-(long)chunkMask) * 0x37E84A99DAE458FUL) >> 58)];
                            #endif
                            chunkMask &= chunkMask - 1;
                            var globalBlockIdx = chunkBlockIdx + (chunkIdx << Const.BLOCKS_IN_CHUNK_SHIFT);

                            var curSegmentIdx = (chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT) + (chunkBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT);
                            if (curSegmentIdx != segmentIdx) {
                                segmentIdx = curSegmentIdx;
                                worldMasks = world.EntitiesSegments[segmentIdx].Masks;
                                LoadSegments(filter, segmentIdx);
                            }

                            var blockIdx = (byte)(chunkBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);
                            var disabledBlockIdx = blockIdx + Const.BLOCKS_IN_SEGMENT;
                            var loadedBlockIdx = disabledBlockIdx + Const.BLOCKS_IN_SEGMENT;

                            var entitiesMask = entities switch {
                                EntityStatusType.Enabled => worldMasks[loadedBlockIdx] & worldMasks[blockIdx] & ~worldMasks[disabledBlockIdx],
                                EntityStatusType.Disabled => worldMasks[loadedBlockIdx] & worldMasks[disabledBlockIdx],
                                _ => worldMasks[loadedBlockIdx] & worldMasks[blockIdx]
                            };
                            entitiesMask &= FilterBlock(filter, segmentIdx, blockIdx);

                            if (entitiesMask != 0) {
                                if (previousGlobalBlockIdx >= 0) {
                                    filteredBlocks[previousGlobalBlockIdx].NextGlobalBlock = (int)globalBlockIdx;
                                }
                                else {
                                    #if FFS_ECS_DEBUG
                                    const int block = 1;
                                    var queryMode = strict ? (byte)1 : (byte)2;
                                    AssertSameQueryMode(WorldTypeName, queryMode);
                                    world.QueryMode = queryMode;
                                    #endif

                                    queryData = world.PushCurrentQuery();

                                    if (!strict) {
                                        world.PushQueryDataForDestroy(queryData);

                                        switch (entities) {
                                            case EntityStatusType.Enabled: world.PushQueryDataForDisable(queryData); break;
                                            case EntityStatusType.Disabled: world.PushQueryDataForEnable(queryData); break;
                                        }
                                    }
                                    #if FFS_ECS_DEBUG
                                    else {
                                        world.BlockDestroy(block);

                                        switch (entities) {
                                            case EntityStatusType.Enabled: world.BlockDisable(block); break;
                                            case EntityStatusType.Disabled: world.BlockEnable(block); break;
                                        }
                                    }
                                    BlockQuery(filter, block);
                                    #endif

                                    filteredBlocks = queryData.Blocks;
                                    firstGlobalBlockIdx = (int)globalBlockIdx;
                                }

                                filteredBlocks[globalBlockIdx].EntitiesMask = entitiesMask;
                                filteredBlocks[globalBlockIdx].NextGlobalBlock = -1;
                                previousGlobalBlockIdx = (int)globalBlockIdx;
                            }
                        } while (chunkMask != 0);
                    }
                }
            }

            return filteredBlocks != null;
        }
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [MethodImpl(NoInlining)]
        internal static bool Prepare(QueryDef filter, ReadOnlySpan<uint> chunks, EntityStatusType entities,
                              bool strict, out QueryData queryData, out int firstGlobalBlockIdx) {

            ref var world = ref Data.Instance;

            queryData = default;
            BlockMaskCache[] filteredBlocks = null;

            #if !NET6_0_OR_GREATER
            var deBruijn = Utils.DeBruijn;
            #endif

            var previousGlobalBlockIdx = -1;
            firstGlobalBlockIdx = -1;

            for (uint chunkMapIdx = 0; chunkMapIdx < chunks.Length; chunkMapIdx++) {
                var chunkIdx = chunks[(int)chunkMapIdx];
                var chunkMask = world.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
                chunkMask &= FilterChunk(filter, chunkIdx);

                if (chunkMask != 0) {
                    var segmentIdx = uint.MaxValue;

                    ulong[] worldMasks = null;

                    do {
                        #if NET6_0_OR_GREATER
                        var chunkBlockIdx = (uint)System.Numerics.BitOperations.TrailingZeroCount(chunkMask);
                        #else
                        var chunkBlockIdx = (uint)deBruijn[(uint)(((chunkMask & (ulong)-(long)chunkMask) * 0x37E84A99DAE458FUL) >> 58)];
                        #endif
                        chunkMask &= chunkMask - 1;
                        var globalBlockIdx = chunkBlockIdx + (chunkIdx << Const.BLOCKS_IN_CHUNK_SHIFT);

                        var curSegmentIdx = (chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT) + (chunkBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT);
                        if (curSegmentIdx != segmentIdx) {
                            segmentIdx = curSegmentIdx;
                            worldMasks = world.EntitiesSegments[segmentIdx].Masks;
                            LoadSegments(filter, segmentIdx);
                        }

                        var blockIdx = (byte)(chunkBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);
                        var disabledBlockIdx = blockIdx + Const.BLOCKS_IN_SEGMENT;
                        var loadedBlockIdx = disabledBlockIdx + Const.BLOCKS_IN_SEGMENT;

                        var entitiesMask = entities switch {
                            EntityStatusType.Enabled => worldMasks[loadedBlockIdx] & worldMasks[blockIdx] & ~worldMasks[disabledBlockIdx],
                            EntityStatusType.Disabled => worldMasks[loadedBlockIdx] & worldMasks[disabledBlockIdx],
                            _ => worldMasks[loadedBlockIdx] & worldMasks[blockIdx]
                        };
                        entitiesMask &= FilterBlock(filter, segmentIdx, blockIdx);

                        if (entitiesMask != 0) {
                            if (previousGlobalBlockIdx >= 0) {
                                filteredBlocks[previousGlobalBlockIdx].NextGlobalBlock = (int)globalBlockIdx;
                            }
                            else {
                                #if FFS_ECS_DEBUG
                                const int block = 1;
                                var queryMode = strict ? (byte)1 : (byte)2;
                                AssertSameQueryMode(WorldTypeName, queryMode);
                                world.QueryMode = queryMode;
                                #endif

                                queryData = world.PushCurrentQuery();

                                if (!strict) {
                                    world.PushQueryDataForDestroy(queryData);

                                    switch (entities) {
                                        case EntityStatusType.Enabled: world.PushQueryDataForDisable(queryData); break;
                                        case EntityStatusType.Disabled: world.PushQueryDataForEnable(queryData); break;
                                    }
                                }
                                #if FFS_ECS_DEBUG
                                else {
                                    world.BlockDestroy(block);

                                    switch (entities) {
                                        case EntityStatusType.Enabled: world.BlockDisable(block); break;
                                        case EntityStatusType.Disabled: world.BlockEnable(block); break;
                                    }
                                }
                                BlockQuery(filter, block);
                                #endif

                                filteredBlocks = queryData.Blocks;
                                firstGlobalBlockIdx = (int)globalBlockIdx;
                            }

                            filteredBlocks[globalBlockIdx].EntitiesMask = entitiesMask;
                            filteredBlocks[globalBlockIdx].NextGlobalBlock = -1;
                            previousGlobalBlockIdx = (int)globalBlockIdx;
                        }
                    } while (chunkMask != 0);
                }
            }

            return filteredBlocks != null;
        }
            

        [MethodImpl(AggressiveInlining)]
        internal static void Dispose(QueryDef filter, EntityStatusType entities, bool strict, QueryData queryData) {
            ref var world = ref Data.Instance;

            world.PopCurrentQuery(queryData);
            #if FFS_ECS_DEBUG
            const int unblock = -1;
            #endif

            if (!strict) {
                world.PopQueryDataForDestroy();

                switch (entities) {
                    case EntityStatusType.Enabled: world.PopQueryDataForDisable(); break;
                    case EntityStatusType.Disabled: world.PopQueryDataForEnable(); break;
                }
            }
            #if FFS_ECS_DEBUG
            else {
                world.BlockDestroy(unblock);

                switch (entities) {
                    case EntityStatusType.Enabled: world.BlockDisable(unblock); break;
                    case EntityStatusType.Disabled: world.BlockEnable(unblock); break;
                }
            }

            BlockQuery(filter, unblock);
            if (world.QueryDataCount == 0) {
                world.QueryMode = 0;
            }
            #endif
        }
        #endregion

        #region FIND FIRST
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        [MethodImpl(NoInlining)]
        internal static bool FindFirst(QueryDef filter, ReadOnlySpan<ushort> clusters, EntityStatusType entities, out Entity entity, bool assertSingle) {
            ref var world = ref Data.Instance;
            clusters = world.GetActiveClustersIfEmpty(clusters);
            entity = default;

            #if !NET6_0_OR_GREATER
            var deBruijn = Utils.DeBruijn;
            #endif

            #if FFS_ECS_DEBUG
            var found = false;
            #endif

            for (var i = 0; i < clusters.Length; i++) {
                var clusterIdx = clusters[i];
                ref var cluster = ref world.Clusters[clusterIdx];
                if (cluster.Disabled) continue;

                for (uint chunkMapIdx = 0; chunkMapIdx < cluster.LoadedChunksCount; chunkMapIdx++) {
                    var chunkIdx = cluster.LoadedChunks[chunkMapIdx];
                    var chunkMask = world.HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
                    chunkMask &= FilterChunk(filter, chunkIdx);

                    if (chunkMask != 0) {
                        var segmentIdx = uint.MaxValue;
                        ulong[] worldMasks = null;

                        do {
                            #if NET6_0_OR_GREATER
                            var chunkBlockIdx = (uint)System.Numerics.BitOperations.TrailingZeroCount(chunkMask);
                            #else
                            var chunkBlockIdx = (uint)deBruijn[(uint)(((chunkMask & (ulong)-(long)chunkMask) * 0x37E84A99DAE458FUL) >> 58)];
                            #endif
                            chunkMask &= chunkMask - 1;
                            var globalBlockIdx = chunkBlockIdx + (chunkIdx << Const.BLOCKS_IN_CHUNK_SHIFT);

                            var curSegmentIdx = (chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT) + (chunkBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT);
                            if (curSegmentIdx != segmentIdx) {
                                segmentIdx = curSegmentIdx;
                                worldMasks = world.EntitiesSegments[segmentIdx].Masks;
                                LoadSegments(filter, segmentIdx);
                            }

                            var blockIdx = (byte)(chunkBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);
                            var disabledBlockIdx = blockIdx + Const.BLOCKS_IN_SEGMENT;
                            var loadedBlockIdx = disabledBlockIdx + Const.BLOCKS_IN_SEGMENT;

                            var entitiesMask = entities switch {
                                EntityStatusType.Enabled => worldMasks[loadedBlockIdx] & worldMasks[blockIdx] & ~worldMasks[disabledBlockIdx],
                                EntityStatusType.Disabled => worldMasks[loadedBlockIdx] & worldMasks[disabledBlockIdx],
                                _ => worldMasks[loadedBlockIdx] & worldMasks[blockIdx]
                            };
                            entitiesMask &= FilterBlock(filter, segmentIdx, blockIdx);

                            if (entitiesMask != 0) {
                                #if FFS_ECS_DEBUG
                                if (found) {
                                    throw new StaticEcsException($"World<{typeof(TWorld)}> query One() found more than one entity");
                                }
                                #endif

                                #if NET6_0_OR_GREATER
                                var bitIdx = (uint)System.Numerics.BitOperations.TrailingZeroCount(entitiesMask);
                                #else
                                var bitIdx = (uint)deBruijn[(uint)(((entitiesMask & (ulong)-(long)entitiesMask) * 0x37E84A99DAE458FUL) >> 58)];
                                #endif
                                entity.IdWithOffset = (globalBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT) + Const.ENTITY_ID_OFFSET + bitIdx;

                                #if FFS_ECS_DEBUG
                                if (assertSingle) {
                                    if ((entitiesMask & (entitiesMask - 1)) != 0) {
                                        throw new StaticEcsException($"World<{typeof(TWorld)}> query One() found more than one entity");
                                    }
                                    found = true;
                                    continue;
                                }
                                #endif
                                return true;
                            }
                        } while (chunkMask != 0);
                    }
                }
            }

            #if FFS_ECS_DEBUG
            return found;
            #else
            return false;
            #endif
        }
        #endregion

    }
}