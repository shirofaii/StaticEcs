#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {
    /// <summary>
    /// Query filter that matches every entity.
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct Nothing : IQueryFilter { }

    /// <summary>
    /// Combines two filters; an entity must pass both. Built by <c>.Without&lt;...&gt;()</c>, or by hand
    /// to combine a <see cref="ICustomQueryFilter"/> with other filters.
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct And<TFilter0, TFilter1> : IQueryFilter
        where TFilter0 : struct, IQueryFilter
        where TFilter1 : struct, IQueryFilter {
        internal readonly TFilter0 Filter0;
        internal readonly TFilter1 Filter1;

        public And(TFilter0 filter0, TFilter1 filter1) {
            Filter0 = filter0;
            Filter1 = filter1;
        }
    }
}
