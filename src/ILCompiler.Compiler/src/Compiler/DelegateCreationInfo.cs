// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Captures information required to generate a ReadyToRun helper to create a delegate type instance
    /// pointing to a specific target method.
    /// </summary>
    public sealed class DelegateCreationInfo
    {
        /// <summary>
        /// Gets the node corresponding to the method that initializes the delegate.
        /// </summary>
        public IMethodNode Constructor
        {
            get; private set;
        }

        /// <summary>
        /// Gets the node representing the target method of the delegate.
        /// </summary>
        public ISymbolNode Target
        {
            get; private set;
        }

        /// <summary>
        /// Gets an optional node passed as an additional argument to the constructor.
        /// </summary>
        public IMethodNode Thunk
        {
            get; private set;
        }

        private DelegateCreationInfo(IMethodNode constructor, ISymbolNode target, IMethodNode thunk = null)
        {
            Constructor = constructor;
            Target = target;
            Thunk = thunk;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="DelegateCreationInfo"/> set up to construct a delegate of type
        /// '<paramref name="delegateType"/>' pointing to '<paramref name="targetMethod"/>'.
        /// </summary>
        public static DelegateCreationInfo Create(TypeDesc delegateType, MethodDesc targetMethod, NodeFactory factory)
        {
            var context = (CompilerTypeSystemContext)delegateType.Context;
            var systemDelegate = targetMethod.Context.GetWellKnownType(WellKnownType.MulticastDelegate).BaseType;

            int paramCountTargetMethod = targetMethod.Signature.Length;
            if (!targetMethod.Signature.IsStatic)
            {
                paramCountTargetMethod++;
            }

            DelegateInfo delegateInfo = context.GetDelegateInfo(delegateType.GetTypeDefinition());
            int paramCountDelegateClosed = delegateInfo.Signature.Length + 1;
            bool closed = false;
            if (paramCountDelegateClosed == paramCountTargetMethod)
            {
                closed = true;
            }
            else
            {
                Debug.Assert(paramCountDelegateClosed == paramCountTargetMethod + 1);
            }

            if (targetMethod.Signature.IsStatic)
            {
                MethodDesc invokeThunk;
                if (!closed)
                {
                    // Open delegate to a static method
                    invokeThunk = delegateInfo.Thunks[DelegateThunkKind.OpenStaticThunk];
                }
                else
                {
                    // Closed delegate to a static method (i.e. delegate to an extension method that locks the first parameter)
                    invokeThunk = delegateInfo.Thunks[DelegateThunkKind.ClosedStaticThunk];
                }

                var instantiatedDelegateType = delegateType as InstantiatedType;
                if (instantiatedDelegateType != null)
                    invokeThunk = context.GetMethodForInstantiatedType(invokeThunk, instantiatedDelegateType);

                // We use InitializeClosedStaticThunk for both because RyuJIT generates same code for both,
                // but passes null as the first parameter for the open one.
                return new DelegateCreationInfo(
                    factory.MethodEntrypoint(systemDelegate.GetKnownMethod("InitializeClosedStaticThunk", null)),
                    factory.MethodEntrypoint(targetMethod),
                    factory.MethodEntrypoint(invokeThunk));
            }
            else
            {
                if (!closed)
                    throw new NotImplementedException("Open instance delegates");

                bool useUnboxingStub = targetMethod.OwningType.IsValueType;

                return new DelegateCreationInfo(
                    factory.MethodEntrypoint(systemDelegate.GetKnownMethod("InitializeClosedInstance", null)),
                    factory.MethodEntrypoint(targetMethod, useUnboxingStub));
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as DelegateCreationInfo;
            return other != null && Constructor == other.Constructor
                && Target == other.Target && Thunk == other.Thunk;
        }

        public override int GetHashCode()
        {
            return Constructor.GetHashCode() ^ Target.GetHashCode();
        }
    }
}
