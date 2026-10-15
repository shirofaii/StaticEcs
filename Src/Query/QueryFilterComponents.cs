#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {
    /// <summary>
    /// Query filter that requires an entity to have every listed component or tag, enabled.
    /// Built by <c>W.With&lt;A, B&gt;()</c>; available with 1–8 type parameters.
    /// <para>
    /// During strict-mode iteration, deleting or disabling a listed type on another entity of the
    /// iteration snapshot is forbidden (asserted in debug mode).
    /// </para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0> : IQueryFilter
        where T0 : struct, IComponentOrTag { }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0, T1> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag { }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0, T1, T2> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag { }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0, T1, T2, T3> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag { }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag { }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4, T5> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag { }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4, T5, T6> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag { }

    /// <inheritdoc cref="All{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct All<T0, T1, T2, T3, T4, T5, T6, T7> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag
        where T7 : struct, IComponentOrTag { }

    /// <summary>
    /// Query filter that excludes entities having any of the listed components or tags, enabled.
    /// Built by <c>.Without&lt;A, B&gt;()</c>; available with 1–8 type parameters.
    /// <para>
    /// During strict-mode iteration, adding or enabling a listed type on another entity of the
    /// iteration snapshot is forbidden (asserted in debug mode).
    /// </para>
    /// </summary>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0> : IQueryFilter
        where T0 : struct, IComponentOrTag { }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0, T1> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag { }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0, T1, T2> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag { }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0, T1, T2, T3> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag { }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag { }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4, T5> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag { }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4, T5, T6> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag { }

    /// <inheritdoc cref="None{T0}"/>
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    #endif
    public readonly struct None<T0, T1, T2, T3, T4, T5, T6, T7> : IQueryFilter
        where T0 : struct, IComponentOrTag
        where T1 : struct, IComponentOrTag
        where T2 : struct, IComponentOrTag
        where T3 : struct, IComponentOrTag
        where T4 : struct, IComponentOrTag
        where T5 : struct, IComponentOrTag
        where T6 : struct, IComponentOrTag
        where T7 : struct, IComponentOrTag { }
}
