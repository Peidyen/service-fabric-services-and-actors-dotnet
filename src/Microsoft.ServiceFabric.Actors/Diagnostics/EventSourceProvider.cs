﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Fabric;
    using System.Reflection;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Description;

    internal class EventSourceProvider
    {
        internal readonly ActorTypeInformation actorTypeInformation;

        private readonly string actorType;
        private readonly ServiceContext serviceContext;
        private readonly ActorFrameworkEventSource writer;

        private Dictionary<long, ActorMethodInfo> actorMethodInfo;

        internal EventSourceProvider(ServiceContext serviceContext, ActorTypeInformation actorTypeInformation)
        {
            this.serviceContext = serviceContext;
            this.actorTypeInformation = actorTypeInformation;
            this.actorType = actorTypeInformation.ImplementationType.ToString();

            this.writer = ActorFrameworkEventSource.Writer;
        }

        internal void RegisterWithDiagnosticsEventManager(DiagnosticsEventManager diagnosticsEventManager)
        {
            this.InitializeActorMethodInfo(diagnosticsEventManager);

            diagnosticsEventManager.OnActorChangeRole += this.OnActorChangeRole;
            diagnosticsEventManager.OnActorActivated += this.OnActorActivated;
            diagnosticsEventManager.OnActorDeactivated += this.OnActorDeactivated;
            diagnosticsEventManager.OnActorMethodStart += this.OnActorMethodStart;
            diagnosticsEventManager.OnActorMethodFinish += this.OnActorMethodFinish;
            diagnosticsEventManager.OnSaveActorStateStart += this.OnSaveActorStateStart;
            diagnosticsEventManager.OnSaveActorStateFinish += this.OnSaveActorStateFinish;
            diagnosticsEventManager.OnPendingActorMethodCallsUpdated += this.OnPendingActorMethodCallsUpdated;
        }

        internal virtual void InitializeActorMethodInfo(DiagnosticsEventManager diagnosticsEventManager)
        {
            this.actorMethodInfo = new Dictionary<long, ActorMethodInfo>();

            foreach (Type actorInterfaceType in this.actorTypeInformation.InterfaceTypes)
            {
                int interfaceId;
                MethodDescription[] actorInterfaceMethodDescriptions;
                diagnosticsEventManager.ActorMethodFriendlyNameBuilder.GetActorInterfaceMethodDescriptions(
                    actorInterfaceType,
                    out interfaceId,
                    out actorInterfaceMethodDescriptions);
                this.InitializeActorMethodInfo(actorInterfaceMethodDescriptions, interfaceId, this.actorMethodInfo);
            }
        }

        internal void InitializeActorMethodInfo(
            MethodDescription[] actorInterfaceMethodDescriptions, int interfaceId,
            Dictionary<long, ActorMethodInfo> actorMethodInfos)
        {
            foreach (MethodDescription actorInterfaceMethodDescription in actorInterfaceMethodDescriptions)
            {
                MethodInfo methodInfo = actorInterfaceMethodDescription.MethodInfo;
                var ami = new ActorMethodInfo
                {
                    MethodName = string.Concat(methodInfo.DeclaringType.Name, ".", methodInfo.Name),
                    MethodSignature = actorInterfaceMethodDescription.MethodInfo.ToString()
                };

                long key =
                    DiagnosticsEventManager.GetInterfaceMethodKey(
                        (uint) interfaceId,
                        (uint) actorInterfaceMethodDescription.Id);
                actorMethodInfos[key] = ami;
            }
        }

        internal virtual ActorMethodInfo GetActorMethodInfo(long key, RemotingListener remotingListener)
        {
            ActorMethodInfo methodInfo = this.actorMethodInfo[key];
            return methodInfo;
        }

        private void OnActorChangeRole(ChangeRoleDiagnosticData changeRoleData)
        {
            if (ReplicaRole.Primary == changeRoleData.NewRole)
            {
                this.writer.ReplicaChangeRoleToPrimary(this.serviceContext);
            }
            else if (ReplicaRole.Primary == changeRoleData.CurrentRole)
            {
                this.writer.ReplicaChangeRoleFromPrimary(this.serviceContext);
            }
        }

        private void OnActorActivated(ActivationDiagnosticData activationData)
        {
            ActorId actorId = activationData.ActorId;
            this.writer.ActorActivated(
                this.actorType,
                actorId,
                this.serviceContext);
        }

        private void OnActorDeactivated(ActivationDiagnosticData activationData)
        {
            ActorId actorId = activationData.ActorId;
            this.writer.ActorDeactivated(
                this.actorType,
                actorId,
                this.serviceContext);
        }

        private void OnActorMethodStart(ActorMethodDiagnosticData methodData)
        {
            if (this.writer.IsActorMethodStartEventEnabled())
            {
                ActorId actorId = methodData.ActorId;
                ActorMethodInfo methodInfo = this.GetActorMethodInfo(methodData.InterfaceMethodKey, methodData.RemotingListener);
                this.writer.ActorMethodStart(
                    methodInfo.MethodName,
                    methodInfo.MethodSignature,
                    this.actorType,
                    actorId,
                    this.serviceContext);
            }
        }

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        private void OnActorMethodFinish(ActorMethodDiagnosticData methodData)
        {
            if (null == methodData.Exception)
            {
                if (this.writer.IsActorMethodStopEventEnabled())
                {
                    ActorId actorId = methodData.ActorId;
                    ActorMethodInfo methodInfo = this.GetActorMethodInfo(methodData.InterfaceMethodKey, methodData.RemotingListener);
                    this.writer.ActorMethodStop(
                        methodData.MethodExecutionTime.Value.Ticks,
                        methodInfo.MethodName,
                        methodInfo.MethodSignature,
                        this.actorType,
                        actorId,
                        this.serviceContext);
                }
            }
            else
            {
                ActorId actorId = methodData.ActorId;
                ActorMethodInfo methodInfo = this.GetActorMethodInfo(methodData.InterfaceMethodKey, methodData.RemotingListener);
                this.writer.ActorMethodThrewException(
                    methodData.Exception.ToString(),
                    methodData.MethodExecutionTime.Value.Ticks,
                    methodInfo.MethodName,
                    methodInfo.MethodSignature,
                    this.actorType,
                    actorId,
                    this.serviceContext);
            }
        }

        private void OnPendingActorMethodCallsUpdated(PendingActorMethodDiagnosticData pendingMethodData)
        {
            if (this.writer.IsPendingMethodCallsEventEnabled())
            {
                ActorId actorId = pendingMethodData.ActorId;
                this.writer.ActorMethodCallsWaitingForLock(
                    pendingMethodData.PendingActorMethodCalls,
                    this.actorType,
                    actorId,
                    this.serviceContext);
            }
        }

        private void OnSaveActorStateStart(ActorStateDiagnosticData stateData)
        {
            if (this.writer.IsActorSaveStateStartEventEnabled())
            {
                ActorId actorId = stateData.ActorId;
                this.writer.ActorSaveStateStart(
                    this.actorType,
                    actorId,
                    this.serviceContext);
            }
        }

        private void OnSaveActorStateFinish(ActorStateDiagnosticData stateData)
        {
            if (this.writer.IsActorSaveStateStopEventEnabled())
            {
                ActorId actorId = stateData.ActorId;
                this.writer.ActorSaveStateStop(
                    // ReSharper disable once PossibleInvalidOperationException
                    stateData.OperationTime.Value.Ticks,
                    this.actorType,
                    actorId,
                    this.serviceContext);
            }
        }

        internal class ActorMethodInfo
        {
            internal string MethodName;
            internal string MethodSignature;
        }
    }
}