#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System;
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
    public sealed class ComponentMasks {
        internal const int Existed = 0;
        internal const int Added = 1;
        internal const int AddedToNewSegment = 2;

        internal readonly ushort Id;
        internal readonly bool IsTag;
        internal readonly bool HasDisable;
        internal readonly byte SegmentMaskLen;
        internal readonly ulong IdMask;
        internal readonly ulong IdMaskInv;
        internal readonly ushort IdDiv;

        internal HeuristicChunk[] HeuristicChunks;
        internal ulong[][] Segments;
        internal ulong[][] Pool;
        internal int PoolCount;
        internal ulong[][] WorldMask;
        internal ushort WorldMaskLen;

        #if FFS_ECS_DEBUG
        internal int BlockerDelete;
        internal int BlockerAdd;
        internal int BlockerDisable;
        internal int BlockerEnable;
        #endif

        internal ComponentMasks(ushort id, bool isTag, bool hasDisable) {
            Id = id;
            IsTag = isTag;
            HasDisable = hasDisable;
            SegmentMaskLen = (byte) (hasDisable ? Const.BLOCKS_IN_SEGMENT * 2 : Const.BLOCKS_IN_SEGMENT);
            IdDiv = (ushort) (id >> Const.U64_SHIFT);
            IdMask = 1UL << (id & Const.U64_MASK);
            IdMaskInv = ~IdMask;
        }

        internal void Initialize(uint chunksCapacity, ulong[][] worldMask, ushort worldMaskLen) {
            WorldMask = worldMask;
            WorldMaskLen = worldMaskLen;
            var segmentsCapacity = chunksCapacity * Const.SEGMENTS_IN_CHUNK;
            HeuristicChunks = new HeuristicChunk[chunksCapacity];
            Segments = new ulong[segmentsCapacity][];
            Pool = new ulong[segmentsCapacity][];
            PoolCount = 0;
        }

        internal void Resize(uint chunksCapacity, ulong[][] worldMask) {
            var segmentsCapacity = chunksCapacity * Const.SEGMENTS_IN_CHUNK;
            WorldMask = worldMask;
            Array.Resize(ref HeuristicChunks, (int) chunksCapacity);
            Array.Resize(ref Segments, (int) segmentsCapacity);
            Array.Resize(ref Pool, (int) segmentsCapacity);
        }

        [MethodImpl(AggressiveInlining)]
        internal bool Has(uint entityId) {
            var segment = Segments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT];
            return segment != null && (segment[(entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK] & (1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK))) != 0;
        }

        [MethodImpl(AggressiveInlining)]
        internal bool HasDisabled(uint entityId) {
            if (!HasDisable) return false;
            var segment = Segments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT];
            return segment != null && (segment[((entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK) + Const.BLOCKS_IN_SEGMENT] & (1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK))) != 0;
        }

        [MethodImpl(AggressiveInlining)]
        internal bool HasEnabled(uint entityId) {
            if (!HasDisable) return Has(entityId);
            var segment = Segments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT];
            var blockIdx = (entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK;
            return segment != null && (segment[blockIdx] & ~segment[blockIdx + Const.BLOCKS_IN_SEGMENT] & (1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK))) != 0;
        }

        internal ToggleResult Disable(uint entityId) {
            var segment = Segments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT];
            var blockIdx = (entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK;
            var bit = 1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK);
            if (segment == null || (segment[blockIdx] & bit) == 0) {
                return ToggleResult.MissingComponent;
            }

            ref var disabled = ref segment[blockIdx + Const.BLOCKS_IN_SEGMENT];
            if ((disabled & bit) != 0) {
                return ToggleResult.Unchanged;
            }

            disabled |= bit;
            return ToggleResult.Changed;
        }

        internal ToggleResult Enable(uint entityId) {
            var segment = Segments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT];
            var blockIdx = (entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK;
            var bit = 1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK);
            if (segment == null || (segment[blockIdx] & bit) == 0) {
                return ToggleResult.MissingComponent;
            }

            ref var disabled = ref segment[blockIdx + Const.BLOCKS_IN_SEGMENT];
            if ((disabled & bit) == 0) {
                return ToggleResult.Unchanged;
            }

            disabled &= ~bit;
            return ToggleResult.Changed;
        }

        [MethodImpl(AggressiveInlining)]
        internal int Add(uint entityId) {
            var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
            var segment = Segments[segmentIdx];
            var result = Added;
            if (segment == null) {
                segment = CreateSegment(segmentIdx);
                result = AddedToNewSegment;
            }

            ref var mask = ref segment[(entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK];
            var bit = 1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK);
            if ((mask & bit) != 0) {
                return Existed;
            }

            if (mask == 0 || (mask | bit) == ulong.MaxValue) {
                AddTransition(entityId, ref mask, bit);
            }
            else {
                mask |= bit;
            }

            return result;
        }

        [MethodImpl(NoInlining)]
        private void AddTransition(uint entityId, ref ulong mask, ulong bit) {
            var chunkBlockIdx = (byte) ((entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK);
            ref var heuristic = ref HeuristicChunks[entityId >> Const.ENTITIES_IN_CHUNK_SHIFT];
            if (mask == 0) {
                heuristic.NotEmptyBlocks.SetBit(chunkBlockIdx);
            }

            mask |= bit;
            if (mask == ulong.MaxValue) {
                heuristic.FullBlocks.SetBit(chunkBlockIdx);
            }
        }

        [MethodImpl(AggressiveInlining)]
        internal ulong[] Remove(uint entityId) {
            var segment = Segments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT];
            if (segment == null) {
                return null;
            }

            var blockIdx = (entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK;
            ref var mask = ref segment[blockIdx];
            var bit = 1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK);
            if ((mask & bit) == 0) {
                return null;
            }

            if (mask == ulong.MaxValue) {
                HeuristicChunks[entityId >> Const.ENTITIES_IN_CHUNK_SHIFT].FullBlocks.ClearBit((byte) ((entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK));
            }

            mask &= ~bit;
            if (HasDisable) {
                segment[blockIdx + Const.BLOCKS_IN_SEGMENT] &= ~bit;
            }

            return segment;
        }

        [MethodImpl(AggressiveInlining)]
        internal bool ReleaseIfEmpty(uint entityId, ulong[] segment) {
            var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
            return segment[(entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK] == 0 && Segments[segmentIdx] == segment && ReleaseEmptyBlock(entityId, segmentIdx, segment);
        }

        [MethodImpl(NoInlining)]
        private bool ReleaseEmptyBlock(uint entityId, uint segmentIdx, ulong[] segment) {
            var chunkIdx = entityId >> Const.ENTITIES_IN_CHUNK_SHIFT;
            var notEmptyBlocks = HeuristicChunks[chunkIdx].NotEmptyBlocks.ClearBit((byte) ((entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK));
            if ((notEmptyBlocks & Const.DataMasks[segmentIdx & Const.SEGMENTS_IN_CHUNK_MASK]) != 0UL) {
                return false;
            }

            FreeSegment(chunkIdx, segmentIdx, segment);
            return true;
        }

        [MethodImpl(NoInlining)]
        internal ulong[] CreateSegment(uint segmentIdx) {
            WorldMask[segmentIdx >> Const.SEGMENTS_IN_CHUNK_SHIFT][(segmentIdx & Const.SEGMENTS_IN_CHUNK_MASK) * WorldMaskLen + IdDiv] |= IdMask;

            ulong[] segment;
            if (PoolCount > 0) {
                var poolIdx = --PoolCount;
                segment = Pool[poolIdx];
                Pool[poolIdx] = null;
            }
            else {
                segment = new ulong[SegmentMaskLen];
            }

            Segments[segmentIdx] = segment;
            return segment;
        }

        [MethodImpl(NoInlining)]
        internal void FreeSegment(uint chunkIdx, uint segmentIdx, ulong[] segment) {
            WorldMask[chunkIdx][(segmentIdx & Const.SEGMENTS_IN_CHUNK_MASK) * WorldMaskLen + IdDiv] &= IdMaskInv;
            Segments[segmentIdx] = null;
            Pool[PoolCount++] = segment;
        }

        internal bool ReleaseSegment(int segmentIdx) {
            ref var segment = ref Segments[segmentIdx];
            if (segment == null) return false;
            Array.Clear(segment, 0, SegmentMaskLen);
            Pool[PoolCount++] = segment;
            segment = null;
            return true;
        }

        internal void HardReset() {
            for (var i = 0; i < Segments.Length; i++) {
                ReleaseSegment(i);
            }

            Array.Clear(HeuristicChunks, 0, HeuristicChunks.Length);
        }

        [MethodImpl(AggressiveInlining)]
        internal ulong EnabledMask(uint segmentIdx, int segmentBlockIdx) {
            var segment = Segments[segmentIdx];
            if (segment == null) return 0UL;
            return HasDisable
                ? segment[segmentBlockIdx] & ~segment[segmentBlockIdx + Const.BLOCKS_IN_SEGMENT]
                : segment[segmentBlockIdx];
        }

        [MethodImpl(AggressiveInlining)]
        internal ulong DisabledMask(uint segmentIdx, int segmentBlockIdx) {
            if (!HasDisable) return 0UL;
            var segment = Segments[segmentIdx];
            return segment != null ? segment[segmentBlockIdx + Const.BLOCKS_IN_SEGMENT] : 0UL;
        }

        [MethodImpl(AggressiveInlining)]
        internal ulong AnyMask(uint segmentIdx, int segmentBlockIdx) {
            var segment = Segments[segmentIdx];
            return segment != null ? segment[segmentBlockIdx] : 0UL;
        }

        internal uint CalculateCount() {
            uint count = 0;
            for (var chunkIdx = 0; chunkIdx < HeuristicChunks.Length; chunkIdx++) {
                ref var heuristic = ref HeuristicChunks[chunkIdx];
                var fullBlocks = heuristic.FullBlocks.Value;
                count += (uint) (fullBlocks.PopCnt() * Const.ENTITIES_IN_BLOCK);

                var partialMask = heuristic.NotEmptyBlocks.Value & ~fullBlocks;
                while (partialMask != 0) {
                    var chunkBlockIdx = Utils.PopLsb(ref partialMask);
                    var segmentIdx = (uint) ((chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT) + (chunkBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT));
                    count += (uint) Segments[segmentIdx][chunkBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK].PopCnt();
                }
            }

            return count;
        }

        internal int CalculateCapacity() {
            var count = PoolCount;
            for (var segmentIdx = 0; segmentIdx < Segments.Length; segmentIdx++) {
                if (Segments[segmentIdx] != null) {
                    count++;
                }
            }

            return count * Const.ENTITIES_IN_SEGMENT;
        }

        #if FFS_ECS_DEBUG
        internal void BlockDelete(int val) {
            BlockerDelete += val;
        }

        internal void BlockDeleteDisable(int val) {
            BlockerDelete += val;
            if (!IsTag) BlockerDisable += val;
        }

        internal void BlockDeleteEnable(int val) {
            BlockerDelete += val;
            if (!IsTag) BlockerEnable += val;
        }

        internal void BlockAdd(int val) {
            BlockerAdd += val;
        }

        internal void BlockAddEnable(int val) {
            BlockerAdd += val;
            if (!IsTag) BlockerEnable += val;
        }
        #endif
    }
}
