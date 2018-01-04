// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Microsoft.ServiceFabric.Services.Remoting.Description
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ServiceFabric.Services.Common;

    internal abstract class InterfaceDescription
    {
        private readonly Type remotedInterfaceType;
        private readonly bool useCRCIdGeneration;
        private readonly int interfaceId;
        private readonly int interfaceIdV1;

        private readonly MethodDescription[] methods;

        protected InterfaceDescription(
            string remotedInterfaceKindName,
            Type remotedInterfaceType,
            bool useCRCIdGeneration,
            MethodReturnCheck methodReturnCheck = MethodReturnCheck.EnsureReturnsTask)
        {
            EnsureNotGeneric(remotedInterfaceKindName, remotedInterfaceType);

            this.remotedInterfaceType = remotedInterfaceType;
            this.useCRCIdGeneration = useCRCIdGeneration;
            if (this.useCRCIdGeneration)
            {
                this.interfaceId = IdUtil.ComputeIdWithCRC(remotedInterfaceType);
                //This is needed for backward compatibility support to V1 Stack like ActorEventproxy
                this.interfaceIdV1 = IdUtil.ComputeId(remotedInterfaceType);
            }
            else
            {
                this.interfaceId = IdUtil.ComputeId(remotedInterfaceType);
            }
            
            this.methods = GetMethodDescriptions(remotedInterfaceKindName, remotedInterfaceType, methodReturnCheck,useCRCIdGeneration);
        
        }

        public int V1Id
        {
            get { return this.interfaceIdV1; }
        }

        public int Id
        {
            get { return this.interfaceId; }
        }

        public Type InterfaceType
        {
            get { return this.remotedInterfaceType; }
        }

        public MethodDescription[] Methods
        {
            get { return this.methods; }
        }

        private static void EnsureNotGeneric(
            string remotedInterfaceKindName,
            Type remotedInterfaceType)
        {
            if (remotedInterfaceType.GetTypeInfo().IsGenericType ||
                remotedInterfaceType.GetTypeInfo().IsGenericTypeDefinition)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        SR.ErrorRemotedInterfaceIsGeneric,
                        remotedInterfaceKindName,
                        remotedInterfaceType.FullName),
                    remotedInterfaceKindName + "InterfaceType");
            }
        }


        private static MethodDescription[] GetMethodDescriptions(
            string remotedInterfaceKindName,
            Type remotedInterfaceType,
            MethodReturnCheck methodReturnCheck,
            bool useCRCIdGeneration
           )
        {
            EnsureValidMethods(remotedInterfaceKindName, remotedInterfaceType, methodReturnCheck);
            var methods = remotedInterfaceType.GetMethods();
            var methodDescriptions = new MethodDescription[methods.Length];
            for (int i = 0; i < methods.Length; i++)
            {
              methodDescriptions[i] = MethodDescription.Create(remotedInterfaceKindName, methods[i],useCRCIdGeneration);
            }
            return methodDescriptions;
        }

        private static void EnsureValidMethods(
            string remotedInterfaceKindName,
            Type remotedInterfaceType,
            MethodReturnCheck methodReturnCheck)
        {
            var methodNameSet = new HashSet<string>();
            foreach (var m in remotedInterfaceType.GetMethods())
            {
                EnsureNotOverloaded(remotedInterfaceKindName, remotedInterfaceType, m, methodNameSet);
                EnsureNotGeneric(remotedInterfaceKindName, remotedInterfaceType, m);
                EnsureNotVariableArgs(remotedInterfaceKindName, remotedInterfaceType, m);

                if (methodReturnCheck == MethodReturnCheck.EnsureReturnsTask)
                {
                    EnsureReturnsTask(remotedInterfaceKindName, remotedInterfaceType, m);
                }

                if (methodReturnCheck == MethodReturnCheck.EnsureReturnsVoid)
                {
                    EnsureReturnsVoid(remotedInterfaceKindName, remotedInterfaceType, m);
                }
            }
        }

        private static void EnsureNotOverloaded(string remotedInterfaceKindName, Type remotedInterfaceType,
            MethodInfo methodInfo,
            ISet<string> methodNameSet)
        {
            if (methodNameSet.Contains(methodInfo.Name))
            {
                ThrowArgumentExceptionForMethodChecks(remotedInterfaceKindName, remotedInterfaceType, methodInfo,
                    SR.ErrorRemotedMethodsIsOverloaded);
            }

            methodNameSet.Add((methodInfo.Name));
        }

        private static void EnsureNotGeneric(string remotedInterfaceKindName, Type remotedInterfaceType,
            MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
            {
                ThrowArgumentExceptionForMethodChecks(remotedInterfaceKindName, remotedInterfaceType, methodInfo,
                    SR.ErrorRemotedMethodHasGenerics);
            }
        }

        private static void EnsureNotVariableArgs(string remotedInterfaceKindName, Type remotedInterfaceType,
            MethodInfo methodInfo)
        {
            if (methodInfo.CallingConvention == CallingConventions.VarArgs)
            {
                ThrowArgumentExceptionForMethodChecks(remotedInterfaceKindName, remotedInterfaceType, methodInfo,
                    SR.ErrorRemotedMethodHasVaArgs);
            }
        }

        private static void EnsureReturnsTask(
            string remotedInterfaceKindName,
            Type remotedInterfaceType,
            MethodInfo methodInfo)
        {
            if (!TypeUtility.IsTaskType(methodInfo.ReturnType))
            {
                ThrowArgumentExceptionForMethodChecks(
                    remotedInterfaceKindName,
                    remotedInterfaceType,
                    methodInfo,
                    SR.ErrorRemotedMethodDoesNotReturnTask);
            }
        }

        private static void EnsureReturnsVoid(
            string remotedInterfaceKindName,
            Type remotedInterfaceType,
            MethodInfo methodInfo)
        {
            if (!TypeUtility.IsVoidType(methodInfo.ReturnType))
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        SR.ErrorRemotedMethodDoesNotReturnVoid,
                        remotedInterfaceKindName,
                        methodInfo.Name,
                        remotedInterfaceType.FullName,
                        methodInfo.ReturnType.FullName,
                        typeof(void)),
                    remotedInterfaceKindName + "InterfaceType");
            }
        }

        private static void ThrowArgumentExceptionForMethodChecks(
            string remotedInterfaceKindName,
            Type remotedInterfaceType,
            MethodInfo methodInfo,
            string resourceName)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    resourceName,
                    remotedInterfaceKindName,
                    methodInfo.Name,
                    remotedInterfaceType.FullName),
                remotedInterfaceKindName + "InterfaceType");
        }
    }
}