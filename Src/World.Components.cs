#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FFS.Libraries.StaticPack;
using static System.Runtime.CompilerServices.MethodImplOptions;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {

    /// <summary>
    /// Marker interface for zero-size tag components. Tags carry no data — they serve as boolean flags
    /// on entities, tracked purely through presence bitmasks.
    /// </summary>
    public interface ITag : IComponentOrTag { }

    /// <summary>
    /// Marker interface for ECS data components. Components carry data (as opposed to <see cref="ITag"/> which is zero-size).
    /// Inherits hooks from <see cref="IComponentOrTag"/>.
    /// </summary>
    public interface IComponent : IComponentOrTag { }

    /// <summary>
    /// Base interface for all ECS types that can be attached to entities (components and tags).
    /// All methods have default (empty) implementations — override only the hooks you need.
    /// Hook detection uses reflection at static initialization time; the JIT eliminates branches
    /// for hooks that are not overridden, so there is zero runtime cost for unused hooks.
    /// </summary>
    public interface IComponentOrTag {
        /// <summary>
        /// Called immediately after this component is added to an entity or after the value is overwritten
        /// (in the <c>Set(entity, value)</c> overload). Use this hook to initialize dependent state,
        /// subscribe to events, or set up cross-component relationships.
        /// <para>
        /// For the value-overwrite case (<c>Set(entity, value)</c> when component already exists),
        /// <see cref="OnDelete"/> is called on the old value first, then the value is replaced,
        /// then this hook is called on the new value.
        /// </para>
        /// </summary>
        /// <param name="self">The entity this component was added to.</param>
        public void OnAdd(World.Entity self) {}

        /// <summary>
        /// Called when this component is about to be removed from an entity via <c>Delete</c>,
        /// or before being overwritten via <c>Set(entity, value)</c>. Use this hook to clean up
        /// resources, unsubscribe from events, or break cross-component relationships.
        /// <para>
        /// Also called during entity destruction for each component the entity has.
        /// </para>
        /// </summary>
        /// <param name="self">The entity this component is being removed from.</param>
        public void OnDelete(World.Entity self, HookReason reason) {}

        /// <summary>
        /// Custom copy logic invoked by <see cref="World.Components{T}.Copy"/>.
        /// When implemented, this replaces the default behavior (which is a bitwise copy + Add on destination).
        /// Use this for components that require deep copy, reference counting, or conditional copy logic.
        /// </summary>
        /// <param name="self">The source entity being copied from.</param>
        /// <param name="other">The destination entity being copied to.</param>
        public void CopyTo(World.Entity self, World.Entity other) {}

        /// <summary>
        /// Custom serialization hook for writing this component to a binary stream.
        /// <para>
        /// <b>Required</b> for entity-level serialization (<c>EntitiesSnapshot</c> via
        /// <c>CreateEntitiesSnapshotWriter</c>) when the component is <b>not</b> unmanaged.
        /// For unmanaged components without this hook, the component body is written via
        /// <c>Unsafe.WriteUnaligned</c> (raw bytes). If you implement one of <c>Write</c>/<c>Read</c>,
        /// you must implement both — the reader picks <c>Read</c> when present and falls back
        /// to raw <c>ReadUnaligned</c> only when neither hook is defined and the saved version matches.
        /// </para>
        /// <para>
        /// For chunk-level serialization (<c>WorldSnapshot</c>, <c>ClusterSnapshot</c>, <c>ChunkSnapshot</c>),
        /// unmanaged types can use an <see cref="IPackArrayStrategy{T}"/> (e.g. <c>UnmanagedPackArrayStrategy&lt;T&gt;</c>)
        /// for bulk memory copying instead of per-component hooks. If the strategy reports
        /// <c>IsUnmanaged() == true</c>, this hook is not called during chunk writes.
        /// Non-unmanaged types always use this hook in all serialization paths.
        /// </para>
        /// </summary>
        /// <param name="writer">Binary writer to serialize data into.</param>
        /// <param name="self">The entity this component belongs to.</param>
        public void Write(ref BinaryPackWriter writer, World.Entity self) {}

        /// <summary>
        /// Custom deserialization hook for reading this component from a binary stream.
        /// <para>
        /// <b>Required</b> for entity-level deserialization (<c>EntitiesSnapshot</c> via
        /// <c>LoadEntitiesSnapshot</c>) when the component is <b>not</b> unmanaged, and also
        /// for unmanaged components when migrating between non-matching <c>Version</c> values.
        /// For unmanaged components without this hook, the body is read via
        /// <c>Unsafe.ReadUnaligned</c> (raw bytes) — only when the saved version equals the current one.
        /// </para>
        /// <para>
        /// For chunk-level deserialization, unmanaged types with a matching version use
        /// <see cref="IPackArrayStrategy{T}"/> for bulk reads. However, when the serialized version
        /// differs from the current <see cref="ComponentTypeConfig{T}.Version"/>, this hook is called
        /// even for unmanaged types to enable data migration.
        /// Non-unmanaged types always use this hook in all deserialization paths.
        /// </para>
        /// </summary>
        /// <param name="reader">Binary reader to deserialize data from.</param>
        /// <param name="self">The entity this component is being loaded onto.</param>
        /// <param name="version">The version byte that was stored during serialization, for migration logic.</param>
        public void Read(ref BinaryPackReader reader, World.Entity self, byte version) {}
    }

    internal interface IComponentInternal {
        internal void OnInitialize();
    }

    internal interface IComponentHookOverride {
        internal bool HasOnAdd();
        internal bool HasOnDelete();
        internal bool HasCopyTo();
    }

    internal interface IComponentStrategyOverride {
        internal IPackArrayStrategy<T> ArrayPackStrategy<T>() where T : struct;
    }
    
    /// <summary>
    /// Configuration for registering a component type with <c>World.Types().Component&lt;T&gt;()</c>.
    /// Controls serialization identity (Guid), data versioning, and binary read/write strategy.
    /// </summary>
    /// <typeparam name="T">The component type being configured.</typeparam>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    #endif
    public readonly struct ComponentTypeConfig<T> where T : struct, IComponentOrTag {
        /// <summary>
        /// Stable identifier for this component type used during serialization.
        /// Serializer uses this Guid to match saved data to the correct component type
        /// even if the type is renamed or moved.
        /// Default is computed via <see cref="Utils.GuidFromAQN"/>.
        /// </summary>
        public readonly Guid? Guid;

        /// <summary>
        /// Schema version byte written alongside serialized component data.
        /// When the serialized version differs from the current version, the component's
        /// <see cref="IComponent.Read"/> hook is invoked for data migration instead of
        /// the default binary deserialization strategy.
        /// Increment this when changing the component's data layout to enable backward-compatible loading.
        /// </summary>
        public readonly byte? Version;

        /// <summary>
        /// When <c>false</c> (default), the framework manages component data lifecycle:
        /// new segments are pre-initialized with <see cref="DefaultValue"/>, and data is reset
        /// to <see cref="DefaultValue"/> upon deletion (if no <see cref="IComponent.OnDelete"/> hook).
        /// When <c>true</c>, the framework does not initialize or clear component data — the user
        /// is responsible for all data management. Useful for high-frequency unmanaged types.
        /// When <see cref="IComponent.OnDelete"/> is defined, the hook handles cleanup
        /// regardless of this flag.
        /// </summary>
        public readonly bool? NoDataLifecycle;

        /// <summary>
        /// Strategy for binary serialization of component arrays. Defaults to <see cref="StructPackArrayStrategy{T}"/>
        /// which performs direct memory copies for unmanaged types. Override with a custom strategy
        /// for types requiring specialized serialization.
        /// </summary>
        public readonly IPackArrayStrategy<T> ReadWriteStrategy;

        /// <summary>
        /// Default value for component data lifecycle. Used in two ways:
        /// (1) New storage segments are pre-initialized with this value, so <c>Add&lt;T&gt;()</c> returns it.
        /// (2) On deletion, data is reset to this value (if no <see cref="IComponent.OnDelete"/> hook).
        /// Only effective when <see cref="NoDataLifecycle"/> is <c>false</c> (default).
        /// </summary>
        public readonly T? DefaultValue;

        /// <summary>
        /// Creates a configuration for component type registration.
        /// </summary>
        /// <param name="guid">Stable serialization identifier. Default is computed via <see cref="Utils.GuidFromAQN"/>.</param>
        /// <param name="version">Schema version for data migration. Default is 0.</param>
        /// <param name="noDataLifecycle">When <c>true</c>, disables framework data lifecycle (no init, no clear). Default is <c>false</c>.</param>
        /// <param name="readWriteStrategy">Custom binary serialization strategy. Default is <see cref="StructPackArrayStrategy{T}"/>.</param>
        /// <param name="defaultValue">Default value for initialization and deletion reset. Default is <c>default(T)</c>.</param>
        public ComponentTypeConfig(Guid? guid = null,
                                   byte? version = null,
                                   bool? noDataLifecycle = null,
                                   IPackArrayStrategy<T> readWriteStrategy = null,
                                   T? defaultValue = null) {
            Guid = guid;
            Version = version;
            NoDataLifecycle = noDataLifecycle;
            ReadWriteStrategy = readWriteStrategy;
            DefaultValue = defaultValue;
        }

        internal ComponentTypeConfig<T> MergeWith(ComponentTypeConfig<T> other) {
            return new ComponentTypeConfig<T>(
                guid: Guid ?? other.Guid,
                version: Version ?? other.Version,
                noDataLifecycle: NoDataLifecycle ?? other.NoDataLifecycle,
                readWriteStrategy: ReadWriteStrategy ?? other.ReadWriteStrategy,
                defaultValue: DefaultValue ?? other.DefaultValue
            );
        }

    }

    /// <summary>
    /// Configuration for tag type registration, specifying serialization and tracking options.
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    public readonly struct TagTypeConfig<T> where T : struct, IComponentOrTag {
        /// <inheritdoc cref="ComponentTypeConfig{T}.Guid"/>>
        public readonly Guid? Guid;

        public TagTypeConfig(Guid? guid = null) {
            Guid = guid;
        }

        public ComponentTypeConfig<T> AsComponentConfig => AsComponentConfigInternal(this);

        internal static ComponentTypeConfig<T> AsComponentConfigInternal(TagTypeConfig<T> config) {
            return new ComponentTypeConfig<T>(guid: config.Guid);
        }
    }

    public interface IComponentConfig<T> where T : struct, IComponentOrTag {
        ComponentTypeConfig<T> Config();
    }

    public interface ITagConfig<T> where T : struct, IComponentOrTag {
        TagTypeConfig<T> Config();
    }

    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public abstract partial class World {

        /// <summary>
        /// Static storage and operations for component type <typeparamref name="T"/> within this world.
        /// This struct holds all per-type data: component arrays (SoA layout), presence bitmasks,
        /// heuristic chunks for fast iteration, and query update lists.
        /// <para>
        /// Access via <c>Components&lt;T&gt;.Instance</c> for direct low-level operations,
        /// or use the higher-level <see cref="Entity"/> API (e.g., <c>entity.Add&lt;T&gt;()</c>).
        /// The Entity API delegates to these methods internally.
        /// </para>
        /// <para>
        /// Component data is stored in segments of 256 entries. Segments are allocated lazily on first access
        /// and returned to a pool when all entities in the segment lose this component.
        /// Presence is tracked via <c>ulong</c> bitmasks (64 entities per block, 4 blocks per segment),
        /// with a parallel disabled-mask for per-component enable/disable functionality.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The component type. Must be a struct implementing <see cref="IComponent"/>.</typeparam>
        #if ENABLE_IL2CPP
        [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
        [Il2CppEagerStaticClassConstruction]
        #endif
        [StructLayout(LayoutKind.Sequential)]
        public struct Components<
            #if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
            #endif
            T> where T : struct, IComponentOrTag {
            
            #if FFS_ECS_DEBUG
            internal static Components<T> instance;
            
            public static ref Components<T> Instance {
                get {
                    AssertWorldIsInitialized(ComponentsTypeName);
                    if (!instance.IsRegistered) {
                        throw new StaticEcsException($"Component {typeof(T).GenericName()} is not registered.");
                    }
                    return ref instance;
                }
            }
            #else
            /// <summary>
            /// Singleton instance of this component storage. All public methods operate on this instance.
            /// Initialized when <c>World.Types().Component&lt;T&gt;()</c> is called.
            /// </summary>
            public static Components<T> Instance;
            #endif

            /// <summary>
            /// Type-erased handle for this component type, providing access via <c>unsafe delegate*</c>
            /// function pointers without generic type parameters. Used by serialization, tooling,
            /// and reflection-driven code. See <see cref="ComponentsHandle"/>.
            /// </summary>
            public static ComponentsHandle Handle;

            /// <summary>
            /// Safe registration check that does not throw when the type is not yet registered.
            /// Returns <c>true</c> after <c>Types().Component&lt;T&gt;()</c> / <c>Tag&lt;T&gt;()</c> has been called
            /// for this world, and <c>false</c> before registration and after <c>World.Destroy()</c>.
            /// Unlike <see cref="Instance"/>, this property never asserts and can be called in any world state.
            /// </summary>
            public static bool IsTypeRegistered {
                [MethodImpl(AggressiveInlining)]
                #if FFS_ECS_DEBUG
                get => instance.IsRegistered;
                #else
                get => Instance.IsRegistered;
                #endif
            }

            #if UNITY_2022_1_OR_NEWER
            [UnityEngine.Scripting.Preserve]
            #endif
            [MethodImpl(NoInlining)]
            internal static void AutoRegister(bool isTag) {
                #if FFS_ECS_DEBUG
                if (instance.IsRegistered) {
                #else
                if (Instance.IsRegistered) {
                #endif
                    return;
                }
                var nonSerializable = typeof(INonSerializable).IsAssignableFrom(typeof(T));
                if (isTag) {
                    if (default(T) is ITagConfig<T> tagConfig) {
                        Data.Instance.RegisterComponentOrTagTypeInternal(tagConfig.Config().AsComponentConfig, true, nonSerializable, typeof(T).Name);
                        return;
                    }
                }
                else if (default(T) is IComponentConfig<T> componentConfig) {
                    Data.Instance.RegisterComponentOrTagTypeInternal(componentConfig.Config(), false, nonSerializable, typeof(T).Name);
                    return;
                }
                var typeName = typeof(T).Name;
                var guid = typeof(T).GuidFromAQN();
                #if FFS_ECS_DEBUG
                AssertGuidNotEmpty(WorldTypeName, guid, typeName);
                instance = new Components<T>(Data.Instance.ComponentPoolsCount, guid, 0, false, null, false, default, isTag, nonSerializable, typeName);
                #else
                Instance = new Components<T>(Data.Instance.ComponentPoolsCount, guid, 0, false, null, false, default, isTag, nonSerializable, typeName);
                #endif
                Handle = ComponentsHandle.Create<T>();
                Data.Instance.RegisterComponentPool(Handle, guid, nonSerializable, typeName);
            }

            internal ComponentMasks Masks;
            internal ulong[][] MaskSegments;
            internal T[][] ComponentSegments;

            /// <summary>
            /// Whether this storage represents a tag type (no data, no enable/disable, no change tracking).
            /// </summary>
            public readonly bool IsTag;

            /// <summary>
            /// When <c>true</c>, data of this component/tag type is excluded from all world snapshot
            /// serialization (Add/Set/Has/Query/lifecycle still work as usual).
            /// Set automatically when the component implements <see cref="INonSerializable"/>;
            /// for <see cref="Link{T}"/>, <see cref="Links{T}"/> and <see cref="Multi{TValue}"/>
            /// inherited from the user's <c>T</c>/<c>TValue</c>.
            /// </summary>
            public readonly bool NonSerializable;

            #if FFS_ECS_DEBUG
            /// <summary>
            /// Whether this component implements <see cref="IComponentInternal"/> (Multi&lt;&gt; / Links&lt;&gt;).
            /// Such components are handles whose <c>OnAdd</c> hook allocates fresh backing storage,
            /// so <c>Set</c> with a non-default value would alias or leak data.
            /// </summary>
            public readonly bool IsInternal;
            #endif

            internal readonly bool DataLifecycle;
            internal readonly bool HasOnAdd;
            internal readonly bool HasOnDelete;
            internal readonly T DefaultValue;

            internal T[][] _componentsPool;
            internal int _componentsPoolCount;
            internal readonly bool HasDefaultValue;
            
            /// <summary>
            /// Runtime-assigned numeric identifier for this component type within the world.
            /// Assigned sequentially during registration (starting from 0). Used internally for
            /// bitmask indexing in the chunk heuristic world mask and by <see cref="ComponentsHandle.DynamicId"/>.
            /// </summary>
            public readonly ushort DynamicId;

            internal readonly bool HasCopyTo;
            internal readonly bool HasWrite;
            internal readonly bool HasRead;
            internal readonly bool Unmanaged;

            private IPackArrayStrategy<T> _readWriteArrayStrategy;
            private IPackArrayStrategyResettable _resettableStrategy;

            /// <summary>
            /// Stable serialization identifier for this component type, as specified in
            /// <see cref="ComponentTypeConfig{T}.Guid"/>. Used by the serializer to match
            /// saved data to the correct component type across renames/refactors.
            /// </summary>
            public readonly Guid Guid;

            /// <summary>
            /// Schema version of this component type, as specified in <see cref="ComponentTypeConfig{T}.Version"/>.
            /// Written during serialization and compared during deserialization to trigger data migration
            /// via <see cref="IComponent.Read"/> when versions differ.
            /// </summary>
            public readonly byte Version;

            /// <summary>
            /// Whether this component type has been registered with the world via
            /// <c>Types().Component&lt;T&gt;()</c>. In debug mode always check this before using <see cref="Instance"/>
            /// </summary>
            public readonly bool IsRegistered;

            internal static string TypeName;

            #if FFS_ECS_DEBUG
            internal static readonly string ComponentsTypeName = $"{WorldTypeName}.Components<{typeof(T).GenericName()}>"; 
            
            #endif

            #region PUBLIC API
            /// <summary>
            /// Checks whether the entity has component <typeparamref name="T"/> (either enabled or disabled).
            /// This is a fast bitmask check with no side effects. Returns <c>false</c> if the entity's
            /// segment has not been allocated for this component type.
            /// <para>
            /// <b>Note:</b> Returns <c>true</c> for both enabled AND disabled components.
            /// Use <see cref="HasEnabled"/> to check only enabled components,
            /// or <see cref="HasDisabled"/> to check only disabled components.
            /// </para>
            /// </summary>
            /// <param name="entity">Entity to check. Must not be destroyed or unloaded.</param>
            /// <returns><c>true</c> if the entity has this component (in any state, including disabled).</returns>
            [MethodImpl(AggressiveInlining)]
            public readonly bool Has(Entity entity) {
                #if FFS_ECS_DEBUG
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                #endif
                return HasUnchecked(entity);
            }

            #if FFS_ECS_DEBUG
            [MethodImpl(NoInlining)]
            private readonly void AccessFailed(Entity entity, [CallerMemberName] string method = "") {
                AssertIsNotTag<T>(ComponentsTypeName, method);
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity, method);
                AssertEntityHasComponent<T>(ComponentsTypeName, entity, method);
            }
            #endif

            [MethodImpl(AggressiveInlining)]
            internal readonly bool HasUnchecked(Entity entity) {
                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                var segment = MaskSegments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT];
                return segment != null && (segment[(entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK] & (1UL << (int) (entityId & Const.ENTITIES_IN_BLOCK_MASK))) != 0;
            }

            /// <summary>
            /// Returns a mutable reference to component <typeparamref name="T"/> on the given entity.
            /// The entity MUST have this component (asserted in debug mode). Use <see cref="Has"/>
            /// to check first if unsure.
            /// <para>
            /// This is the fastest way to read/write component data — no presence check, no hooks,
            /// no tracking, just a direct array access via segment indexing.
            /// Does NOT mark the component as Changed. Use <see cref="Mut"/> for tracked mutable access.
            /// </para>
            /// </summary>
            /// <param name="entity">Entity that must have component <typeparamref name="T"/>.</param>
            /// <returns>A mutable reference to the stored component data.</returns>
            [MethodImpl(AggressiveInlining)]
            public readonly ref T Ref(Entity entity) {
                #if FFS_ECS_DEBUG
                if (IsTag || !HasUnchecked(entity) || !Data.Instance.EntityIsLoaded(entity)) AccessFailed(entity);
                #endif

                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                return ref ComponentSegments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT][entityId & Const.ENTITIES_IN_SEGMENT_MASK];
            }

            /// <summary>
            /// Returns a read-only reference to component <typeparamref name="T"/> on the given entity.
            /// Does NOT mark the component as changed. Use when you need to read component data
            /// without triggering change tracking.
            /// </summary>
            /// <param name="entity">Entity that must have component <typeparamref name="T"/>.</param>
            /// <returns>A read-only reference to the stored component data.</returns>
            [MethodImpl(AggressiveInlining)]
            public readonly ref readonly T Read(Entity entity) {
                #if FFS_ECS_DEBUG
                if (IsTag || !HasUnchecked(entity) || !Data.Instance.EntityIsLoaded(entity)) AccessFailed(entity);
                #endif

                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                return ref ComponentSegments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT][entityId & Const.ENTITIES_IN_SEGMENT_MASK];
            }

            /// <summary>
            /// Ensures entity has component <typeparamref name="T"/> and returns a mutable reference to it.
            /// If the component already exists, returns the existing value without modification and without
            /// triggering any hooks. If the component is new, default-initializes it, updates query caches,
            /// and triggers the <see cref="IComponent.OnAdd"/> hook.
            /// <para>
            /// This is the idiomatic "get-or-add" operation. Use when you want to ensure a component
            /// exists and then modify it via the returned reference:
            /// <c>ref var pos = ref Components&lt;Position&gt;.Instance.Add(entity); pos.X = 100;</c>
            /// </para>
            /// </summary>
            /// <param name="entity">Target entity. Must not be destroyed or unloaded.</param>
            /// <returns>A mutable reference to the component data (existing or newly created).</returns>
            [MethodImpl(AggressiveInlining)]
            public ref T Add(Entity entity) {
                #if FFS_ECS_DEBUG
                AssertIsNotTag<T>(ComponentsTypeName);
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                AssertNotBlockedByQuery(ComponentsTypeName, entity, Masks.BlockerAdd);
                #endif

                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
                var added = Masks.Add(entityId);
                if (added == ComponentMasks.AddedToNewSegment) {
                    ComponentSegments[segmentIdx] = NewDataSegment();
                }

                ref var component = ref ComponentSegments[segmentIdx][entityId & Const.ENTITIES_IN_SEGMENT_MASK];
                if (added == ComponentMasks.Existed) {
                    return ref component;
                }

                if (HasOnAdd) {
                    CallOnAdd(ref component, entity);
                }

                return ref component;
            }

            /// <summary>
            /// Ensures entity has component <typeparamref name="T"/> and reports whether it was newly added.
            /// Behaves identically to <see cref="Add(Entity)"/>: if the component already exists,
            /// returns existing data without modification or hooks. If new, default-initializes and triggers OnAdd.
            /// </summary>
            /// <param name="entity">Target entity. Must not be destroyed or unloaded.</param>
            /// <param name="isNew"><c>true</c> if the component was newly added; <c>false</c> if it already existed.</param>
            /// <returns>A mutable reference to the component data.</returns>
            [MethodImpl(AggressiveInlining)]
            public ref T Add(Entity entity, out bool isNew) {
                #if FFS_ECS_DEBUG
                AssertIsNotTag<T>(ComponentsTypeName);
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                AssertNotBlockedByQuery(ComponentsTypeName, entity, Masks.BlockerAdd);
                #endif

                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
                var added = Masks.Add(entityId);
                if (added == ComponentMasks.AddedToNewSegment) {
                    ComponentSegments[segmentIdx] = NewDataSegment();
                }

                ref var component = ref ComponentSegments[segmentIdx][entityId & Const.ENTITIES_IN_SEGMENT_MASK];
                isNew = added != ComponentMasks.Existed;
                if (!isNew) {
                    return ref component;
                }

                if (HasOnAdd) {
                    CallOnAdd(ref component, entity);
                }

                return ref component;
            }

            /// <summary>
            /// Sets component <typeparamref name="T"/> to the given value on the entity, always overwriting.
            /// <para>
            /// If the component already exists: calls <see cref="IComponent.OnDelete"/> on the old value,
            /// overwrites with <paramref name="value"/>, then calls <see cref="IComponent.OnAdd"/> on the new value.
            /// If the component is new: sets the value, updates query caches, and calls OnAdd.
            /// </para>
            /// <para>
            /// Unlike <see cref="Add(Entity)"/>, this overload always replaces the data, making it suitable
            /// for "set to this exact value" semantics rather than "ensure exists and modify".
            /// </para>
            /// </summary>
            /// <param name="entity">Target entity. Must not be destroyed or unloaded.</param>
            /// <param name="value">The value to store.</param>
            /// <param name="withOnAdd">If <c>true</c> (default), calls OnAdd hook if implemented.</param>
            [MethodImpl(AggressiveInlining)]
            public void Set(Entity entity, T value, bool withOnAdd = true) {
                #if FFS_ECS_DEBUG
                AssertIsNotTag<T>(ComponentsTypeName);
                AssertInternalSetValueIsDefault(value, ComponentsTypeName);
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                AssertNotBlockedByQuery(ComponentsTypeName, entity, Masks.BlockerAdd);
                #endif

                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
                var added = Masks.Add(entityId);
                if (added == ComponentMasks.AddedToNewSegment) {
                    ComponentSegments[segmentIdx] = NewDataSegment();
                }

                ref var component = ref ComponentSegments[segmentIdx][entityId & Const.ENTITIES_IN_SEGMENT_MASK];
                if (added == ComponentMasks.Existed && HasOnDelete) {
                    CallOnDelete(ref component, entity, HookReason.Default);
                }

                component = value;

                if (withOnAdd && HasOnAdd) {
                    CallOnAdd(ref component, entity);
                }
            }
            
            [MethodImpl(NoInlining)]
            private void CallOnAdd(ref T component, Entity entity) {
                component.OnAdd(entity);
            }

            /// <summary>
            /// Removes component <typeparamref name="T"/> from the entity.
            /// If the component is present: clears presence and disabled bitmasks, updates query caches,
            /// and optionally calls <see cref="IComponent.OnDelete"/> on the stored value.
            /// If the entity's segment becomes completely empty after deletion, the segment is returned to the pool.
            /// <para>
            /// The <paramref name="reason"/> parameter indicates why the component is being removed
            /// (<see cref="HookReason.Default"/> for explicit removal, <see crefHookReason.UnloadEntityad"/> for entity unloading,
            /// <see cref="HookReason.WorldDestroy"/> for world destruction) and is forwarded to the OnDelete hook.
            /// </para>
            /// </summary>
            /// <param name="entity">Target entity. Must not be destroyed or unloaded.</param>
            /// <param name="reason">The reason for deletion, forwarded to the OnDelete hook.</param>
            /// <returns><c>true</c> if the component was present and removed; <c>false</c> if absent.</returns>
            [MethodImpl(AggressiveInlining)]
            public bool Delete(Entity entity, HookReason reason = HookReason.Default, bool withOnDelete = true) {
                #if FFS_ECS_DEBUG
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                AssertNotBlockedByQuery(ComponentsTypeName, entity, Masks.BlockerDelete);
                #endif

                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                var maskSegment = Masks.Remove(entityId);
                if (maskSegment == null) {
                    return false;
                }

                var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
                if (withOnDelete && HasOnDelete) {
                    CallOnDelete(ref ComponentSegments[segmentIdx][entityId & Const.ENTITIES_IN_SEGMENT_MASK], entity, HookReason.Default);
                }
                else if (DataLifecycle) {
                    ComponentSegments[segmentIdx][entityId & Const.ENTITIES_IN_SEGMENT_MASK] = DefaultValue;
                }

                if (Masks.ReleaseIfEmpty(entityId, maskSegment) && !IsTag) {
                    FreeDataSegment(segmentIdx);
                }

                return true;
            }
            
            [MethodImpl(NoInlining)]
            private void CallOnDelete(ref T component, Entity entity, HookReason reason) {
                component.OnDelete(entity, reason);
            }

            /// <summary>
            /// Copies component <typeparamref name="T"/> from <paramref name="source"/> to <paramref name="destination"/>.
            /// If the source entity has this component:
            /// <list type="bullet">
            /// <item>If <see cref="IComponent.CopyTo"/> is implemented: delegates to that hook
            /// (allowing custom deep-copy logic, reference counting, etc.).
            /// <b>Note:</b> The CopyTo hook is responsible for managing the disabled state on the destination entity.
            /// The <paramref name="source"/> disabled state is passed via the <c>disabled</c> parameter.</item>
            /// <item>Otherwise: performs <c>Set(destination, source_value)</c> (bitwise copy).
            /// Also copies the disabled state automatically.</item>
            /// </list>
            /// </summary>
            /// <param name="source">Entity to copy from. Must not be destroyed or unloaded.</param>
            /// <param name="destination">Entity to copy to. Must not be destroyed or unloaded.</param>
            /// <returns><c>true</c> if the source had the component, and it was copied; <c>false</c> if source lacked it.</returns>
            [MethodImpl(AggressiveInlining)]
            public bool Copy(Entity source, Entity destination) {
                #if FFS_ECS_DEBUG
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, source);
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, destination);
                #endif
                
                var sourceEntityId = source.IdWithOffset - Const.ENTITY_ID_OFFSET;
                var segmentIdx = sourceEntityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
                var segmentBlockIdx = (byte) ((sourceEntityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK);
                var sourceBlockEntityMask = 1UL << (byte) (sourceEntityId & Const.ENTITIES_IN_BLOCK_MASK);

                var maskSegment = Masks.Segments[segmentIdx];
                if (maskSegment != null && (maskSegment[segmentBlockIdx] & sourceBlockEntityMask) != 0) {
                    if (IsTag) {
                        Set(destination);
                    } else {
                        var sourceSegmentEntityIdx = (byte) (sourceEntityId & Const.ENTITIES_IN_SEGMENT_MASK);
                        if (HasCopyTo) {
                            ComponentSegments[segmentIdx][sourceSegmentEntityIdx].CopyTo(source, destination);
                        }
                        else {
                            Set(destination, ComponentSegments[segmentIdx][sourceSegmentEntityIdx]);
                        }
                    }
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Moves component <typeparamref name="T"/> from <paramref name="source"/> to <paramref name="destination"/>.
            /// Equivalent to <see cref="Copy"/> followed by <see cref="Delete"/> on the source.
            /// After this operation, the source no longer has the component.
            /// </summary>
            /// <param name="source">Entity to move from. Must not be destroyed or unloaded.</param>
            /// <param name="destination">Entity to move to. Must not be destroyed or unloaded.</param>
            /// <returns><c>true</c> if the source had the component, and it was moved; <c>false</c> if source lacked it.</returns>
            [MethodImpl(AggressiveInlining)]
            public bool Move(Entity source, Entity destination) {
                if (IsTag && source.IdWithOffset == destination.IdWithOffset) {
                    return Has(source);
                }
                if (Copy(source, destination)) {
                    Delete(source);
                    return true;
                }

                return false;
            }

            #region TAG_OPERATIONS
            /// <summary>
            /// Sets (adds) tag on the entity. Tag-only operation.
            /// If the tag is already present, this is a no-op and returns <c>false</c>.
            /// </summary>
            [MethodImpl(AggressiveInlining)]
            public bool Set(Entity entity) {
                #if FFS_ECS_DEBUG
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                AssertNotBlockedByQuery(ComponentsTypeName, entity, Masks.BlockerAdd);
                AssertIsTag<T>(ComponentsTypeName);
                #endif

                return Masks.Add(entity.IdWithOffset - Const.ENTITY_ID_OFFSET) != ComponentMasks.Existed;
            }

            /// <summary>
            /// Toggles tag on the entity: removes if present, adds if absent. Tag-only operation.
            /// </summary>
            [MethodImpl(AggressiveInlining)]
            public bool Toggle(Entity entity) {
                #if FFS_ECS_DEBUG
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                AssertIsTag<T>(ComponentsTypeName);
                #endif

                if (Delete(entity)) {
                    return false;
                }

                Set(entity);
                return true;
            }

            /// <summary>
            /// Conditionally sets or removes tag based on the state flag. Tag-only operation.
            /// </summary>
            [MethodImpl(AggressiveInlining)]
            public void Apply(Entity entity, bool state) {
                #if FFS_ECS_DEBUG
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                AssertIsTag<T>(ComponentsTypeName);
                #endif

                if (state) {
                    Set(entity);
                }
                else {
                    Delete(entity);
                }
            }
            #endregion

            /// <summary>
            /// Counts the total number of entities that currently have component <typeparamref name="T"/>
            /// (both enabled and disabled). This is a full scan over all heuristic chunks and bitmasks,
            /// using popcount for efficient bit counting. The result is exact but not O(1) — use
            /// sparingly, not in hot loops.
            /// </summary>
            /// <returns>Total number of entities with this component across the entire world.</returns>
            [MethodImpl(AggressiveInlining)]
            public readonly uint CalculateCount() => Masks.CalculateCount();

            /// <summary>
            /// Calculates the current memory capacity for this component type, in entity slots.
            /// Returns the total number of allocated segment slots (including pooled segments),
            /// each holding <c>ENTITIES_IN_SEGMENT</c> (256) entries. This reflects the memory footprint,
            /// not the actual entity count.
            /// </summary>
            /// <returns>Total capacity in entity slots (allocated segments * 256 + pooled segments * 256).</returns>
            [MethodImpl(AggressiveInlining)]
            public readonly int CalculateCapacity() => Masks.CalculateCapacity();
            #endregion

            #region INTERNAL API
            #region BATCH
            [MethodImpl(AggressiveInlining)]
            internal void BatchDelete(QueryData queryData, int nextGlobalBlockIdx, HookReason reason = HookReason.Default) {
                if (IsTag) {
                    while (nextGlobalBlockIdx >= 0) {
                        ref var blocks = ref queryData.Blocks[nextGlobalBlockIdx];
                    
                        var globalBlockIdx = (uint)nextGlobalBlockIdx;
                        nextGlobalBlockIdx = blocks.NextGlobalBlock;
                    
                        var segmentIdx = globalBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT;
                        var segmentBlockIdx = (byte) (globalBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);

                        BatchDeleteTag(blocks.EntitiesMask, segmentIdx, segmentBlockIdx);
                    }
                }
                else {
                    while (nextGlobalBlockIdx >= 0) {
                        ref var blocks = ref queryData.Blocks[nextGlobalBlockIdx];

                        var globalBlockIdx = (uint)nextGlobalBlockIdx;
                        nextGlobalBlockIdx = blocks.NextGlobalBlock;

                        var segmentIdx = globalBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT;
                        var segmentBlockIdx = (byte) (globalBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);

                        BatchDelete(blocks.EntitiesMask, segmentIdx, segmentBlockIdx, reason);
                    }
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void BatchAdd(ulong entitiesMaskFilter, uint segmentIdx, byte segmentBlockIdx) {
                #if !NET6_0_OR_GREATER
                var deBruijn = Utils.DeBruijn;
                #endif

                var baseGlobalBlockEntityIdx = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) + (segmentBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                var chunkBlockIdx = (byte)((baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK);
                var chunkIdx = baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_CHUNK_SHIFT;
                var baseSegmentEntityIdx = (byte) (baseGlobalBlockEntityIdx & Const.ENTITIES_IN_SEGMENT_MASK);

                ref var maskSegment = ref Masks.Segments[segmentIdx];
                ref var componentSegment = ref ComponentSegments[segmentIdx];
                if (maskSegment == null) {
                    (maskSegment, componentSegment) = CreateNewSegment(segmentIdx);
                }

                ref var entitiesMask = ref maskSegment[segmentBlockIdx];
                entitiesMaskFilter &= ~entitiesMask;
                if (entitiesMaskFilter == 0) {
                    return;
                }

                if (entitiesMask == 0) {
                    Masks.HeuristicChunks[chunkIdx].NotEmptyBlocks.SetBit(chunkBlockIdx);
                }
                
                if (!HasOnAdd) {
                    entitiesMask |= entitiesMaskFilter;
                }
                else {
                    var entity = new Entity(baseGlobalBlockEntityIdx);
                    ref var entityIdRef = ref entity.IdWithOffset;

                    var starts = entitiesMaskFilter & ~(entitiesMaskFilter << 1);
                    var ends = entitiesMaskFilter & ~(entitiesMaskFilter >> 1);
                    do {
                        #if NET6_0_OR_GREATER
                        var start = (byte)System.Numerics.BitOperations.TrailingZeroCount(starts);
                        var endIncluded = (byte)System.Numerics.BitOperations.TrailingZeroCount(ends);
                        #else
                        var start = deBruijn[(uint)(((starts & (ulong)-(long)starts) * 0x37E84A99DAE458FUL) >> 58)];
                        var endIncluded = deBruijn[(uint)(((ends & (ulong)-(long)ends) * 0x37E84A99DAE458FUL) >> 58)];
                        #endif
                        starts &= starts - 1UL;
                        ends &= ends - 1UL;

                        entityIdRef = baseGlobalBlockEntityIdx + start + Const.ENTITY_ID_OFFSET;

                        var blockEntityMask = 1UL << start;
                        while (start <= endIncluded) {
                            entitiesMask |= blockEntityMask;

                            #if FFS_ECS_DEBUG
                            Data.Instance.SetCurrentQueryEntity(entity.IdWithOffset);
                            #endif
                            componentSegment[baseSegmentEntityIdx + start].OnAdd(entity);
                            blockEntityMask <<= 1;
                            entityIdRef++;
                            start++;
                        }
                    } while (starts != 0);
                }

                if (entitiesMask == ulong.MaxValue) {
                    Masks.HeuristicChunks[chunkIdx].FullBlocks.SetBit(chunkBlockIdx);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void BatchSet(T value, ulong entitiesMaskFilter, uint segmentIdx, byte segmentBlockIdx) {
                #if !NET6_0_OR_GREATER
                var deBruijn = Utils.DeBruijn;
                #endif

                var baseGlobalBlockEntityIdx = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) + (segmentBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                var chunkBlockIdx = (byte)((baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK);
                var chunkIdx = baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_CHUNK_SHIFT;
                var baseSegmentEntityIdx = (byte) (baseGlobalBlockEntityIdx & Const.ENTITIES_IN_SEGMENT_MASK);

                ref var maskSegment = ref Masks.Segments[segmentIdx];
                ref var componentSegment = ref ComponentSegments[segmentIdx];
                if (maskSegment == null) {
                    (maskSegment, componentSegment) = CreateNewSegment(segmentIdx);
                }

                ref var entitiesMask = ref maskSegment[segmentBlockIdx];
                var existingEntitiesMask = entitiesMaskFilter & entitiesMask;
                var newEntitiesMaskFilter = entitiesMaskFilter & ~entitiesMask;

                if (HasOnDelete && existingEntitiesMask != 0) {
                    var entity = new Entity(baseGlobalBlockEntityIdx);
                    ref var entityIdRef = ref entity.IdWithOffset;

                    var starts = existingEntitiesMask & ~(existingEntitiesMask << 1);
                    var ends = existingEntitiesMask & ~(existingEntitiesMask >> 1);
                    do {
                        #if NET6_0_OR_GREATER
                        var start = (byte)System.Numerics.BitOperations.TrailingZeroCount(starts);
                        var endIncluded = (byte)System.Numerics.BitOperations.TrailingZeroCount(ends);
                        #else
                        var start = deBruijn[(uint)(((starts & (ulong)-(long)starts) * 0x37E84A99DAE458FUL) >> 58)];
                        var endIncluded = deBruijn[(uint)(((ends & (ulong)-(long)ends) * 0x37E84A99DAE458FUL) >> 58)];
                        #endif
                        starts &= starts - 1UL;
                        ends &= ends - 1UL;

                        entityIdRef = baseGlobalBlockEntityIdx + start + Const.ENTITY_ID_OFFSET;
                        while (start <= endIncluded) {
                            #if FFS_ECS_DEBUG
                            Data.Instance.SetCurrentQueryEntity(entity.IdWithOffset);
                            #endif
                            componentSegment[baseSegmentEntityIdx + start].OnDelete(entity, HookReason.Default);
                            entityIdRef++;
                            start++;
                        }
                    } while (starts != 0);

                    if (Masks.Segments[segmentIdx] != maskSegment) {
                        #pragma warning disable EPC30
                        // ReSharper disable once TailRecursiveCall
                        BatchSet(value, entitiesMaskFilter, segmentIdx, segmentBlockIdx);
                        #pragma warning restore EPC30
                        return;
                    }
                }

                {
                    var starts = entitiesMaskFilter & ~(entitiesMaskFilter << 1);
                    var ends = entitiesMaskFilter & ~(entitiesMaskFilter >> 1);
                    do {
                        #if NET6_0_OR_GREATER
                        var start = (byte)System.Numerics.BitOperations.TrailingZeroCount(starts);
                        var endIncluded = (byte)System.Numerics.BitOperations.TrailingZeroCount(ends);
                        #else
                        var start = deBruijn[(uint)(((starts & (ulong)-(long)starts) * 0x37E84A99DAE458FUL) >> 58)];
                        var endIncluded = deBruijn[(uint)(((ends & (ulong)-(long)ends) * 0x37E84A99DAE458FUL) >> 58)];
                        #endif
                        starts &= starts - 1UL;
                        ends &= ends - 1UL;

                        var count = endIncluded - start + 1;
                        new Span<T>(componentSegment, baseSegmentEntityIdx + start, count).Fill(value);
                    } while (starts != 0);
                }

                if (newEntitiesMaskFilter != 0) {
                    if (entitiesMask == 0) {
                        Masks.HeuristicChunks[chunkIdx].NotEmptyBlocks.SetBit(chunkBlockIdx);
                    }
                    entitiesMask |= newEntitiesMaskFilter;
                    if (entitiesMask == ulong.MaxValue) {
                        Masks.HeuristicChunks[chunkIdx].FullBlocks.SetBit(chunkBlockIdx);
                    }
                }

                if (HasOnAdd) {
                    var entity = new Entity(baseGlobalBlockEntityIdx);
                    ref var entityIdRef = ref entity.IdWithOffset;

                    var starts = entitiesMaskFilter & ~(entitiesMaskFilter << 1);
                    var ends = entitiesMaskFilter & ~(entitiesMaskFilter >> 1);
                    do {
                        #if NET6_0_OR_GREATER
                        var start = (byte)System.Numerics.BitOperations.TrailingZeroCount(starts);
                        var endIncluded = (byte)System.Numerics.BitOperations.TrailingZeroCount(ends);
                        #else
                        var start = deBruijn[(uint)(((starts & (ulong)-(long)starts) * 0x37E84A99DAE458FUL) >> 58)];
                        var endIncluded = deBruijn[(uint)(((ends & (ulong)-(long)ends) * 0x37E84A99DAE458FUL) >> 58)];
                        #endif
                        starts &= starts - 1UL;
                        ends &= ends - 1UL;

                        entityIdRef = baseGlobalBlockEntityIdx + start + Const.ENTITY_ID_OFFSET;
                        while (start <= endIncluded) {
                            #if FFS_ECS_DEBUG
                            Data.Instance.SetCurrentQueryEntity(entity.IdWithOffset);
                            #endif
                            componentSegment[baseSegmentEntityIdx + start].OnAdd(entity);
                            entityIdRef++;
                            start++;
                        }
                    } while (starts != 0);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void BatchDelete(ulong entitiesMaskFilter, uint segmentIdx, byte segmentBlockIdx, HookReason reason = HookReason.Default) {
                #if !NET6_0_OR_GREATER
                var deBruijn = Utils.DeBruijn;
                #endif

                var baseGlobalBlockEntityIdx = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) + (segmentBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                var chunkBlockIdx = (byte)((baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK);
                var chunkIdx = baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_CHUNK_SHIFT;
                var baseSegmentEntityIdx = (byte) (baseGlobalBlockEntityIdx & Const.ENTITIES_IN_SEGMENT_MASK);

                var maskSegment = Masks.Segments[segmentIdx];
                var componentSegment = ComponentSegments[segmentIdx];
                if (maskSegment == null) {
                    return;
                }

                ref var entitiesMask = ref maskSegment[segmentBlockIdx];
                entitiesMaskFilter &= entitiesMask;
                if (entitiesMaskFilter == 0) {
                    return;
                }
                if (entitiesMask == ulong.MaxValue) {
                    Masks.HeuristicChunks[chunkIdx].FullBlocks.ClearBit(chunkBlockIdx);
                }
                
                if (!HasOnDelete && !DataLifecycle) {
                    entitiesMask &= ~entitiesMaskFilter;
                }
                else if (entitiesMaskFilter == ulong.MaxValue && !HasOnDelete && !DataLifecycle) {
                    entitiesMask = 0;
                }
                else if (entitiesMaskFilter == ulong.MaxValue && !HasOnDelete) {
                    entitiesMask = 0;
                    if (HasDefaultValue) {
                        componentSegment.AsSpan(baseSegmentEntityIdx, Const.ENTITIES_IN_BLOCK).Fill(DefaultValue);
                    } else {
                        Array.Clear(componentSegment, baseSegmentEntityIdx, Const.ENTITIES_IN_BLOCK);
                    }
                }
                else if (!HasOnDelete) {
                    var starts = entitiesMaskFilter & ~(entitiesMaskFilter << 1);
                    var ends = entitiesMaskFilter & ~(entitiesMaskFilter >> 1);
                    do {
                        #if NET6_0_OR_GREATER
                        var start = (byte)System.Numerics.BitOperations.TrailingZeroCount(starts);
                        var endIncluded = (byte)System.Numerics.BitOperations.TrailingZeroCount(ends);
                        #else
                        var start = deBruijn[(uint)(((starts & (ulong)-(long)starts) * 0x37E84A99DAE458FUL) >> 58)];
                        var endIncluded = deBruijn[(uint)(((ends & (ulong)-(long)ends) * 0x37E84A99DAE458FUL) >> 58)];
                        #endif
                        starts &= starts - 1UL;
                        ends &= ends - 1UL;

                        var count = endIncluded - start + 1;
                        var runMask = ((2UL << (endIncluded - start)) - 1) << start;
                        var invertedRunMask = ~runMask;
                        entitiesMask &= invertedRunMask;

                        if (HasDefaultValue) {
                            componentSegment.AsSpan(baseSegmentEntityIdx + start, count).Fill(DefaultValue);
                        } else {
                            Array.Clear(componentSegment, baseSegmentEntityIdx + start, count);
                        }
                    } while (starts != 0);
                }
                else {
                    var entity = new Entity(baseGlobalBlockEntityIdx);
                    ref var entityIdRef = ref entity.IdWithOffset;

                    var starts = entitiesMaskFilter & ~(entitiesMaskFilter << 1);
                    var ends = entitiesMaskFilter & ~(entitiesMaskFilter >> 1);
                    do {
                        #if NET6_0_OR_GREATER
                        var start = (byte)System.Numerics.BitOperations.TrailingZeroCount(starts);
                        var endIncluded = (byte)System.Numerics.BitOperations.TrailingZeroCount(ends);
                        #else
                        var start = deBruijn[(uint)(((starts & (ulong)-(long)starts) * 0x37E84A99DAE458FUL) >> 58)];
                        var endIncluded = deBruijn[(uint)(((ends & (ulong)-(long)ends) * 0x37E84A99DAE458FUL) >> 58)];
                        #endif
                        starts &= starts - 1UL;
                        ends &= ends - 1UL;

                        entityIdRef = baseGlobalBlockEntityIdx + start + Const.ENTITY_ID_OFFSET;

                        var blockEntityMask = 1UL << start;
                        while (start <= endIncluded) {
                            var invertedBlockEntityMask = ~blockEntityMask;
                            entitiesMask &= invertedBlockEntityMask;

                            #if FFS_ECS_DEBUG
                            Data.Instance.SetCurrentQueryEntity(entity.IdWithOffset);
                            #endif
                            componentSegment[baseSegmentEntityIdx + start].OnDelete(entity, reason);
                            blockEntityMask <<= 1;
                            entityIdRef++;
                            start++;
                        }
                    } while (starts != 0);
                }

                if (entitiesMask == 0 && Masks.Segments[segmentIdx] == maskSegment) {
                    var notEmptyBlocks = Masks.HeuristicChunks[chunkIdx].NotEmptyBlocks.ClearBit(chunkBlockIdx);
                    if ((notEmptyBlocks & Const.DataMasks[segmentIdx & Const.SEGMENTS_IN_CHUNK_MASK]) == 0UL) {
                        FreeSegment(chunkIdx, segmentIdx, maskSegment, componentSegment);
                    }
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void BatchSetTag(ulong entitiesMaskFilter, uint segmentIdx, byte segmentBlockIdx) {
                var baseGlobalBlockEntityIdx = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) + (segmentBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                var chunkBlockIdx = (byte)((baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK);
                var chunkIdx = baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_CHUNK_SHIFT;

                ref var maskSegment = ref Masks.Segments[segmentIdx];
                maskSegment ??= CreateNewSegment(segmentIdx).Item1;

                ref var entitiesMask = ref maskSegment[segmentBlockIdx];
                entitiesMaskFilter &= ~entitiesMask;
                if (entitiesMaskFilter == 0) {
                    return;
                }

                if (entitiesMask == 0) {
                    Masks.HeuristicChunks[chunkIdx].NotEmptyBlocks.SetBit(chunkBlockIdx);
                }

                entitiesMask |= entitiesMaskFilter;

                if (entitiesMask == ulong.MaxValue) {
                    Masks.HeuristicChunks[chunkIdx].FullBlocks.SetBit(chunkBlockIdx);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void BatchDeleteTag(ulong entitiesMaskFilter, uint segmentIdx, byte segmentBlockIdx) {
                var baseGlobalBlockEntityIdx = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) + (segmentBlockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                var chunkBlockIdx = (byte)((baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_CHUNK_MASK);
                var chunkIdx = baseGlobalBlockEntityIdx >> Const.ENTITIES_IN_CHUNK_SHIFT;

                var maskSegment = Masks.Segments[segmentIdx];
                if (maskSegment == null) {
                    return;
                }

                ref var entitiesMask = ref maskSegment[segmentBlockIdx];
                entitiesMaskFilter &= entitiesMask;
                if (entitiesMaskFilter == 0) {
                    return;
                }

                if (entitiesMask == ulong.MaxValue) {
                    Masks.HeuristicChunks[chunkIdx].FullBlocks.ClearBit(chunkBlockIdx);
                }
                
                entitiesMask &= ~entitiesMaskFilter;

                if (entitiesMask == 0) {
                    var notEmptyBlocks = Masks.HeuristicChunks[chunkIdx].NotEmptyBlocks.ClearBit(chunkBlockIdx);
                    if ((notEmptyBlocks & Const.DataMasks[segmentIdx & Const.SEGMENTS_IN_CHUNK_MASK]) == 0UL) {
                        Masks.FreeSegment(chunkIdx, segmentIdx, maskSegment);
                    }
                }
            }

            #endregion

            #if NET5_0_OR_GREATER
            [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Component metadata is preserved by the registration path.")]
            #endif
            internal Components(ushort componentId, Guid guid, byte version, bool noDataLifecycle, IPackArrayStrategy<T> readWriteStrategy, bool hasDefaultValue, T defaultValue, bool isTag, bool nonSerializable, string typeName) {
                TypeName = typeName;
                IsTag = isTag;
                NonSerializable = nonSerializable;
                DynamicId = componentId;

                Guid = guid;
                Version = version;
                _readWriteArrayStrategy = default(T) is IComponentStrategyOverride ? null : readWriteStrategy;
                _resettableStrategy = _readWriteArrayStrategy as IPackArrayStrategyResettable;

                _componentsPoolCount = 0;
                var hooks = TypeHooks.DescribeComponent(typeof(T), default(T), isTag);
                HasOnAdd = hooks.HasOnAdd;
                HasOnDelete = hooks.HasOnDelete;
                HasCopyTo = hooks.HasCopyTo;
                HasWrite = hooks.HasWrite;
                HasRead = hooks.HasRead;
                Unmanaged = isTag || !RuntimeHelpers.IsReferenceOrContainsReferences<T>();
                DataLifecycle = !isTag && !noDataLifecycle;
                DefaultValue = defaultValue;
                HasDefaultValue = hasDefaultValue;

                Masks = new ComponentMasks(componentId, isTag);
                #if FFS_ECS_DEBUG
                IsInternal = hooks.IsInternal;
                #endif

                IsRegistered = true;
                ComponentSegments = null;
                MaskSegments = null;
                _componentsPool = null;

                #if FFS_ECS_TRACE
                Utils.Trace($"Registered {TypeName}:\n"
                            + $"IsTag {IsTag}\n"
                            + $"DynamicId {DynamicId}\n"
                            + $"Guid {Guid}\n"
                            + $"Version {Version}\n"
                            + $"Unmanaged {Unmanaged}\n"
                            + $"HasDefaultValue {HasDefaultValue}\n"
                            + $"ReadWriteStrategyType {_readWriteArrayStrategy?.GetType().ToString() ?? "null"}\n"
                            + $"HasOnAdd {HasOnAdd}\n"
                            + $"HasOnDelete {HasOnDelete}\n"
                            + $"HasWrite {HasWrite}\n"
                            + $"HasRead {HasRead}\n"
                            + "\n"
                );
                #endif
            }
            
            [MethodImpl(NoInlining)]
            private void ResolveStrategy() {
                if (_readWriteArrayStrategy != null) return;
                if (default(T) is IComponentStrategyOverride packOverride) {
                    _readWriteArrayStrategy = packOverride.ArrayPackStrategy<T>();
                }
                _readWriteArrayStrategy ??= AutoRegistration.TryCreateUnmanagedPackArrayStrategy<T>() ?? new StructPackArrayStrategy<T>();
                _resettableStrategy = _readWriteArrayStrategy as IPackArrayStrategyResettable;
            }

            [MethodImpl(AggressiveInlining)]
            internal void Initialize(uint chunksCapacity, ulong[][] chunkHeuristicMask, ushort chunkHeuristicMaskLen) {
                Masks.Initialize(chunksCapacity, chunkHeuristicMask, chunkHeuristicMaskLen);
                MaskSegments = Masks.Segments;
                if (!IsTag) {
                    var segmentsCapacity = chunksCapacity * Const.SEGMENTS_IN_CHUNK;
                    ComponentSegments = new T[segmentsCapacity][];
                    _componentsPool = new T[segmentsCapacity][];
                }

                if (default(T) is IComponentInternal component) {
                    component.OnInitialize();
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal void DestroyInternal() {
                this = default;
            }

            [MethodImpl(NoInlining)]
            internal void HardResetInternal() {
                for (var i = 0; i < Masks.Segments.Length; i++) {
                    ReleaseSegmentInternal(i);
                }

                Array.Clear(Masks.HeuristicChunks, 0, Masks.HeuristicChunks.Length);
            }

            [MethodImpl(NoInlining)]
            internal void HardResetChunkInternal(uint chunkIdx) {
                var baseSegmentIdx = (int)(chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT);
                for (var s = 0; s < Const.SEGMENTS_IN_CHUNK; s++) {
                    ReleaseSegmentInternal(baseSegmentIdx + s);
                }
                Masks.HeuristicChunks[chunkIdx] = default;
            }

            [MethodImpl(AggressiveInlining)]
            private void ReleaseSegmentInternal(int segIdx) {
                if (!IsTag && Masks.Segments[segIdx] != null) {
                    ref var components = ref ComponentSegments[segIdx];
                    if (HasDefaultValue) {
                        components.AsSpan().Fill(DefaultValue);
                    } else {
                        Array.Clear(components, 0, Const.ENTITIES_IN_SEGMENT);
                    }
                    _componentsPool[_componentsPoolCount++] = components;
                    components = null;
                }
                Masks.ReleaseSegment(segIdx);
            }

            [MethodImpl(AggressiveInlining)]
            internal void Resize(uint chunksCapacity, ulong[][] chunkHeuristicMask) {
                Masks.Resize(chunksCapacity, chunkHeuristicMask);
                MaskSegments = Masks.Segments;
                if (!IsTag) {
                    var segmentsCapacity = (int) (chunksCapacity * Const.SEGMENTS_IN_CHUNK);
                    Array.Resize(ref ComponentSegments, segmentsCapacity);
                    Array.Resize(ref _componentsPool, segmentsCapacity);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal readonly void TryToStringComponent(StringBuilder builder, Entity entity) {
                if (Has(entity)) {
                    builder.Append(" - [");
                    builder.Append(DynamicId);
                    builder.Append("] ");
                    if (IsTag) {
                        builder.AppendLine(TypeName);
                    } else {
                        builder.Append(TypeName);
                        builder.Append(" ( ");
                        builder.Append(Read(entity));
                        builder.AppendLine(" )");
                    }
                }
            }
            
            [MethodImpl(AggressiveInlining)]
            public readonly bool TryGet(Entity entity, out T value) {
                #if FFS_ECS_DEBUG
                AssertEntityIsNotDestroyedAndLoaded(ComponentsTypeName, entity);
                #endif

                var entityId = entity.IdWithOffset - Const.ENTITY_ID_OFFSET;
                var segmentIdx = entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT;
                var segmentBlockIdx = (byte) ((entityId >> Const.ENTITIES_IN_BLOCK_SHIFT) & Const.BLOCKS_IN_SEGMENT_MASK);
                var blockEntityMask = 1UL << (byte) (entityId & Const.ENTITIES_IN_BLOCK_MASK);

                var maskSegment = Masks.Segments[segmentIdx];
                if (maskSegment != null && (maskSegment[segmentBlockIdx] & blockEntityMask) != 0) {
                    value = ComponentSegments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT][entityId & Const.ENTITIES_IN_SEGMENT_MASK];
                    return true;
                }

                value = default;
                return false;
            }

            [MethodImpl(NoInlining)]
            private (ulong[], T[]) CreateNewSegment(uint segmentIdx) {
                return (Masks.CreateSegment(segmentIdx), IsTag ? null : NewDataSegment());
            }

            private T[] NewDataSegment() {
                if (_componentsPoolCount > 0) {
                    var poolIdx = --_componentsPoolCount;
                    var pooled = _componentsPool[poolIdx];
                    _componentsPool[poolIdx] = null;
                    return pooled;
                }

                var comps = new T[Const.ENTITIES_IN_SEGMENT];
                if (HasDefaultValue && DataLifecycle) {
                    comps.AsSpan().Fill(DefaultValue);
                }
                return comps;
            }

            private void FreeDataSegment(uint segmentIdx) {
                _componentsPool[_componentsPoolCount++] = ComponentSegments[segmentIdx];
                ComponentSegments[segmentIdx] = null;
            }

            [MethodImpl(NoInlining)]
            private void FreeSegment(uint chunkIdx, uint segmentIdx, ulong[] maskSegment, T[] componentSegment) {
                Masks.FreeSegment(chunkIdx, segmentIdx, maskSegment);
                if (!IsTag) {
                    ComponentSegments[segmentIdx] = null;
                    _componentsPool[_componentsPoolCount++] = componentSegment;
                }
            }

            #region QUERY
            [MethodImpl(AggressiveInlining)]
            internal readonly ulong EnabledMask(uint segmentIdx, int segmentBlockIdx) => Masks.Mask(segmentIdx, segmentBlockIdx);

            
            #if FFS_ECS_DEBUG
            [MethodImpl(AggressiveInlining)]
            internal void BlockDelete(int val) => Masks.BlockDelete(val);

            [MethodImpl(AggressiveInlining)]
            internal void BlockAdd(int val) => Masks.BlockAdd(val);

            #endif
            #endregion

            #region SERIALIZATION
            [MethodImpl(AggressiveInlining)]
            internal void WriteChunk(ref BinaryPackWriter writer, uint chunkIdx) {
                writer.WriteBool(IsTag);

                ref var heuristic = ref Masks.HeuristicChunks[chunkIdx];
                var notEmptyBlocks = heuristic.NotEmptyBlocks.Value;
                writer.WriteUlong(notEmptyBlocks);
                
                if (notEmptyBlocks != 0) {
                    #if !NET6_0_OR_GREATER
                    var deBruijn = Utils.DeBruijn;
                    #endif

                    writer.WriteUlong(heuristic.FullBlocks.Value);
                    if (IsTag) {
                        var segmentBase = chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT;
                        for (var segment = 0; segment < Const.SEGMENTS_IN_CHUNK; segment++) {
                            if (((notEmptyBlocks >> (segment << Const.BLOCKS_IN_SEGMENT_SHIFT)) & 0b1111UL) != 0) {
                                var masks = Masks.Segments[segmentBase + segment];
                                writer.WriteUlong(masks[0]);
                                writer.WriteUlong(masks[1]);
                                writer.WriteUlong(masks[2]);
                                writer.WriteUlong(masks[3]);
                            }
                        }
                    }
                    else {
                        ResolveStrategy();
                        var unmanagedStrategy = _readWriteArrayStrategy.IsUnmanaged();

                        writer.WriteByte(Version);
                        writer.WriteBool(unmanagedStrategy);

                        _resettableStrategy?.Reset();

                        var entity = new Entity();
                        ref var entityId = ref entity.IdWithOffset;
                        var segmentIdx = uint.MaxValue;
                        ulong[] masks = null;
                        T[] components = null;
                        while (notEmptyBlocks != 0) {
                            #if NET6_0_OR_GREATER
                            var chunkBlockIdx = (uint)System.Numerics.BitOperations.TrailingZeroCount(notEmptyBlocks);
                            #else
                            var chunkBlockIdx = (uint)deBruijn[(uint)(((notEmptyBlocks & (ulong)-(long)notEmptyBlocks) * 0x37E84A99DAE458FUL) >> 58)];
                            #endif
                            notEmptyBlocks &= notEmptyBlocks - 1;
                            var blockIdx = (byte)(chunkBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);
                            var curSegmentIdx = (chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT) + (chunkBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT);
                            if (curSegmentIdx != segmentIdx) {
                                segmentIdx = curSegmentIdx;
                                masks = Masks.Segments[segmentIdx];
                                components = ComponentSegments[segmentIdx];
                            }

                            var entitiesMask = masks![blockIdx];

                            writer.WriteUlong(entitiesMask);

                            var chunkBlockEntityId = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) +
                                                            (blockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                            var componentOffset = chunkBlockEntityId & Const.ENTITIES_IN_SEGMENT_MASK;
                            chunkBlockEntityId += Const.ENTITY_ID_OFFSET;

                            if (unmanagedStrategy) {
                                #if NET6_0_OR_GREATER
                                var firstBit = (byte)System.Numerics.BitOperations.TrailingZeroCount(entitiesMask);
                                #else
                                var firstBit = deBruijn[(uint)(((entitiesMask & (ulong)-(long)entitiesMask) * 0x37E84A99DAE458FUL) >> 58)];
                                #endif
                                var lastBit = (byte)Utils.Msb(entitiesMask);
                                var rangeCount = lastBit - firstBit + 1;
                                writer.WriteByte(firstBit);
                                writer.WriteByte((byte)rangeCount);
                                _readWriteArrayStrategy.WriteArray(ref writer, components, (int)(componentOffset + firstBit), rangeCount);
                            }
                            else {
                                #if FFS_ECS_DEBUG
                                if (!HasWrite) throw new StaticEcsException($"Method Write not implemented for component type {typeof(T)}");
                                #endif

                                do {
                                    var isolatedBit = entitiesMask & (ulong)-(long)entitiesMask;
                                    #if NET6_0_OR_GREATER
                                    var runStart = (byte)System.Numerics.BitOperations.TrailingZeroCount(entitiesMask);
                                    #else
                                    var runStart = deBruijn[(uint)((isolatedBit * 0x37E84A99DAE458FUL) >> 58)];
                                    #endif

                                    var componentIdx = runStart + componentOffset;
                                    entityId = chunkBlockEntityId + runStart;

                                    do {
                                        components[componentIdx].Write(ref writer, entity);
                                        isolatedBit <<= 1;
                                        componentIdx++;
                                        entityId++;
                                    } while ((entitiesMask & isolatedBit) != 0);

                                    entitiesMask &= ~(isolatedBit - 1);
                                } while (entitiesMask != 0);
                            }
                        }
                    }
                }

            }

            [MethodImpl(AggressiveInlining)]
            internal void ReadChunk(ref BinaryPackReader reader, uint chunkIdx) {
                var isTag = reader.ReadBool();
                #if FFS_ECS_DEBUG
                if (isTag != IsTag) throw new StaticEcsException($"isTag != IsTag");
                #endif

                ref var heuristic = ref Masks.HeuristicChunks[chunkIdx];
                var notEmptyBlocks = reader.ReadUlong();
                heuristic.NotEmptyBlocks.Value = notEmptyBlocks;
                
                if (notEmptyBlocks != 0) {
                    #if !NET6_0_OR_GREATER
                    var deBruijn = Utils.DeBruijn;
                    #endif
                    
                    heuristic.FullBlocks.Value = reader.ReadUlong();
                    if (IsTag) {
                        var segmentBase = chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT;
                        for (var segment = 0; segment < Const.SEGMENTS_IN_CHUNK; segment++) {
                            if (((notEmptyBlocks >> (segment << Const.BLOCKS_IN_SEGMENT_SHIFT)) & 0b1111UL) != 0) {
                                var segmentIdx = segmentBase + segment;
                                ref var masks = ref Masks.Segments[segmentIdx];
                                masks ??= CreateNewSegment((uint)segmentIdx).Item1;
                                masks[0] = reader.ReadUlong();
                                masks[1] = reader.ReadUlong();
                                masks[2] = reader.ReadUlong();
                                masks[3] = reader.ReadUlong();
                            }
                        }
                    }
                    else {
                        var oldVersion = reader.ReadByte();
                        var unmanagedStrategy = reader.ReadBool();
                        ResolveStrategy();

                        _resettableStrategy?.Reset();

                        var segmentIdx = uint.MaxValue;
                        ulong[] masks = null;
                        T[] components = null;

                        var entity = new Entity();
                        ref var entityId = ref entity.IdWithOffset;
                        while (notEmptyBlocks != 0) {
                            #if NET6_0_OR_GREATER
                            var chunkBlockIdx = (uint)System.Numerics.BitOperations.TrailingZeroCount(notEmptyBlocks);
                            #else
                            var chunkBlockIdx = (uint)deBruijn[(uint)(((notEmptyBlocks & (ulong)-(long)notEmptyBlocks) * 0x37E84A99DAE458FUL) >> 58)];
                            #endif
                            notEmptyBlocks &= notEmptyBlocks - 1;
                            var blockIdx = (byte)(chunkBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);
                            var curSegmentIdx = (chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT) + (chunkBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT);
                            if (curSegmentIdx != segmentIdx) {
                                segmentIdx = curSegmentIdx;

                                masks = Masks.Segments[segmentIdx];
                                if (masks == null) {
                                    (masks, ComponentSegments[segmentIdx]) = CreateNewSegment(segmentIdx);
                                    Masks.Segments[segmentIdx] = masks;
                                }

                                components = ComponentSegments[segmentIdx];
                            }

                            var entitiesMask = reader.ReadUlong();
                            masks![blockIdx] = entitiesMask;

                            var chunkBlockEntityId = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) +
                                                            (blockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                            var componentOffset = chunkBlockEntityId & Const.ENTITIES_IN_SEGMENT_MASK;
                            chunkBlockEntityId += Const.ENTITY_ID_OFFSET;

                            if (unmanagedStrategy) {
                                var firstBit = reader.ReadByte();
                                var rangeCount = (int)reader.ReadByte();
                                if (oldVersion == Version) {
                                    _readWriteArrayStrategy.ReadArray(ref reader, ref components, (int)(componentOffset + firstBit));
                                }
                                else {
                                    #if FFS_ECS_DEBUG
                                    if (!HasRead) throw new StaticEcsException($"Method Read not implemented for component type {typeof(T)}");
                                    #endif
                                    _ = reader.ReadNullFlag();
                                    _ = reader.ReadInt(); // count
                                    var byteSize = reader.ReadUint();
                                    var oneSize = byteSize / (uint)rangeCount;
                                    for (var cur = 0; cur < rangeCount; cur++) {
                                        var bitIdx = firstBit + cur;
                                        entityId = (uint)(chunkBlockEntityId + bitIdx);
                                        var mask = 1UL << bitIdx;
                                        if ((entitiesMask & mask) != 0) {
                                            components[componentOffset + bitIdx].Read(ref reader, entity, oldVersion);
                                        }
                                        else {
                                            reader.SkipNext(oneSize);
                                        }
                                    }
                                }
                            }
                            else {
                                #if FFS_ECS_DEBUG
                                if (!HasRead) throw new StaticEcsException($"Method Read not implemented for component type {typeof(T)}");
                                #endif

                                do {
                                    var isolatedBit = entitiesMask & (ulong)-(long)entitiesMask;
                                    #if NET6_0_OR_GREATER
                                    var runStart = (byte)System.Numerics.BitOperations.TrailingZeroCount(entitiesMask);
                                    #else
                                    var runStart = deBruijn[(uint)((isolatedBit * 0x37E84A99DAE458FUL) >> 58)];
                                    #endif

                                    var componentIdx = runStart + componentOffset;
                                    entityId = chunkBlockEntityId + runStart;

                                    do {
                                        components[componentIdx].Read(ref reader, entity, oldVersion);
                                        isolatedBit <<= 1;
                                        componentIdx++;
                                        entityId++;
                                    } while ((entitiesMask & isolatedBit) != 0);

                                    entitiesMask &= ~(isolatedBit - 1);
                                } while (entitiesMask != 0);
                            }
                        }
                    }
                }

            }

            [MethodImpl(AggressiveInlining)]
            internal bool WriteEntity(ref BinaryPackWriter writer, Entity entity, bool deleteComponent) {
                if (!Has(entity)) {
                    return false;
                }

                writer.WriteBool(IsTag);
                if (IsTag) {
                    return true;
                }

                var offset = writer.MakePoint(sizeof(ushort));
                writer.WriteByte(Version);
                if (HasWrite) {
                    Ref(entity).Write(ref writer, entity);
                } else if (Unmanaged) {
                    writer.ForceWriteUnmanaged(in Read(entity));
                }
                #if FFS_ECS_DEBUG
                else {
                    throw new StaticEcsException($"Method Write not implemented for managed component type {typeof(T)}");
                }
                #endif
                var size = writer.Position - (offset + sizeof(short));
                #if FFS_ECS_DEBUG
                if (size > short.MaxValue) throw new StaticEcsException($"Size of component {typeof(T)} more than {short.MaxValue} bytes");
                #endif
                writer.WriteUshortAt(offset, (ushort)size);

                if (deleteComponent) {
                    Delete(entity);
                }

                return true;
            }

            [MethodImpl(AggressiveInlining)]
            internal void ReadEntity(ref BinaryPackReader reader, Entity entity) {
                _ = reader.ReadBool(); // isTag
                if (IsTag) {
                    Set(entity);
                    return;
                }

                reader.ReadUshort();
                var oldVersion = reader.ReadByte();

                var component = default(T);
                if (HasRead) {
                    component.Read(ref reader, entity, oldVersion);
                } else if (Unmanaged && oldVersion == Version) {
                    reader.ForceReadUnmanaged(out component);
                }
                #if FFS_ECS_DEBUG
                else {
                    throw new StaticEcsException(
                        Unmanaged
                            ? $"Method Read not implemented for unmanaged component type {typeof(T)} and saved version {oldVersion} differs from current {Version}"
                            : $"Method Read not implemented for managed component type {typeof(T)}");
                }
                #endif
                Set(entity, component, false);
            }
            #endregion

            #region HANDLE
            [MethodImpl(AggressiveInlining)]
            internal static void _Initialize(uint chunksCapacity, ulong[][] chunkHeuristicMask, ushort chunkHeuristicMaskLen) {
                #if FFS_ECS_DEBUG
                instance.Initialize(chunksCapacity, chunkHeuristicMask, chunkHeuristicMaskLen);
                #else
                Instance.Initialize(chunksCapacity, chunkHeuristicMask, chunkHeuristicMaskLen);
                #endif
            }

            [MethodImpl(AggressiveInlining)]
            internal static void _Resize(uint chunksCapacity, ulong[][] chunkHeuristicMask) => Instance.Resize(chunksCapacity, chunkHeuristicMask);

            [MethodImpl(AggressiveInlining)]
            internal static void _Destroy() {
                Instance.DestroyInternal();
                Handle = default;
            }

            [MethodImpl(AggressiveInlining)]
            internal static void _HardReset() => Instance.HardResetInternal();

            [MethodImpl(AggressiveInlining)]
            internal static void _HardResetChunk(uint chunkIdx) => Instance.HardResetChunkInternal(chunkIdx);

            [MethodImpl(AggressiveInlining)]
            internal static void _TryToStringComponent(StringBuilder builder, uint eid) => Instance.TryToStringComponent(builder, new Entity(eid));

            [MethodImpl(AggressiveInlining)]
            internal static void _WriteChunk(ref BinaryPackWriter writer, uint chunkIdx) => Instance.WriteChunk(ref writer, chunkIdx);

            [MethodImpl(AggressiveInlining)]
            internal static void _ReadChunk(ref BinaryPackReader reader, uint chunkIdx) => Instance.ReadChunk(ref reader, chunkIdx);

            [MethodImpl(AggressiveInlining)]
            internal static bool _WriteEntity(ref BinaryPackWriter writer, uint eid, bool deleteComponent) => Instance.WriteEntity(ref writer, new Entity(eid), deleteComponent);

            [MethodImpl(AggressiveInlining)]
            internal static void _ReadEntity(ref BinaryPackReader reader, uint eid) => Instance.ReadEntity(ref reader, new Entity(eid));


            [MethodImpl(AggressiveInlining)]
            internal static Array _ComponentsSegments() => Instance.ComponentSegments;





            [MethodImpl(AggressiveInlining)]
            internal static void _BatchDelete(ulong entitiesMaskFilter, uint segmentIdx, byte segmentBlockIdx, HookReason reason) {
                if (Instance.IsTag) {
                    Instance.BatchDeleteTag(entitiesMaskFilter, segmentIdx, segmentBlockIdx);
                }
                else {
                    Instance.BatchDelete(entitiesMaskFilter, segmentIdx, segmentBlockIdx, reason);
                }
            }



            [MethodImpl(AggressiveInlining)]
            internal static bool _TryGetRaw(uint eid, out IComponentOrTag value) {
                var entity = new Entity(eid);
                if (Instance.Has(entity)) {
                    value = Instance.IsTag ? default : Instance.Read(entity);
                    return true;
                }

                value = default;
                return false;
            }

            [MethodImpl(AggressiveInlining)]
            internal static void _SetRaw(uint eid, IComponentOrTag value) {
                if (Instance.IsTag) {
                    Instance.Set(new Entity(eid));
                } else {
                    Instance.Set(new Entity(eid), (T)value);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal static void _SetRawDirect(uint eid, IComponentOrTag value) {
                if (!Instance.IsTag) {
                    var entityId = eid;
                    Instance.ComponentSegments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT]
                        [entityId & Const.ENTITIES_IN_SEGMENT_MASK] = (T) value;
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal static void _Set(uint eid) {
                if (Instance.IsTag) {
                    Instance.Set(new Entity(eid));
                }
                else {
                    Instance.Set(new Entity(eid), default);
                }
            }

            [MethodImpl(AggressiveInlining)]
            internal static bool _Add(uint eid) {
                Instance.Add(new Entity(eid), out var added);
                return added;
            }




            [MethodImpl(AggressiveInlining)]
            internal static bool _Delete(uint eid, HookReason reason) => Instance.Delete(new Entity(eid), reason);

            [MethodImpl(AggressiveInlining)]
            internal static bool _Copy(uint srcEid, uint dstEid) => Instance.Copy(new Entity(srcEid), new Entity(dstEid));

            [MethodImpl(AggressiveInlining)]
            internal static bool _Move(uint srcEid, uint dstEid) => Instance.Move(new Entity(srcEid), new Entity(dstEid));

            [MethodImpl(AggressiveInlining)]
            internal static IComponentOrTag _DefaultValue() => default(T);
            #endregion
            #endregion
        }
    }

    #region SERIALIZER UTILS
    /// <summary>
    /// Delegate for handling deserialization of a component type that has been removed from the codebase.
    /// When the serializer encounters a component Guid that no longer maps to a registered type,
    /// this delegate is invoked to read the saved data (so the binary stream stays aligned)
    /// and optionally migrate it to a replacement component on the entity.
    /// <para>
    /// Register migration handlers via the world serializer configuration to handle
    /// backward-compatible loading of save files after component types are deleted or replaced.
    /// </para>
    /// </summary>
    /// <param name="reader">Binary reader positioned at the start of the component's serialized data.</param>
    /// <param name="entity">The entity this component was saved on.</param>
    /// <param name="version">The schema version byte that was stored during serialization.</param>
    public delegate void EcsComponentDeleteMigrationReader(ref BinaryPackReader reader, World.Entity entity, byte version);
    
    internal static class ComponentSerializerUtils {
        
        [MethodImpl(AggressiveInlining)]
        internal static void SkipOneComponent(this ref BinaryPackReader reader) {
            var isTag = reader.ReadBool();
            if (!isTag) {
                var size = reader.ReadUshort();
                reader.SkipNext((uint) size); 
            }
        }
        
        [MethodImpl(AggressiveInlining)]
        internal static void DeleteOneComponentMigration(this ref BinaryPackReader reader, World.Entity entity, EcsComponentDeleteMigrationReader migration) {
            var isTag = reader.ReadBool();
            if (!isTag) {
                reader.ReadUshort();
                var oldVersion = reader.ReadByte();
                migration(ref reader, entity, oldVersion);
            }
            else {
                migration(ref reader, entity, 0);
            }
        }
        
        [MethodImpl(AggressiveInlining)]
        internal static void DeleteAllComponentMigration(this ref BinaryPackReader reader, EcsComponentDeleteMigrationReader migration, uint chunkIdx) {
            var isTag = reader.ReadBool();
            var notEmptyBlocks = reader.ReadUlong();

            if (notEmptyBlocks != 0) {
                #if !NET6_0_OR_GREATER
                var deBruijn = Utils.DeBruijn;
                #endif

                reader.ReadUlong(); // FullBlocks;
                
                var entity = new World.Entity();
                ref var entityId = ref entity.IdWithOffset;
                
                if (isTag) {
                    for (var segment = 0; segment < Const.SEGMENTS_IN_CHUNK; segment++) {
                        var segmentShift = segment << Const.BLOCKS_IN_SEGMENT_SHIFT;
                        var segmentMask = (notEmptyBlocks >> segmentShift) & 0b1111UL;

                        if (segmentMask == 0) continue;

                        var segmentBlockEntityBase = (uint)((chunkIdx << Const.ENTITIES_IN_CHUNK_SHIFT) + (segmentShift << Const.ENTITIES_IN_BLOCK_SHIFT) + Const.ENTITY_ID_OFFSET);

                        for (var blockIdx = 0; blockIdx < Const.BLOCKS_IN_SEGMENT; blockIdx++) {
                            var entitiesMask = reader.ReadUlong();
                            if (entitiesMask == 0) continue;

                            do {
                                var isolatedBit = entitiesMask & (ulong)-(long)entitiesMask;
                                #if NET6_0_OR_GREATER
                                var runStart = (byte)System.Numerics.BitOperations.TrailingZeroCount(entitiesMask);
                                #else
                                var runStart = deBruijn[(uint)((isolatedBit * 0x37E84A99DAE458FUL) >> 58)];
                                #endif

                                entityId = (uint)(segmentBlockEntityBase + (blockIdx << Const.ENTITIES_IN_BLOCK_SHIFT) + runStart);

                                do {
                                    migration(ref reader, entity, 0);
                                    isolatedBit <<= 1;
                                    entityId++;
                                } while ((entitiesMask & isolatedBit) != 0);

                                entitiesMask &= ~(isolatedBit - 1);
                            } while (entitiesMask != 0);
                        }
                    }
                    return;
                }
                
                var oldVersion = reader.ReadByte();
                var unmanagedStrategy = reader.ReadBool();

                while (notEmptyBlocks != 0) {
                    #if NET6_0_OR_GREATER
                    var chunkBlockIdx = (uint)System.Numerics.BitOperations.TrailingZeroCount(notEmptyBlocks);
                    #else
                    var chunkBlockIdx = (uint)deBruijn[(uint)(((notEmptyBlocks & (ulong)-(long)notEmptyBlocks) * 0x37E84A99DAE458FUL) >> 58)];
                    #endif
                    notEmptyBlocks &= notEmptyBlocks - 1;
                    var blockIdx = (byte)(chunkBlockIdx & Const.BLOCKS_IN_SEGMENT_MASK);
                    var segmentIdx = (chunkIdx << Const.SEGMENTS_IN_CHUNK_SHIFT) + (chunkBlockIdx >> Const.BLOCKS_IN_SEGMENT_SHIFT);
                    var entitiesMask = reader.ReadUlong();

                    var chunkBlockEntityId = (uint)((segmentIdx << Const.ENTITIES_IN_SEGMENT_SHIFT) +
                                                    (blockIdx << Const.ENTITIES_IN_BLOCK_SHIFT));
                    chunkBlockEntityId += Const.ENTITY_ID_OFFSET;

                    if (unmanagedStrategy) {
                        var firstBit = reader.ReadByte();
                        var rangeCount = (int)reader.ReadByte();
                        _ = reader.ReadNullFlag();
                        _ = reader.ReadInt(); // count
                        var byteSize = reader.ReadUint();
                        var oneSize = byteSize / (uint)rangeCount;
                        for (var cur = 0; cur < rangeCount; cur++) {
                            var bitIdx = firstBit + cur;
                            entityId = (uint)(chunkBlockEntityId + bitIdx);
                            var mask = 1UL << bitIdx;
                            if ((entitiesMask & mask) != 0) {
                                migration(ref reader, entity, oldVersion);
                            }
                            else {
                                reader.SkipNext(oneSize);
                            }
                        }
                    }
                    else {
                        do {
                            var isolatedBit = entitiesMask & (ulong)-(long)entitiesMask;
                            #if NET6_0_OR_GREATER
                            var runStart = (byte)System.Numerics.BitOperations.TrailingZeroCount(entitiesMask);
                            #else
                            var runStart = deBruijn[(uint)((isolatedBit * 0x37E84A99DAE458FUL) >> 58)];
                            #endif

                            entityId = chunkBlockEntityId + runStart;

                            do {
                                migration(ref reader, entity, oldVersion);
                                isolatedBit <<= 1;
                                entityId++;
                            } while ((entitiesMask & isolatedBit) != 0);

                            entitiesMask &= ~(isolatedBit - 1);
                        } while (entitiesMask != 0);
                    }
                }
            }
        }
    }
    #endregion
    

}