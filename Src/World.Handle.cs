#if ((DEBUG || FFS_ECS_ENABLE_DEBUG) && !FFS_ECS_DISABLE_DEBUG)
#define FFS_ECS_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
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
    /// Type-erased handle for accessing a <see cref="World"/> without knowing the concrete
    /// <c>World</c> type at compile time. Bridges the static-generic ECS world to runtime-dynamic
    /// access via function pointers.
    /// <para>
    /// <b>Primary use cases:</b> editor/inspector tools, debug visualizers, serialization frameworks,
    /// and plugin systems that need to interact with any world instance without generic type parameters.
    /// All operations go through unsafe function pointers stored at creation time, so there is minimal
    /// overhead beyond the indirection itself.
    /// </para>
    /// <para>
    /// Obtain a WorldHandle via <c>World.Handle</c> or internal creation methods.
    /// The handle remains valid as long as the world exists. Entity IDs used in this handle are
    /// raw <c>uint</c> values.
    /// </para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct WorldHandle {

        /// <summary>
        /// The <see cref="Type"/> of the <c>World</c> class that this handle represents.
        /// Useful for distinguishing between handles for different worlds at runtime.
        /// </summary>
        public readonly Type WorldType;

        internal static WorldHandle Create() => new(typeof(World));

        internal WorldHandle(Type worldType) {
            WorldType = worldType;
        }

        [MethodImpl(AggressiveInlining)]
        public WorldStatus Status() {
            return World._WorldStatus();
        }

        #region ENTITIES
        /// <summary>
        /// Returns the <see cref="GIDStatus"/> of the entity referenced by <paramref name="gid"/>.
        /// This is the type-erased equivalent of <see cref="EntityGID.Status()"/>.
        /// </summary>
        /// <param name="gid">The global entity identifier to check.</param>
        /// <returns>
        /// <see cref="StaticEcs.GIDStatus.Active"/> if the entity exists, version matches, and it is loaded,
        /// <see cref="StaticEcs.GIDStatus.NotActual"/> if the entity does not exist or version/cluster doesn't match,
        /// <see cref="StaticEcs.GIDStatus.NotLoaded"/> if the entity exists and version matches but is unloaded.
        /// </returns>
        [MethodImpl(AggressiveInlining)]
        public GIDStatus GIDStatus(EntityGID gid) {
            return World._GidStatus(gid);
        }

        /// <summary>
        /// Creates a new entity with the specified type in the specified cluster and returns its <see cref="EntityGID"/>.
        /// Unlike the generic <c>World.NewEntity</c> which returns an <c>Entity</c>
        /// struct, this returns a GID suitable for storage in type-erased contexts (editors, tools).
        /// </summary>
        /// <param name="entityType">Entity type (0–255). Entities of the same type are grouped into the same segments within a cluster.</param>
        /// <param name="clusterId">Cluster to create the entity in.</param>
        /// <returns>The <see cref="EntityGID"/> of the newly created entity.</returns>
        [MethodImpl(AggressiveInlining)]
        public EntityGID NewEntity(byte entityType, ushort clusterId = 0) {
            return World._NewEntity(entityType, clusterId);
        }

        /// <summary>
        /// Calculates the total number of alive entities across all chunks in the world.
        /// See <see cref="World.CalculateEntitiesCount"/>.
        /// </summary>
        /// <returns>Total entity count.</returns>
        [MethodImpl(AggressiveInlining)]
        public uint CalculateEntitiesCount() {
            return World._CalculateEntitiesCount();
        }

        /// <summary>
        /// Calculates the total entity slot capacity across all registered chunks.
        /// See <see cref="World.CalculateEntitiesCapacity"/>.
        /// </summary>
        /// <returns>Total allocated entity slot count.</returns>
        [MethodImpl(AggressiveInlining)]
        public uint CalculateEntitiesCapacity() {
            return World._CalculateEntitiesCapacity();
        }

        /// <summary>
        /// Returns whether the entity type with the specified byte ID is registered in this world.
        /// </summary>
        [MethodImpl(AggressiveInlining)]
        public bool IsEntityTypeRegistered(byte id) {
            return World._IsEntityTypeRegistered(id);
        }

        /// <summary>
        /// Calculates the number of alive entities of the specified type.
        /// </summary>
        [MethodImpl(AggressiveInlining)]
        public uint CalculateEntitiesCountByType(byte entityType) {
            return World._CalculateEntitiesCountByType(entityType);
        }

        /// <summary>
        /// Calculates the capacity (allocated segments × 256) for the specified entity type.
        /// </summary>
        [MethodImpl(AggressiveInlining)]
        public uint CalculateEntitiesCapacityByType(byte entityType) {
            return World._CalculateEntitiesCapacityByType(entityType);
        }

        /// <summary>
        /// Destroys the entity referenced by <paramref name="gid"/>. Idempotent: returns <c>false</c>
        /// if the entity is already destroyed or the GID is stale.
        /// </summary>
        /// <param name="gid">The global entity identifier to destroy.</param>
        /// <returns><c>true</c> if the entity was destroyed; <c>false</c> if already destroyed or GID is stale.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool DestroyEntity(EntityGID gid) {
            return World._DestroyEntity(gid);
        }

        /// <summary>
        /// Destroys every loaded entity in the world, triggering OnDelete hooks.
        /// The world remains initialized. See <see cref="World.DestroyAllLoadedEntities"/>.
        /// </summary>
        [MethodImpl(AggressiveInlining)]
        public void DestroyAllLoadedEntities() {
            World._DestroyAllEntities();
        }

        /// <summary>
        /// Destroys all entities within a specific cluster.
        /// See <see cref="World.DestroyAllEntitiesInCluster"/>.
        /// </summary>
        /// <param name="clusterId">Cluster identifier.</param>
        [MethodImpl(AggressiveInlining)]
        public void DestroyAllEntitiesInCluster(ushort clusterId) {
            World._DestroyAllEntitiesInCluster(clusterId);
        }

        /// <summary>
        /// Destroys all entities within a specific chunk.
        /// See <see cref="World.DestroyAllEntitiesInChunk"/>.
        /// </summary>
        /// <param name="chunkId">Chunk index.</param>
        [MethodImpl(AggressiveInlining)]
        public void DestroyAllEntitiesInChunk(uint chunkId) {
            World._DestroyAllEntitiesInChunk(chunkId);
        }
        #endregion

        #region COMPONENTS
        /// <summary>
        /// Attempts to obtain a type-erased <see cref="ComponentsHandle"/> for the specified
        /// component type. Returns <c>false</c> if the component type is not registered in this world.
        /// <para>
        /// Use this to dynamically discover and manipulate components when the concrete type
        /// is only known at runtime (e.g. editor inspectors iterating over all components on an entity).
        /// </para>
        /// </summary>
        /// <param name="componentType">The <see cref="Type"/> of the component struct (must implement <see cref="IComponent"/>).</param>
        /// <param name="handle">The resulting handle. Valid only when the method returns <c>true</c>.</param>
        /// <returns><c>true</c> if the component type is registered and the handle was created.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool TryGetComponentsHandle(Type componentType, out ComponentsHandle handle) {
            return World._TryGetComponentsHandle(componentType, out handle);
        }

        /// <summary>
        /// Returns a reference to the <see cref="ComponentsHandle"/> for the specified component type.
        /// Throws <see cref="StaticEcsException"/> if the component type is not registered.
        /// <para>
        /// Unlike <see cref="TryGetComponentsHandle"/>, this method avoids copying the entire handle
        /// struct by returning a direct reference into the internal storage.
        /// Use when the component type is guaranteed to be registered.
        /// </para>
        /// </summary>
        /// <param name="componentType">The <see cref="Type"/> of the component struct (must implement <see cref="IComponent"/>).</param>
        /// <returns>A readonly reference to the <see cref="ComponentsHandle"/>.</returns>
        [MethodImpl(AggressiveInlining)]
        public ref readonly ComponentsHandle GetComponentsHandle(Type componentType) {
            return ref World._GetComponentsHandle(componentType);
        }

        /// <summary>
        /// Returns handles for all component types registered in this world.
        /// Useful for editor tools that need to enumerate and display all available component types.
        /// </summary>
        /// <returns>Read-only span of all registered <see cref="ComponentsHandle"/> instances.</returns>
        [MethodImpl(AggressiveInlining)]
        public ReadOnlySpan<ComponentsHandle> GetAllComponentsHandles() {
            return World._GetAllComponentsHandles();
        }
        #endregion

        #region EVENTS
        #endregion

        #region RESOURCES
        /// <summary>
        /// Checks whether a singleton resource of the given runtime <see cref="Type"/> exists.
        /// Type-erased equivalent of <see cref="World.HasResource{TResource}()"/>.
        /// </summary>
        /// <param name="type">Runtime type of the resource.</param>
        /// <returns><c>true</c> if the resource exists.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool HasResource(Type type) {
            return World._HasResource(type);
        }

        /// <summary>
        /// Checks whether a keyed resource with the given string key exists.
        /// </summary>
        /// <param name="key">String key identifying the resource.</param>
        /// <returns><c>true</c> if the keyed resource exists.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool HasResource(string key) {
            return World._HasResourceByKey(key);
        }

        /// <summary>
        /// Returns the singleton resource of the given runtime type as a boxed object.
        /// The resource must have been previously set.
        /// </summary>
        /// <param name="type">Runtime type of the resource.</param>
        /// <returns>The resource value as <see cref="IResource"/>.</returns>
        [MethodImpl(AggressiveInlining)]
        public IResource GetResource(Type type) {
            return World._GetResource(type);
        }

        /// <summary>
        /// Returns a keyed resource as <see cref="IResource"/>.
        /// </summary>
        /// <param name="key">String key identifying the resource.</param>
        /// <returns>The resource value as <see cref="IResource"/>.</returns>
        [MethodImpl(AggressiveInlining)]
        public IResource GetResource(string key) {
            return World._GetResourceByKey(key);
        }

        /// <summary>
        /// Removes the singleton resource of the given runtime type.
        /// </summary>
        /// <param name="type">Runtime type of the resource to remove.</param>
        [MethodImpl(AggressiveInlining)]
        public void RemoveResource(Type type) {
            World._RemoveResource(type);
        }

        /// <summary>
        /// Removes a keyed resource.
        /// </summary>
        /// <param name="key">String key of the resource to remove.</param>
        [MethodImpl(AggressiveInlining)]
        public void RemoveResource(string key) {
            World._RemoveResourceByKey(key);
        }

        /// <summary>
        /// Sets (or replaces) a singleton resource, accepting the value as <see cref="IResource"/>.
        /// </summary>
        /// <param name="type">Runtime type of the resource.</param>
        /// <param name="value">The resource value (will be stored as-is; must match the type).</param>
        /// <param name="clearOnDestroy">If <c>true</c>, the resource is cleared when the world is destroyed.</param>
        [MethodImpl(AggressiveInlining)]
        public void SetResource(Type type, IResource value, bool clearOnDestroy) {
            World._SetResource(type, value, clearOnDestroy);
        }

        /// <summary>
        /// Sets (or replaces) a keyed resource, accepting the value as <see cref="IResource"/>.
        /// </summary>
        /// <param name="key">String key for this resource instance.</param>
        /// <param name="value">The resource value.</param>
        /// <param name="clearOnDestroy">If <c>true</c>, the resource is cleared when the world is destroyed.</param>
        [MethodImpl(AggressiveInlining)]
        public void SetResource(string key, IResource value, bool clearOnDestroy) {
            World._SetResourceByKey(key, value, clearOnDestroy);
        }

        /// <summary>
        /// Returns all string keys of currently stored keyed resources.
        /// Useful for editor/inspector enumeration.
        /// </summary>
        /// <returns>Read-only collection of resource keys.</returns>
        [MethodImpl(AggressiveInlining)]
        public IReadOnlyCollection<string> GetAllResourcesKeys() {
            return World._GetAllResourcesKeys();
        }

        /// <summary>
        /// Returns the <see cref="Type"/> objects of all currently stored singleton (non-keyed) resources.
        /// Useful for editor/inspector enumeration.
        /// </summary>
        /// <returns>Read-only collection of resource types.</returns>
        [MethodImpl(AggressiveInlining)]
        public IReadOnlyCollection<Type> GetAllResourcesTypes() {
            return World._GetAllResourcesTypes();
        }
        #endregion

        #region SYSTEMS
        /// <summary>
        /// Returns handles for every <see cref="World.Systems{TSystemsType}"/> pipeline
        /// currently registered for this world. Useful for editor/inspector tools that need
        /// to enumerate all systems groups and their resources without knowing the concrete
        /// <c>TSystemsType</c> at compile time.
        /// </summary>
        /// <returns>Read-only list of <see cref="StaticEcs.SystemsHandle"/> instances, one per active pipeline.</returns>
        [MethodImpl(AggressiveInlining)]
        public IReadOnlyList<SystemsHandle> GetAllSystemsHandles() {
            return World._GetAllSystemsHandles();
        }

        /// <summary>
        /// Looks up a <see cref="StaticEcs.SystemsHandle"/> by its <c>TSystemsType</c> runtime type.
        /// </summary>
        /// <param name="systemsType">The runtime <see cref="Type"/> of the <c>TSystemsType</c> struct.</param>
        /// <param name="handle">The resulting handle. Valid only when the method returns <c>true</c>.</param>
        /// <returns><c>true</c> if a systems pipeline with the given type is registered for this world.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool TryGetSystemsHandle(Type systemsType, out SystemsHandle handle) {
            return World._TryGetSystemsHandle(systemsType, out handle);
        }
        #endregion
    }

    /// <summary>
    /// Type-erased handle for accessing a specific component type's storage without knowing
    /// the concrete component type at compile time. Provides runtime-dynamic operations on
    /// components: add, remove, read/write (boxed), enable/disable, copy, move, and bitmap queries.
    /// <para>
    /// Obtained via <see cref="WorldHandle.TryGetComponentsHandle"/> or
    /// <see cref="WorldHandle.GetAllComponentsHandles"/>. All entity IDs used here are raw
    /// <c>uint</c> values (with <c>ENTITY_ID_OFFSET</c> applied).
    /// </para>
    /// <para>
    /// <b>Performance note:</b> These operations involve boxing for component values (via
    /// <see cref="IComponent"/>) and function pointer indirection. For hot-path code, use the
    /// generic <c>World.Components&lt;T&gt;</c> API instead. ComponentsHandle is
    /// intended for editor tools, serialization, and other non-performance-critical scenarios.
    /// </para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct ComponentsHandle {
        // base
        private readonly unsafe delegate*<uint, ulong[][], ushort, void> _initialize;
        private readonly unsafe delegate*<uint, ulong[][], void> _resize;
        private readonly unsafe delegate*<void> _destroy;
        private readonly unsafe delegate*<void> _hardReset;
        private readonly unsafe delegate*<uint, void> _hardResetChunk;
        private readonly unsafe delegate*<StringBuilder, uint, void> _tryToStringComponent;
        // serialization
        private readonly unsafe delegate*<ref BinaryPackWriter, uint, void> _writeChunk;
        private readonly unsafe delegate*<ref BinaryPackReader, uint, void> _readChunk;
        private readonly unsafe delegate*<ref BinaryPackWriter, uint, bool, bool> _writeEntity;
        private readonly unsafe delegate*<ref BinaryPackReader, uint, void> _readEntity;
        // chunks & segments
        private readonly unsafe delegate*<Array> _componentsSegments;
        private readonly unsafe delegate*<ulong, uint, byte, HookReason, void> _batchDelete;
        // entity
        private readonly unsafe delegate*<uint, out IComponentOrTag, bool> _tryGetRaw;
        private readonly unsafe delegate*<uint, IComponentOrTag, void> _setRaw;
        private readonly unsafe delegate*<uint, IComponentOrTag, void> _setRawDirect;
        private readonly unsafe delegate*<uint, bool> _add;
        private readonly unsafe delegate*<uint, HookReason, bool> _delete;
        private readonly unsafe delegate*<uint, uint, bool> _copy;
        private readonly unsafe delegate*<uint, uint, bool> _move;
        private readonly unsafe delegate*<uint, void> _set;
        private readonly unsafe delegate*<IComponentOrTag> _defaultValue;

        /// <summary>
        /// The <see cref="Type"/> of the component struct this handle operates on.
        /// </summary>
        public readonly Type ComponentType;

        /// <summary>
        /// The <see cref="Type"/> of the <c>World</c> class this handle's world belongs to.
        /// </summary>
        public readonly Type WorldType;

        /// <summary>
        /// Stable GUID assigned to this component type, used for serialization identification.
        /// Allows renaming the component type without breaking saved data.
        /// </summary>
        public readonly Guid Guid;

        /// <summary>
        /// Runtime-assigned numeric ID for this component type within the world.
        /// Used internally for bitmap indexing. Not stable across runs — do not serialize.
        /// </summary>
        public readonly ushort DynamicId;

        /// <summary>
        /// Whether this handle represents a tag type (zero-size marker component with no data arrays).
        /// </summary>
        public readonly bool IsTag;

        /// <summary>
        /// When <c>true</c>, data of this component/tag type is excluded from all world snapshot
        /// serialization. Mirrors <see cref="INonSerializable"/> on the registered type.
        /// </summary>
        public readonly bool NonSerializable;

        private readonly ComponentMasks _masks;

        internal ComponentMasks Masks => _masks;

        #if NET5_0_OR_GREATER
        [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "Component metadata is preserved by the registration path.")]
        #endif
        internal static unsafe ComponentsHandle Create<TComponent>()
            where TComponent : struct, IComponentOrTag {
            #if FFS_ECS_DEBUG
            ref var instance = ref World.Components<TComponent>.instance;
            if (!instance.IsRegistered) {
                throw new StaticEcsException($"Component {typeof(TComponent).GenericName()} is not registered.");
            }
            #else
            ref var instance = ref World.Components<TComponent>.Instance;
            #endif
            
            return new ComponentsHandle(
                typeof(TComponent),
                typeof(World),
                instance.Guid,
                instance.DynamicId,
                instance.IsTag,
                instance.NonSerializable,
                instance.Masks,
                &World.Components<TComponent>._Initialize,
                &World.Components<TComponent>._Resize,
                &World.Components<TComponent>._Destroy,
                &World.Components<TComponent>._HardReset,
                &World.Components<TComponent>._HardResetChunk,
                &World.Components<TComponent>._TryToStringComponent,
                &World.Components<TComponent>._WriteChunk,
                &World.Components<TComponent>._ReadChunk,
                &World.Components<TComponent>._WriteEntity,
                &World.Components<TComponent>._ReadEntity,
                &World.Components<TComponent>._ComponentsSegments,
                &World.Components<TComponent>._BatchDelete,
                &World.Components<TComponent>._TryGetRaw,
                &World.Components<TComponent>._SetRaw,
                &World.Components<TComponent>._SetRawDirect,
                &World.Components<TComponent>._Add,
                &World.Components<TComponent>._Delete,
                &World.Components<TComponent>._Copy,
                &World.Components<TComponent>._Move,
                &World.Components<TComponent>._Set,
                &World.Components<TComponent>._DefaultValue
            );
        }

        internal unsafe ComponentsHandle(
            Type componentType,
            Type worldType,
            Guid guid,
            ushort dynamicId,
            bool isTag,
            bool nonSerializable,
            ComponentMasks masks,
            delegate*<uint, ulong[][], ushort, void> initialize,
            delegate*<uint, ulong[][], void> resize,
            delegate*<void> destroy,
            delegate*<void> hardReset,
            delegate*<uint, void> hardResetChunk,
            delegate*<StringBuilder, uint, void> tryToStringComponent,
            delegate*<ref BinaryPackWriter, uint, void> writeChunk,
            delegate*<ref BinaryPackReader, uint, void> readChunk,
            delegate*<ref BinaryPackWriter, uint, bool, bool> writeEntity,
            delegate*<ref BinaryPackReader, uint, void> readEntity,
            delegate*<Array> componentsSegments,
            delegate*<ulong, uint, byte, HookReason, void> batchDelete,
            delegate*<uint, out IComponentOrTag, bool> tryGetRaw,
            delegate*<uint, IComponentOrTag, void> setRaw,
            delegate*<uint, IComponentOrTag, void> setRawDirect,
            delegate*<uint, bool> add,
            delegate*<uint, HookReason, bool> delete,
            delegate*<uint, uint, bool> copy,
            delegate*<uint, uint, bool> move,
            delegate*<uint, void> set,
            delegate*<IComponentOrTag> defaultValue
            ) {
            ComponentType = componentType;
            WorldType = worldType;
            Guid = guid;
            DynamicId = dynamicId;
            IsTag = isTag;
            NonSerializable = nonSerializable;
            _masks = masks;
            _initialize = initialize;
            _resize = resize;
            _destroy = destroy;
            _hardReset = hardReset;
            _hardResetChunk = hardResetChunk;
            _tryToStringComponent = tryToStringComponent;
            _writeChunk = writeChunk;
            _readChunk = readChunk;
            _writeEntity = writeEntity;
            _readEntity = readEntity;
            _componentsSegments = componentsSegments;
            _batchDelete = batchDelete;
            _tryGetRaw = tryGetRaw;
            _setRaw = setRaw;
            _setRawDirect = setRawDirect;
            _add = add;
            _delete = delete;
            _copy = copy;
            _move = move;
            _set = set;
            _defaultValue = defaultValue;
        }

        #region BASE
        [MethodImpl(AggressiveInlining)]
        internal void Initialize(uint chunksCapacity, ulong[][] chunkHeuristicMask, ushort chunkHeuristicMaskLen) {
            unsafe { _initialize(chunksCapacity, chunkHeuristicMask, chunkHeuristicMaskLen); }
        }

        [MethodImpl(AggressiveInlining)]
        internal void Resize(uint chunksCapacity, ulong[][] chunkHeuristicMask) {
            unsafe { _resize(chunksCapacity, chunkHeuristicMask); }
        }

        [MethodImpl(AggressiveInlining)]
        internal void Destroy() {
            unsafe { _destroy(); }
        }

        [MethodImpl(AggressiveInlining)]
        internal void HardReset() {
            unsafe { _hardReset(); }
        }

        [MethodImpl(AggressiveInlining)]
        internal void HardResetChunk(uint chunkIdx) {
            unsafe { _hardResetChunk(chunkIdx); }
        }

        #endregion

        #region SERIALIZATION
        [MethodImpl(AggressiveInlining)]
        internal void WriteChunk(ref BinaryPackWriter writer, uint chunkIdx) {
            unsafe { _writeChunk(ref writer, chunkIdx); }
        }

        [MethodImpl(AggressiveInlining)]
        internal void ReadChunk(ref BinaryPackReader reader, uint chunkIdx) {
            unsafe { _readChunk(ref reader, chunkIdx); }
        }

        [MethodImpl(AggressiveInlining)]
        internal bool WriteEntity(ref BinaryPackWriter writer, uint entityId, bool deleteComponent) {
            unsafe { return _writeEntity(ref writer, entityId, deleteComponent); }
        }

        [MethodImpl(AggressiveInlining)]
        internal void ReadEntity(ref BinaryPackReader reader, uint entityId) {
            unsafe { _readEntity(ref reader, entityId); }
        }
        #endregion

        #region CHUNKS_AND_SEGMENTS
        /// <summary>
        /// Returns the <see cref="StaticEcs.HeuristicChunk"/> for this component type at the given chunk.
        /// The heuristic chunk contains bitmap pairs (NotEmptyBlocks / FullBlocks) that enable
        /// fast chunk-level filtering during queries — checking if a chunk has any entities with
        /// this component without scanning individual blocks.
        /// </summary>
        /// <param name="chunkIdx">Chunk index.</param>
        /// <returns>Heuristic bitmap data for the specified chunk.</returns>
        [MethodImpl(AggressiveInlining)]
        public HeuristicChunk HeuristicChunk(uint chunkIdx) {
            return _masks.HeuristicChunks[chunkIdx];
        }

        /// <summary>
        /// Returns the 64-bit bitmask of entities that have this component in the <b>enabled</b>
        /// state within the specified segment and block. Each set bit corresponds to an entity
        /// that has the component, and it is enabled.
        /// <para>
        /// Used for low-level iteration, custom query implementations, and debug visualization.
        /// A segment contains 256 entities divided into 4 blocks of 64.
        /// </para>
        /// </summary>
        /// <param name="segmentIdx">Segment index (entityId >> ENTITIES_IN_SEGMENT_SHIFT).</param>
        /// <param name="blockIdx">Block index within the segment (0..3).</param>
        /// <returns>Bitmask where set bits indicate entities with this component.</returns>
        [MethodImpl(AggressiveInlining)]
        public ulong Mask(uint segmentIdx, int blockIdx) {
            return _masks.Mask(segmentIdx, blockIdx);
        }

        /// <summary>
        /// Batch-deletes this component from all entities indicated by the set bits of
        /// <paramref name="entitiesMaskFilter"/> within the specified segment and block.
        /// Triggers OnDelete hooks for each affected entity.
        /// <para>
        /// Used for bulk cleanup operations, e.g. removing a component from all entities
        /// in a segment block as part of chunk unloading or mass entity destruction.
        /// </para>
        /// </summary>
        /// <param name="entitiesMaskFilter">64-bit mask selecting which entities in the block to process.</param>
        /// <param name="segmentIdx">Segment index.</param>
        /// <param name="segmentBlockIdx">Block index within the segment (0..3).</param>
        /// <param name="reason">The reason for deletion, forwarded to OnDelete hooks.</param>
        [MethodImpl(AggressiveInlining)]
        public void BatchDelete(ulong entitiesMaskFilter, uint segmentIdx, byte segmentBlockIdx, HookReason reason = HookReason.Default) {
            unsafe { _batchDelete(entitiesMaskFilter, segmentIdx, segmentBlockIdx, reason); }
        }

        /// <summary>
        /// Checks whether the specified segment has been allocated for this component type.
        /// A segment is allocated when at least one entity in its range has (or had) this component.
        /// Unallocated segments have no memory footprint for this component.
        /// </summary>
        /// <param name="segmentIdx">Segment index.</param>
        /// <returns><c>true</c> if the segment is allocated; <c>false</c> if null (no memory).</returns>
        [MethodImpl(AggressiveInlining)]
        public bool IsSegmentAllocated(uint segmentIdx) {
            return segmentIdx < _masks.Segments.Length && _masks.Segments[segmentIdx] != null;
        }

        /// <summary>
        /// Calculates the current storage capacity (number of allocated component slots)
        /// for this component type. Grows automatically as chunks are registered.
        /// </summary>
        /// <returns>Total allocated slot count.</returns>
        [MethodImpl(AggressiveInlining)]
        public int CalculateCapacity() {
            return _masks.CalculateCapacity();
        }

        /// <summary>
        /// Calculates the number of entities that currently have this component (enabled or disabled).
        /// Scans the bitmaps — not a cached counter.
        /// </summary>
        /// <returns>Number of entities with this component.</returns>
        [MethodImpl(AggressiveInlining)]
        public uint CalculateCount() {
            return _masks.CalculateCount();
        }
        #endregion

        #region ENTITY
        /// <summary>
        /// Appends a string representation of this component's value on the given entity
        /// to the <see cref="StringBuilder"/>. Does nothing if the entity doesn't have this component.
        /// Used by debug visualizers and entity inspector tools to display component data.
        /// </summary>
        /// <param name="builder">StringBuilder to append the component description to.</param>
        /// <param name="entityId">Raw entity ID (with ENTITY_ID_OFFSET applied).</param>
        [MethodImpl(AggressiveInlining)]
        public void TryToStringComponent(StringBuilder builder, uint entityId) {
            unsafe { _tryToStringComponent(builder, entityId); }
        }

        /// <summary>
        /// Attempts to retrieve the component/tag value for an entity as a boxed <see cref="IComponent"/>.
        /// Returns <c>false</c> if the entity does not have this component.
        /// <para>
        /// The returned value is a boxed copy — modifications to it do not affect the stored component.
        /// Use <see cref="SetRaw"/> to write back modified values.
        /// </para>
        /// </summary>
        /// <param name="entityId">Raw entity ID.</param>
        /// <param name="value">The boxed component value (valid only when returning <c>true</c>).</param>
        /// <returns><c>true</c> if the entity has this component.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool TryGetRaw(uint entityId, out IComponentOrTag value) {
            unsafe { return _tryGetRaw(entityId, out value); }
        }

        /// <summary>
        /// Sets the component/tag value on an entity from a boxed <see cref="IComponent"/>.
        /// The entity must already have this component (call <see cref="Add(uint)"/> first if needed).
        /// The boxed value is unboxed and copied into the component storage.
        /// </summary>
        /// <param name="entityId">Raw entity ID.</param>
        /// <param name="value">The boxed component value to write. Must match the component type.</param>
        [MethodImpl(AggressiveInlining)]
        public void SetRaw(uint entityId, IComponentOrTag value) {
            unsafe { _setRaw(entityId, value); }
        }

        [MethodImpl(AggressiveInlining)]
        internal void SetRawDirect(uint entityId, IComponentOrTag value) {
            unsafe { _setRawDirect(entityId, value); }
        }

        [MethodImpl(AggressiveInlining)]
        internal ref T Ref<T>(uint entityId) where T : struct, IComponent {
            unsafe {
                var segments = (T[][])_componentsSegments();
                return ref segments[entityId >> Const.ENTITIES_IN_SEGMENT_SHIFT][entityId & Const.ENTITIES_IN_SEGMENT_MASK];
            }
        }

        /// <summary>
        /// Adds this component (default-initialized) to an entity. Reports whether the component
        /// was actually added via the <paramref name="added"/> out parameter — returns <c>false</c>
        /// if the entity already had this component (no-op in that case).
        /// Triggers the OnAdd hook if the component was newly added.
        /// </summary>
        /// <param name="entityId">Raw entity ID.</param>
        /// <param name="added"><c>true</c> if the component was newly added; <c>false</c> if it already existed.</param>
        [MethodImpl(AggressiveInlining)]
        public void Add(uint entityId, out bool added) {
            unsafe { added = _add(entityId); }
        }

        /// <summary>
        /// Adds this component (default-initialized) to an entity.
        /// If the entity already has the component, this is a no-op.
        /// Triggers the OnAdd hook if newly added.
        /// </summary>
        /// <param name="entityId">Raw entity ID.</param>
        [MethodImpl(AggressiveInlining)]
        public void Add(uint entityId) {
            unsafe { _add(entityId); }
        }

        /// <summary>
        /// Checks whether an entity has this component in any state (enabled or disabled).
        /// </summary>
        /// <param name="entityId">Raw entity ID.</param>
        /// <returns><c>true</c> if the entity has this component.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool Has(uint entityId) {
            return _masks.Has(entityId);
        }

        /// <summary>
        /// Removes this component from an entity entirely (both data and bitmap presence).
        /// Triggers the OnDelete hook if the component existed.
        /// </summary>
        /// <param name="entityId">Raw entity ID.</param>
        /// <param name="reason">The reason for deletion, forwarded to the OnDelete hook.</param>
        /// <returns><c>true</c> if the component was present and removed; <c>false</c> if the entity didn't have it.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool Delete(uint entityId, HookReason reason = HookReason.Default) {
            unsafe { return _delete(entityId, reason); }
        }

        /// <summary>
        /// Copies this component's value from one entity to another. If the destination entity
        /// doesn't have the component, it is added first. The source entity retains its component.
        /// Triggers the component's CopyTo hook if defined.
        /// </summary>
        /// <param name="srcEntityId">Source entity's raw ID.</param>
        /// <param name="dstEntityId">Destination entity's raw ID.</param>
        /// <returns><c>true</c> if the source had the component and it was copied.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool Copy(uint srcEntityId, uint dstEntityId) {
            unsafe { return _copy(srcEntityId, dstEntityId); }
        }

        /// <summary>
        /// Moves this component from one entity to another: copies the value to the destination
        /// and removes it from the source. If the destination already has the component, its value
        /// is overwritten. Triggers OnDelete on the source and OnAdd (or CopyTo) on the destination.
        /// </summary>
        /// <param name="srcEntityId">Source entity's raw ID (component will be removed).</param>
        /// <param name="dstEntityId">Destination entity's raw ID (component will be added/overwritten).</param>
        /// <returns><c>true</c> if the source had the component and it was moved.</returns>
        [MethodImpl(AggressiveInlining)]
        public bool Move(uint srcEntityId, uint dstEntityId) {
            unsafe { return _move(srcEntityId, dstEntityId); }
        }

        /// <summary>
        /// Sets a default components/tag on an entity. Tags are binary — either present or absent.
        /// </summary>
        /// <param name="entityId">Raw entity ID.</param>
        [MethodImpl(AggressiveInlining)]
        public void Set(uint entityId) {
            unsafe { _set(entityId); }
        }

        /// <summary>
        /// Returns a boxed default instance of the component/tag type.
        /// this always returns a default-constructed instance. Useful for reflection-based tools
        /// that need a prototype value.
        /// </summary>
        /// <returns>A boxed default instance of the component or tag.</returns>
        [MethodImpl(AggressiveInlining)]
        public IComponentOrTag DefaultValue() {
            unsafe { return _defaultValue(); }
        }
        #endregion
    }

}