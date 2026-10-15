#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
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
    public sealed class QueryDef {
        internal readonly ComponentMasks[] With;
        internal readonly ComponentMasks[] Without;
        internal readonly int EntityType;
        internal readonly ICustomQueryFilter[] Customs;
        internal readonly int Generation;
        internal readonly ulong[][] WithSegments;
        internal readonly ulong[][] WithoutSegments;

        internal QueryDef(ComponentMasks[] with, ComponentMasks[] without, int entityType, ICustomQueryFilter[] customs, int generation) {
            With = with;
            Without = without;
            WithSegments = new ulong[with.Length][];
            WithoutSegments = new ulong[without.Length][];
            EntityType = entityType;
            Customs = customs;
            Generation = generation;
        }

        private static readonly HashSet<Type> AllTypes = new() {
            typeof(All<>), typeof(All<,>), typeof(All<,,>), typeof(All<,,,>), typeof(All<,,,,>), typeof(All<,,,,,>), typeof(All<,,,,,,>), typeof(All<,,,,,,,>)
        };

        private static readonly HashSet<Type> NoneTypes = new() {
            typeof(None<>), typeof(None<,>), typeof(None<,,>), typeof(None<,,,>), typeof(None<,,,,>), typeof(None<,,,,,>), typeof(None<,,,,,,>), typeof(None<,,,,,,,>)
        };

        internal static QueryDef Build(Type filterType, object filter, Dictionary<Type, ComponentMasks> masksByType, Dictionary<Type, byte> entityTypeIds, int generation) {
            var with = new List<ComponentMasks>();
            var without = new List<ComponentMasks>();
            var customs = new List<ICustomQueryFilter>();
            var entityType = -1;
            Collect(filterType, filter, with, without, customs, ref entityType, masksByType, entityTypeIds);
            return new QueryDef(with.ToArray(), without.ToArray(), entityType, customs.ToArray(), generation);
        }

        private static void Collect(Type type, object filter, List<ComponentMasks> with, List<ComponentMasks> without, List<ICustomQueryFilter> customs,
                                    ref int entityType, Dictionary<Type, ComponentMasks> masksByType, Dictionary<Type, byte> entityTypeIds) {
            if (type == typeof(Nothing)) {
                return;
            }

            if (filter is ICustomQueryFilter custom) {
                customs.Add(custom);
                return;
            }

            if (!type.IsGenericType) {
                throw new StaticEcsException($"Query filter {type.Name} must implement {nameof(ICustomQueryFilter)}");
            }

            var definition = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();
            if (AllTypes.Contains(definition) || NoneTypes.Contains(definition)) {
                var target = AllTypes.Contains(definition) ? with : without;
                foreach (var arg in args) {
                    if (!masksByType.TryGetValue(arg, out var masks)) {
                        throw new StaticEcsException($"Query uses {arg.Name}, which is not registered");
                    }
                    target.Add(masks);
                }
                return;
            }

            if (definition == typeof(EntityIs<>)) {
                if (!entityTypeIds.TryGetValue(args[0], out var id)) {
                    throw new StaticEcsException($"Query uses entity type {args[0].Name}, which is not registered");
                }
                if (entityType >= 0 && entityType != id) {
                    throw new StaticEcsException("A query can match only one entity type");
                }
                entityType = id;
                return;
            }

            if (definition == typeof(And<,>)) {
                Collect(args[0], type.GetField(nameof(And<Nothing, Nothing>.Filter0), BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(filter),
                    with, without, customs, ref entityType, masksByType, entityTypeIds);
                Collect(args[1], type.GetField(nameof(And<Nothing, Nothing>.Filter1), BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(filter),
                    with, without, customs, ref entityType, masksByType, entityTypeIds);
                return;
            }

            throw new StaticEcsException($"Query filter {type.Name} must implement {nameof(ICustomQueryFilter)}");
        }
    }

    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public abstract partial class World {
        internal static class QueryDefCache<TFilter> where TFilter : struct, IQueryFilter {
            internal static QueryDef Def;
        }

        [MethodImpl(AggressiveInlining)]
        internal static QueryDef QueryDefOf<TFilter>(TFilter filter) where TFilter : struct, IQueryFilter {
            var def = QueryDefCache<TFilter>.Def;
            if (def != null && def.Generation == Data.Instance.Generation) {
                return def;
            }

            return BuildQueryDef<TFilter>(filter);
        }

        [MethodImpl(NoInlining)]
        private static QueryDef BuildQueryDef<TFilter>(TFilter filter) where TFilter : struct, IQueryFilter {
            ref var world = ref Data.Instance;
            var def = QueryDef.Build(typeof(TFilter), filter, world.MasksByType, world.EntityTypeIdByType, world.Generation);
            if (def.Customs.Length == 0) {
                QueryDefCache<TFilter>.Def = def;
            }
            return def;
        }

        [MethodImpl(AggressiveInlining)]
        internal static ulong FilterChunk(QueryDef def, uint chunkIdx) {
            var mask = ulong.MaxValue;
            var with = def.With;
            for (var i = 0; i < with.Length; i++) {
                mask &= with[i].HeuristicChunks[chunkIdx].NotEmptyBlocks.Value;
            }

            var without = def.Without;
            for (var i = 0; i < without.Length; i++) {
                mask &= ~without[i].HeuristicChunks[chunkIdx].FullBlocks.Value;
            }

            if (def.EntityType >= 0) {
                mask &= Data.Instance.EntityTypes[def.EntityType].HeuristicChunks[chunkIdx].Value;
            }

            var customs = def.Customs;
            for (var i = 0; i < customs.Length; i++) {
                mask &= customs[i].FilterChunk(chunkIdx);
            }

            return mask;
        }

        [MethodImpl(AggressiveInlining)]
        internal static void LoadSegments(QueryDef def, uint segmentIdx) {
            var with = def.With;
            var withSegments = def.WithSegments;
            for (var i = 0; i < with.Length; i++) {
                withSegments[i] = with[i].Segments[segmentIdx];
            }

            var without = def.Without;
            var withoutSegments = def.WithoutSegments;
            for (var i = 0; i < without.Length; i++) {
                withoutSegments[i] = without[i].Segments[segmentIdx];
            }
        }

        [MethodImpl(AggressiveInlining)]
        internal static ulong FilterBlock(QueryDef def, uint segmentIdx, byte segmentBlockIdx) {
            var mask = ulong.MaxValue;
            var with = def.With;
            var withSegments = def.WithSegments;
            for (var i = 0; i < withSegments.Length; i++) {
                var segment = withSegments[i];
                if (segment == null) {
                    return 0UL;
                }
                mask &= with[i].HasDisable
                    ? segment[segmentBlockIdx] & ~segment[segmentBlockIdx + Const.BLOCKS_IN_SEGMENT]
                    : segment[segmentBlockIdx];
            }

            var without = def.Without;
            var withoutSegments = def.WithoutSegments;
            for (var i = 0; i < withoutSegments.Length; i++) {
                var segment = withoutSegments[i];
                if (segment != null) {
                    mask &= ~(without[i].HasDisable
                        ? segment[segmentBlockIdx] & ~segment[segmentBlockIdx + Const.BLOCKS_IN_SEGMENT]
                        : segment[segmentBlockIdx]);
                }
            }

            var customs = def.Customs;
            for (var i = 0; i < customs.Length; i++) {
                mask &= customs[i].FilterEntities(segmentIdx, segmentBlockIdx);
            }

            return mask;
        }

        internal static bool EntityMatches(QueryDef def, uint entityId) {
            var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
            if (def.EntityType >= 0 && Data.Instance.EntitiesSegments[segmentIdx].EntityType != def.EntityType) {
                return false;
            }

            LoadSegments(def, segmentIdx);
            var segmentBlockIdx = (byte) ((entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK);
            return (FilterBlock(def, segmentIdx, segmentBlockIdx) & (1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK))) != 0UL;
        }

        #if FFS_ECS_DEBUG
        internal static void BlockQuery(QueryDef def, int val) {
            foreach (var masks in def.With) masks.BlockDeleteDisable(val);
            foreach (var masks in def.Without) masks.BlockAddEnable(val);
            foreach (var custom in def.Customs) custom.Block(val);
        }
        #endif
    }
}
