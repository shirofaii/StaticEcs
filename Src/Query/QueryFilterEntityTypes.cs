#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {
    /// <summary>
    /// Query filter that matches entities of one entity type. Built by <c>W.Is&lt;TEntityType&gt;()</c>.
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct EntityIs<T0> : IQueryFilter
        where T0 : struct, IEntityType { }
}
