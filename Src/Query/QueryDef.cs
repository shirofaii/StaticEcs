#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System;
using System.Collections.Generic;
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
        internal readonly IQueryFilter Custom;
        internal readonly int Generation;
        internal readonly ulong[][] WithSegments;
        internal readonly ulong[][] WithoutSegments;

        internal QueryDef(ComponentMasks[] with, ComponentMasks[] without, int entityType, IQueryFilter custom, int generation) {
            With = with;
            Without = without;
            WithSegments = new ulong[with.Length][];
            WithoutSegments = new ulong[without.Length][];
            EntityType = entityType;
            Custom = custom;
            Generation = generation;
        }

        private static readonly HashSet<Type> AllTypes = new() {
            typeof(All<>), typeof(All<,>), typeof(All<,,>), typeof(All<,,,>), typeof(All<,,,,>), typeof(All<,,,,,>), typeof(All<,,,,,,>), typeof(All<,,,,,,,>)
        };

        private static readonly HashSet<Type> NoneTypes = new() {
            typeof(None<>), typeof(None<,>), typeof(None<,,>), typeof(None<,,,>), typeof(None<,,,,>), typeof(None<,,,,,>), typeof(None<,,,,,,>), typeof(None<,,,,,,,>)
        };

        internal static QueryDef Build(Type filterType, IQueryFilter filter, Dictionary<Type, ComponentMasks> masksByType, Dictionary<Type, byte> entityTypeIds, int generation) {
            var with = new List<ComponentMasks>();
            var without = new List<ComponentMasks>();
            var entityType = -1;
            if (!Collect(filterType, with, without, ref entityType, masksByType, entityTypeIds)) {
                return new QueryDef(Array.Empty<ComponentMasks>(), Array.Empty<ComponentMasks>(), -1, filter, generation);
            }

            return new QueryDef(with.ToArray(), without.ToArray(), entityType, null, generation);
        }

        private static bool Collect(Type type, List<ComponentMasks> with, List<ComponentMasks> without, ref int entityType,
                                    Dictionary<Type, ComponentMasks> masksByType, Dictionary<Type, byte> entityTypeIds) {
            if (type == typeof(Nothing)) {
                return true;
            }

            if (!type.IsGenericType) {
                return false;
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
                return true;
            }

            if (definition == typeof(EntityIs<>)) {
                if (entityType >= 0 || !entityTypeIds.TryGetValue(args[0], out var id)) {
                    return false;
                }
                entityType = id;
                return true;
            }

            if (definition == typeof(And<,>)) {
                return Collect(args[0], with, without, ref entityType, masksByType, entityTypeIds)
                    && Collect(args[1], with, without, ref entityType, masksByType, entityTypeIds);
            }

            return false;
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
            if (def.Custom == null) {
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

            if (def.Custom != null) {
                mask &= def.Custom.FilterChunk(chunkIdx);
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

            if (def.Custom != null) {
                mask &= def.Custom.FilterEntities(segmentIdx, segmentBlockIdx);
            }

            return mask;
        }

        #if FFS_ECS_DEBUG
        internal static void BlockQuery(QueryDef def, int val) {
            foreach (var masks in def.With) masks.BlockDeleteDisable(val);
            foreach (var masks in def.Without) masks.BlockAddEnable(val);
            def.Custom?.Block(val);
        }
        #endif
    }
}
