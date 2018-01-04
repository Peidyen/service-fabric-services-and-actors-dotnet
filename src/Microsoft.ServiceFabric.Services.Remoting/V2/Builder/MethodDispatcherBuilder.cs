﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V2.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting.Builder;
    using Microsoft.ServiceFabric.Services.Remoting.Description;

    internal class MethodDispatcherBuilder<TMethodDispatcher> : CodeBuilderModule
        where TMethodDispatcher : MethodDispatcherBase
    {
        protected readonly Type MethodDispatcherBaseType;
        protected readonly MethodInfo ContinueWithResultMethodInfo;
        protected readonly MethodInfo ContinueWithMethodInfo;

        public MethodDispatcherBuilder(ICodeBuilder codeBuilder) : base(codeBuilder)
        {
            this.MethodDispatcherBaseType = typeof(TMethodDispatcher);

            this.ContinueWithResultMethodInfo = this.MethodDispatcherBaseType.GetMethod(
                "ContinueWithResult",
                BindingFlags.Instance | BindingFlags.NonPublic);

            this.ContinueWithMethodInfo = this.MethodDispatcherBaseType.GetMethod(
                "ContinueWith",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public MethodDispatcherBuildResult Build(InterfaceDescription interfaceDescription)
        {
            var context = new CodeBuilderContext(
                this.CodeBuilder.Names.GetMethodDispatcherAssemblyName(
                    interfaceDescription
                        .InterfaceType),
                this.CodeBuilder.Names.GetMethodDispatcherAssemblyNamespace(
                    interfaceDescription
                        .InterfaceType),
                CodeBuilderAttribute.IsDebuggingEnabled(interfaceDescription.InterfaceType));

            var result = new MethodDispatcherBuildResult(context);

            // build dispatcher class
            TypeBuilder classBuilder = CodeBuilderUtils.CreateClassBuilder(
                context.ModuleBuilder,
                context.AssemblyNamespace,
                this.CodeBuilder.Names.GetMethodDispatcherClassName(interfaceDescription.InterfaceType),
                this.MethodDispatcherBaseType);

            this.AddOnDispatchAsyncMethod(classBuilder, interfaceDescription);
            this.AddOnDispatchMethod(classBuilder, interfaceDescription);

            IReadOnlyDictionary<int, string> methodNameMap = GetMethodNameMap(interfaceDescription);

            // create the dispatcher type, instantiate and initialize it
            result.MethodDispatcherType = classBuilder.CreateTypeInfo().AsType();
            result.MethodDispatcher = (TMethodDispatcher) Activator.CreateInstance(result.MethodDispatcherType);
            var v2MethodDispatcherBase = (MethodDispatcherBase) result.MethodDispatcher;
            v2MethodDispatcherBase.Initialize(
                interfaceDescription,
                methodNameMap);

            context.Complete();
            return result;
        }


        private void AddOnDispatchMethod(TypeBuilder classBuilder, InterfaceDescription interfaceDescription)
        {
            MethodBuilder dispatchMethodImpl = CodeBuilderUtils.CreateProtectedMethodBuilder(
                classBuilder,
                "OnDispatch",
                typeof(void),
                typeof(int), // methodid
                typeof(object), // remoted object
                typeof(IServiceRemotingRequestMessageBody)); // requestBody


            ILGenerator ilGen = dispatchMethodImpl.GetILGenerator();

            LocalBuilder castedObject = ilGen.DeclareLocal(interfaceDescription.InterfaceType);
            ilGen.Emit(OpCodes.Ldarg_2); // load remoted object
            ilGen.Emit(OpCodes.Castclass, interfaceDescription.InterfaceType);
            ilGen.Emit(OpCodes.Stloc, castedObject); // store casted result to local 0

            foreach (MethodDescription methodDescription in interfaceDescription.Methods)
            {
                if (!TypeUtility.IsVoidType(methodDescription.ReturnType))
                {
                    continue;
                }

                Label elseLable = ilGen.DefineLabel();

                this.AddIfMethodIdInvokeBlock(
                    ilGen,
                    elseLable,
                    castedObject,
                    methodDescription,
                    interfaceDescription.InterfaceType.FullName);

                ilGen.MarkLabel(elseLable);
            }

            ilGen.ThrowException(typeof(MissingMethodException));
        }

        private void AddIfMethodIdInvokeBlock(
            ILGenerator ilGen, Label elseLabel, LocalBuilder castedObject,
            MethodDescription methodDescription, string interfaceName)
        {
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldc_I4, methodDescription.Id);
            ilGen.Emit(OpCodes.Bne_Un, elseLabel);

            Type requestBody = typeof(IServiceRemotingRequestMessageBody);

            // now invoke the method on the casted object
            ilGen.Emit(OpCodes.Ldloc, castedObject);

            if (methodDescription.Arguments != null && methodDescription.Arguments.Length != 0)
            {
                MethodInfo method = requestBody.GetMethod("GetParameter");
                for (var i = 0; i < methodDescription.Arguments.Length; i++)
                {
                    MethodArgumentDescription argument = methodDescription.Arguments[i];
                    // ReSharper disable once AssignNullToNotNullAttribute
                    // castedRequestBody is set to non-null in the previous if check on the same condition

                    ilGen.Emit(OpCodes.Ldarg_3);
                    ilGen.Emit(OpCodes.Ldc_I4, i);
                    ilGen.Emit(OpCodes.Ldstr, argument.Name);
                    ilGen.Emit(OpCodes.Ldtoken, argument.ArgumentType);
                    ilGen.Emit(OpCodes.Callvirt, method);
                    ilGen.Emit(OpCodes.Unbox_Any, argument.ArgumentType);
                }
            }


            ilGen.EmitCall(OpCodes.Callvirt, methodDescription.MethodInfo, null);
            ilGen.Emit(OpCodes.Ret);
        }

        private void AddOnDispatchAsyncMethod(
            TypeBuilder classBuilder,
            InterfaceDescription interfaceDescription)
        {
            MethodBuilder dispatchMethodImpl = CodeBuilderUtils.CreateProtectedMethodBuilder(
                classBuilder,
                "OnDispatchAsync",
                typeof(Task<IServiceRemotingResponseMessageBody>),
                typeof(int), // methodid
                typeof(object), // remoted object
                typeof(IServiceRemotingRequestMessageBody), // requestBody
                typeof(IServiceRemotingMessageBodyFactory), //remotingmessageBodyFactory
                typeof(CancellationToken)); // CancellationToken


            ILGenerator ilGen = dispatchMethodImpl.GetILGenerator();

            LocalBuilder castedObject = ilGen.DeclareLocal(interfaceDescription.InterfaceType);
            ilGen.Emit(OpCodes.Ldarg_2); // load remoted object
            ilGen.Emit(OpCodes.Castclass, interfaceDescription.InterfaceType);
            ilGen.Emit(OpCodes.Stloc, castedObject); // store casted result to local 0

            foreach (MethodDescription methodDescription in interfaceDescription.Methods)
            {
                if (!TypeUtility.IsTaskType(methodDescription.ReturnType))
                {
                    continue;
                }

                Label elseLable = ilGen.DefineLabel();

                this.AddIfMethodIdInvokeAsyncBlock(
                    ilGen,
                    elseLable,
                    castedObject,
                    methodDescription,
                    interfaceDescription.InterfaceType.FullName);

                ilGen.MarkLabel(elseLable);
            }

            ilGen.ThrowException(typeof(MissingMethodException));
        }

        private void AddIfMethodIdInvokeAsyncBlock(
            ILGenerator ilGen,
            Label elseLabel,
            LocalBuilder castedObject,
            MethodDescription methodDescription,
            string interfaceName
        )
        {
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldc_I4, methodDescription.Id);
            ilGen.Emit(OpCodes.Bne_Un, elseLabel);

            LocalBuilder invokeTask = ilGen.DeclareLocal(methodDescription.ReturnType);
            Type requestBody = typeof(IServiceRemotingRequestMessageBody);

            // now invoke the method on the casted object
            ilGen.Emit(OpCodes.Ldloc, castedObject);

            if (methodDescription.Arguments != null && methodDescription.Arguments.Length != 0)
            {
                MethodInfo method = requestBody.GetMethod("GetParameter");
                for (var i = 0; i < methodDescription.Arguments.Length; i++)
                {
                    MethodArgumentDescription argument = methodDescription.Arguments[i];
                    // ReSharper disable once AssignNullToNotNullAttribute
                    // castedRequestBody is set to non-null in the previous if check on the same condition

                    ilGen.Emit(OpCodes.Ldarg_3);
                    ilGen.Emit(OpCodes.Ldc_I4, i);
                    ilGen.Emit(OpCodes.Ldstr, argument.Name);
                    ilGen.Emit(OpCodes.Ldtoken, argument.ArgumentType);
                    ilGen.Emit(OpCodes.Callvirt, method);
                    ilGen.Emit(OpCodes.Unbox_Any, argument.ArgumentType);
                }
            }

            if (methodDescription.HasCancellationToken)
            {
                ilGen.Emit(OpCodes.Ldarg, 5);
            }

            ilGen.EmitCall(OpCodes.Callvirt, methodDescription.MethodInfo, null);
            ilGen.Emit(OpCodes.Stloc, invokeTask);

            // call the base method to return continuation task
            if (TypeUtility.IsTaskType(methodDescription.ReturnType) &&
                methodDescription.ReturnType.GetTypeInfo().IsGenericType)
            {
                // the return is Task<IServiceRemotingMessageBody>
                MethodInfo continueWithGenericMethodInfo = this.ContinueWithResultMethodInfo.MakeGenericMethod(
                    methodDescription.ReturnType.GenericTypeArguments[0]);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldstr, interfaceName);
                ilGen.Emit(OpCodes.Ldstr, methodDescription.Name);
                ilGen.Emit(OpCodes.Ldarg, 4);
                ilGen.Emit(OpCodes.Ldloc, invokeTask);
                ilGen.EmitCall(OpCodes.Call, continueWithGenericMethodInfo, null);
                ilGen.Emit(OpCodes.Ret);
            }
            else
            {
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldloc, invokeTask);
                ilGen.EmitCall(OpCodes.Call, this.ContinueWithMethodInfo, null);
                ilGen.Emit(OpCodes.Ret);
            }
        }
    }
}