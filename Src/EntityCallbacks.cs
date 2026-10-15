namespace FFS.Libraries.StaticEcs {
    /// <summary>
    /// Callback receiving an entity, used by bulk entity creation (<c>onCreate</c>) and by serializer callbacks.
    /// </summary>
    /// <typeparam name="TWorld">The world type.</typeparam>
    public delegate void QueryFunctionWithEntity<TWorld>(World<TWorld>.Entity entity)
        where TWorld : struct, IWorldType;
}
