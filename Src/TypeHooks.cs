using System;
using System.Reflection;
using FFS.Libraries.StaticPack;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace FFS.Libraries.StaticEcs {
    #if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, Const.IL2CPPNullChecks)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, Const.IL2CPPArrayBoundsChecks)]
    [Il2CppEagerStaticClassConstruction]
    #endif
    internal readonly struct ComponentHooks {
        internal readonly bool HasOnAdd;
        internal readonly bool HasOnDelete;
        internal readonly bool HasCopyTo;
        internal readonly bool HasWrite;
        internal readonly bool HasRead;
        internal readonly bool IsInternal;

        internal ComponentHooks(bool hasOnAdd, bool hasOnDelete, bool hasCopyTo, bool hasWrite, bool hasRead, bool isInternal) {
            HasOnAdd = hasOnAdd;
            HasOnDelete = hasOnDelete;
            HasCopyTo = hasCopyTo;
            HasWrite = hasWrite;
            HasRead = hasRead;
            IsInternal = isInternal;
        }
    }

    internal static class TypeHooks {
        internal static ComponentHooks DescribeComponent(Type type, object probe, bool isTag) {
            if (isTag) {
                return default;
            }

            if (probe is IComponentHookOverride hookOverride) {
                return new ComponentHooks(hookOverride.HasOnAdd(), hookOverride.HasOnDelete(), hookOverride.HasCopyTo(),
                    ComponentHasWrite(type), ComponentHasRead(type), probe is IComponentInternal);
            }

            return new ComponentHooks(ComponentHasOnAdd(type), ComponentHasOnDelete(type), ComponentHasCopyTo(type),
                ComponentHasWrite(type), ComponentHasRead(type), probe is IComponentInternal);
        }

        private static readonly Type[] ComponentOnAddParams = { typeof(World.Entity) };
        private static readonly Type[] ComponentOnDeleteParams = { typeof(World.Entity), typeof(HookReason) };
        private static readonly Type[] ComponentCopyToParams = { typeof(World.Entity), typeof(World.Entity) };
        private static readonly Type[] ComponentWriteParams = { typeof(BinaryPackWriter).MakeByRefType(), typeof(World.Entity) };
        private static readonly Type[] ComponentReadParams = { typeof(BinaryPackReader).MakeByRefType(), typeof(World.Entity), typeof(byte) };

        private static readonly Type[] LinkOnAddParams = { typeof(World.Entity), typeof(EntityGID) };
        private static readonly Type[] LinkOnDeleteParams = { typeof(World.Entity), typeof(EntityGID), typeof(HookReason) };
        private static readonly Type[] LinkCopyToParams = { typeof(World.Entity), typeof(World.Entity), typeof(EntityGID) };

        private static readonly Type[] MultiWriteParams = { typeof(BinaryPackWriter).MakeByRefType() };
        private static readonly Type[] MultiReadParams = { typeof(BinaryPackReader).MakeByRefType() };

        private static readonly Type[] SystemWriteParams = { typeof(BinaryPackWriter).MakeByRefType() };
        private static readonly Type[] SystemReadParams = { typeof(BinaryPackReader).MakeByRefType(), typeof(byte) };

        internal static bool ComponentHasOnAdd(Type type) => HasMethod(type, nameof(IComponentOrTag.OnAdd), ComponentOnAddParams, false);
        internal static bool ComponentHasOnDelete(Type type) => HasMethod(type, nameof(IComponentOrTag.OnDelete), ComponentOnDeleteParams, false);
        internal static bool ComponentHasCopyTo(Type type) => HasMethod(type, nameof(IComponentOrTag.CopyTo), ComponentCopyToParams, false);
        internal static bool ComponentHasWrite(Type type) => HasMethod(type, nameof(IComponentOrTag.Write), ComponentWriteParams, false);
        internal static bool ComponentHasRead(Type type) => HasMethod(type, nameof(IComponentOrTag.Read), ComponentReadParams, false);

        internal static bool LinkHasOnAdd(Type type) => HasMethod(type, nameof(ILinkType.OnAdd), LinkOnAddParams, false);
        internal static bool LinkHasOnDelete(Type type) => HasMethod(type, nameof(ILinkType.OnDelete), LinkOnDeleteParams, false);
        internal static bool LinkHasCopyTo(Type type) => HasMethod(type, nameof(ILinkType.CopyTo), LinkCopyToParams, false);

        internal static bool MultiHasWrite(Type type) => HasMethod(type, nameof(IMultiComponent.Write), MultiWriteParams, false);
        internal static bool MultiHasRead(Type type) => HasMethod(type, nameof(IMultiComponent.Read), MultiReadParams, false);

        internal static bool SystemHasUpdate(Type type) => HasExactMethod(type, nameof(ISystem.Update), Array.Empty<Type>());
        internal static bool SystemHasInit(Type type) => HasExactMethod(type, nameof(ISystem.Init), Array.Empty<Type>());
        internal static bool SystemHasDestroy(Type type) => HasExactMethod(type, nameof(ISystem.Destroy), Array.Empty<Type>());
        internal static bool SystemHasUpdateIsActive(Type type) => HasExactMethod(type, nameof(ISystem.UpdateIsActive), Array.Empty<Type>());
        internal static bool SystemHasWrite(Type type) => HasExactMethod(type, nameof(ISystem.Write), SystemWriteParams);
        internal static bool SystemHasRead(Type type) => HasExactMethod(type, nameof(ISystem.Read), SystemReadParams);

        private static bool HasMethod(
            #if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            #endif
            Type structType, string methodName, Type[] parameterTypes, bool genericDefinition) {
            var methods = structType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var methodInfo in methods) {
                if (methodInfo.Name != methodName || methodInfo.IsGenericMethodDefinition != genericDefinition) continue;
                var parameters = methodInfo.GetParameters();
                if (parameters.Length != parameterTypes.Length) continue;
                var match = true;
                for (var i = 0; i < parameters.Length; i++) {
                    if (parameterTypes[i].Name != parameters[i].ParameterType.Name) {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }

        private static bool HasExactMethod(
            #if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            #endif
            Type structType, string methodName, Type[] parameterTypes) {
            return structType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, binder: null, types: parameterTypes, modifiers: null) != null;
        }
    }
}
