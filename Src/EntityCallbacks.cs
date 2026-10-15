namespace FFS.Libraries.StaticEcs {
    /// <summary>
    /// Callback receiving an entity, used by bulk entity creation (<c>onCreate</c>) and by serializer callbacks.
    /// </summary>
    public delegate void QueryFunctionWithEntity(World.Entity entity);
}
