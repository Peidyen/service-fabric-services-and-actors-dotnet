﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using Microsoft.ServiceFabric.Services.Remoting.Description;

    internal class MethodBodyTypesBuilder : CodeBuilderModule
    {
        public MethodBodyTypesBuilder(ICodeBuilder codeBuilder)
            : base(codeBuilder)
        {
        }

        public MethodBodyTypesBuildResult Build(InterfaceDescription interfaceDescription)
        {
            var context = new CodeBuilderContext(
                this.CodeBuilder.Names.GetMethodBodyTypesAssemblyName(interfaceDescription.InterfaceType),
                this.CodeBuilder.Names.GetMethodBodyTypesAssemblyNamespace(interfaceDescription.InterfaceType),
                CodeBuilderAttribute.IsDebuggingEnabled(interfaceDescription.InterfaceType));

            var result = new MethodBodyTypesBuildResult(context)
            {
                MethodBodyTypesMap = new Dictionary<string, MethodBodyTypes>()
            };
            foreach (MethodDescription method in interfaceDescription.Methods)
            {
                result.MethodBodyTypesMap.Add(
                    method.Name,
                    Build(this.CodeBuilder.Names, context, method));
            }

            context.Complete();
            return result;
        }

        private static MethodBodyTypes Build(
            ICodeBuilderNames codeBuilderNames,
            CodeBuilderContext context,
            MethodDescription methodDescription)
        {
            var methodDataTypes = new MethodBodyTypes
            {
                RequestBodyType = null,
                ResponseBodyType = null,
                HasCancellationTokenArgument = methodDescription.HasCancellationToken
            };

            if (methodDescription.Arguments != null && methodDescription.Arguments.Length != 0)
            {
                methodDataTypes.RequestBodyType = BuildRequestBodyType(codeBuilderNames, context, methodDescription);
            }

            if (TypeUtility.IsTaskType(methodDescription.ReturnType) && methodDescription.ReturnType.GetTypeInfo().IsGenericType)
            {
                methodDataTypes.ResponseBodyType = BuildResponseBodyType(codeBuilderNames, context, methodDescription);
            }

            return methodDataTypes;
        }

        private static Type BuildRequestBodyType(
            ICodeBuilderNames codeBuilderNames,
            CodeBuilderContext context,
            MethodDescription methodDescription)
        {
            TypeBuilder requestBodyTypeBuilder = CodeBuilderUtils.CreateDataContractTypeBuilder(
                context.ModuleBuilder,
                context.AssemblyNamespace,
                codeBuilderNames.GetRequestBodyTypeName(methodDescription.Name),
                codeBuilderNames.GetDataContractNamespace());

            foreach (MethodArgumentDescription argument in methodDescription.Arguments)
            {
                CodeBuilderUtils.AddDataMemberField(
                    requestBodyTypeBuilder,
                    argument.ArgumentType,
                    argument.Name);
            }

            return requestBodyTypeBuilder.CreateTypeInfo().AsType();
        }

        private static Type BuildResponseBodyType(
            ICodeBuilderNames codeBuilderNames,
            CodeBuilderContext context,
            MethodDescription methodDescription)
        {
            TypeBuilder responseBodyTypeBuilder = CodeBuilderUtils.CreateDataContractTypeBuilder(
                context.ModuleBuilder,
                context.AssemblyNamespace,
                codeBuilderNames.GetResponseBodyTypeName(methodDescription.Name),
                codeBuilderNames.GetDataContractNamespace());


            Type returnDataType = methodDescription.ReturnType.GetGenericArguments()[0];
            CodeBuilderUtils.AddDataMemberField(
                responseBodyTypeBuilder,
                returnDataType,
                codeBuilderNames.RetVal);


            return responseBodyTypeBuilder.CreateTypeInfo().AsType();
        }
    }
}