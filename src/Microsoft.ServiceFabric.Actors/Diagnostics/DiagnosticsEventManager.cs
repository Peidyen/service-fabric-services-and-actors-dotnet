﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Threading;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting;

    internal class DiagnosticsEventManager
    {
        internal OnDiagnosticEvent<ChangeRoleDiagnosticData> OnActorChangeRole;
        internal OnDiagnosticEvent<ActivationDiagnosticData> OnActorActivated;
        internal OnDiagnosticEvent<ActivationDiagnosticData> OnActorDeactivated;
        internal OnDiagnosticEvent<PendingActorMethodDiagnosticData> OnPendingActorMethodCallsUpdated;
        internal OnDiagnosticEvent<ActorMethodDiagnosticData> OnActorMethodStart;
        internal OnDiagnosticEvent<ActorMethodDiagnosticData> OnActorMethodFinish;
        internal OnDiagnosticEvent<ActorStateDiagnosticData> OnSaveActorStateFinish;
        internal OnDiagnosticEvent<ActorStateDiagnosticData> OnSaveActorStateStart;
        internal OnDiagnosticEvent OnActorRequestProcessingStart;
        internal OnDiagnosticEvent<TimeSpan> OnActorRequestProcessingFinish;
        internal OnDiagnosticEvent<TimeSpan> OnActorLockAcquired;
        internal OnDiagnosticEvent<TimeSpan> OnActorLockReleased;
        internal OnDiagnosticEvent<TimeSpan> OnActorRequestDeserializationFinish;
        internal OnDiagnosticEvent<TimeSpan> OnActorResponseSerializationFinish;
        internal OnDiagnosticEvent<TimeSpan> OnActorOnActivateAsyncFinish;
        internal OnDiagnosticEvent<TimeSpan> OnLoadActorStateFinish;

        private ChangeRoleDiagnosticData changeRoleDiagnosticData;

        internal DiagnosticsEventManager(ActorMethodFriendlyNameBuilder methodFriendlyNameBuilder)
        {
            this.ActorMethodFriendlyNameBuilder = methodFriendlyNameBuilder;
        }

        internal ActorMethodFriendlyNameBuilder ActorMethodFriendlyNameBuilder { get; }

        public static long GetInterfaceMethodKey(uint interfaceId, uint methodId)
        {
            var key = (ulong) methodId;
            key = key | ((ulong) interfaceId << 32);
            return (long) key;
        }

        internal void ActorRequestProcessingStart()
        {
            OnDiagnosticEvent callbacks = this.OnActorRequestProcessingStart;
            if (null != callbacks)
            {
                callbacks();
            }
        }

        internal void ActorRequestProcessingFinish(DateTime startTime)
        {
            TimeSpan processingTime = DateTime.UtcNow - startTime;
            OnDiagnosticEvent<TimeSpan> callbacks = this.OnActorRequestProcessingFinish;
            if (null != callbacks)
            {
                callbacks(processingTime);
            }
        }

        internal void ActorRequestDeserializationFinish(DateTime startTime)
        {
            TimeSpan processingTime = DateTime.UtcNow - startTime;
            OnDiagnosticEvent<TimeSpan> callbacks = this.OnActorRequestDeserializationFinish;
            if (null != callbacks)
            {
                callbacks(processingTime);
            }
        }

        internal void ActorResponseSerializationFinish(DateTime startTime)
        {
            TimeSpan processingTime = DateTime.UtcNow - startTime;
            OnDiagnosticEvent<TimeSpan> callbacks = this.OnActorResponseSerializationFinish;
            if (null != callbacks)
            {
                callbacks(processingTime);
            }
        }

        internal void ActorOnActivateAsyncStart(ActorBase actor)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            diagCtx.OnActivateAsyncStopwatch.Restart();
        }

        internal void ActorOnActivateAsyncFinish(ActorBase actor)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            Stopwatch onActivateAsyncStopwatch = diagCtx.OnActivateAsyncStopwatch;
            onActivateAsyncStopwatch.Stop();

            OnDiagnosticEvent<TimeSpan> callbacks = this.OnActorOnActivateAsyncFinish;
            if (null != callbacks)
            {
                callbacks(onActivateAsyncStopwatch.Elapsed);
            }

            onActivateAsyncStopwatch.Reset();
        }

        internal void ActorMethodStart(long interfaceMethodKey, ActorBase actor, RemotingListener remotingListener)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            ActorMethodDiagnosticData mtdEvtArgs = diagCtx.MethodData;
            mtdEvtArgs.ActorId = actor.Id;
            mtdEvtArgs.InterfaceMethodKey = interfaceMethodKey;
            mtdEvtArgs.MethodExecutionTime = null;
            mtdEvtArgs.RemotingListener = remotingListener;
            Stopwatch methodStopwatch = diagCtx.GetOrCreateActorMethodStopwatch();
            methodStopwatch.Restart();

            OnDiagnosticEvent<ActorMethodDiagnosticData> callbacks = this.OnActorMethodStart;
            if (null != callbacks)
            {
                callbacks(mtdEvtArgs);
            }

            // Push the stopwatch to the stopwatch stack. Stack is needed for
            // handling reentrancy.
            diagCtx.PushActorMethodStopwatch(methodStopwatch);
        }

        internal void ActorMethodFinish(long interfaceMethodKey, ActorBase actor, Exception e, RemotingListener remotingListener)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            ActorMethodDiagnosticData mtdEvtArgs = diagCtx.MethodData;

            // Pop the stopwatch from the stopwatch stack.
            Stopwatch mtdStopwatch = diagCtx.PopActorMethodStopwatch();

            mtdStopwatch.Stop();
            mtdEvtArgs.ActorId = actor.Id;
            mtdEvtArgs.InterfaceMethodKey = interfaceMethodKey;
            mtdEvtArgs.MethodExecutionTime = mtdStopwatch.Elapsed;
            mtdEvtArgs.Exception = e;
            mtdEvtArgs.RemotingListener = remotingListener;
            mtdStopwatch.Reset();

            OnDiagnosticEvent<ActorMethodDiagnosticData> callbacks = this.OnActorMethodFinish;
            if (null != callbacks)
            {
                callbacks(mtdEvtArgs);
            }
        }

        internal void LoadActorStateStart(ActorBase actor)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            diagCtx.StateStopwatch.Restart();
        }

        internal void LoadActorStateFinish(ActorBase actor)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            Stopwatch stateStopwatch = diagCtx.StateStopwatch;
            stateStopwatch.Stop();

            OnDiagnosticEvent<TimeSpan> callbacks = this.OnLoadActorStateFinish;
            if (null != callbacks)
            {
                callbacks(stateStopwatch.Elapsed);
            }

            stateStopwatch.Reset();
        }

        internal void SaveActorStateStart(ActorBase actor)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            ActorStateDiagnosticData stateEvtArgs = diagCtx.StateData;
            stateEvtArgs.ActorId = actor.Id;
            stateEvtArgs.OperationTime = null;
            diagCtx.StateStopwatch.Restart();

            OnDiagnosticEvent<ActorStateDiagnosticData> callbacks = this.OnSaveActorStateStart;
            if (null != callbacks)
            {
                callbacks(stateEvtArgs);
            }
        }

        internal void SaveActorStateFinish(ActorBase actor)
        {
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            ActorStateDiagnosticData stateEvtArgs = diagCtx.StateData;
            Stopwatch stateStopwatch = diagCtx.StateStopwatch;
            stateStopwatch.Stop();
            stateEvtArgs.ActorId = actor.Id;
            stateEvtArgs.OperationTime = stateStopwatch.Elapsed;
            stateStopwatch.Reset();

            OnDiagnosticEvent<ActorStateDiagnosticData> callbacks = this.OnSaveActorStateFinish;
            if (null != callbacks)
            {
                callbacks(stateEvtArgs);
            }
        }

        internal DateTime AcquireActorLockStart(ActorBase actor)
        {
            // Use DateTime instead of StopWatch to measure elapsed time. We do this in order to avoid allocating a
            // StopWatch object for each operation that acquires the actor lock.
            DateTime startTime = DateTime.UtcNow;
            Interlocked.Increment(ref actor.DiagnosticsContext.PendingActorMethodCalls);
            return startTime;
        }

        internal void AcquireActorLockFailed(ActorBase actor)
        {
            Interlocked.Decrement(ref actor.DiagnosticsContext.PendingActorMethodCalls);
        }

        internal DateTime AcquireActorLockFinish(ActorBase actor, DateTime actorLockAcquireStartTime)
        {
            // Record the current time
            DateTime currentTime = DateTime.UtcNow;

            // Update number of pending actor method calls
            DiagnosticsManagerActorContext diagCtx = actor.DiagnosticsContext;
            long pendingActorMethodCalls = Interlocked.Decrement(ref diagCtx.PendingActorMethodCalls);
            long delta = pendingActorMethodCalls - diagCtx.LastReportedPendingActorMethodCalls;
            diagCtx.LastReportedPendingActorMethodCalls = pendingActorMethodCalls;

            PendingActorMethodDiagnosticData pendingMtdEvtArgs = diagCtx.PendingMethodDiagnosticData;
            pendingMtdEvtArgs.ActorId = actor.Id;
            pendingMtdEvtArgs.PendingActorMethodCalls = pendingActorMethodCalls;
            pendingMtdEvtArgs.PendingActorMethodCallsDelta = delta;

            OnDiagnosticEvent<PendingActorMethodDiagnosticData> callbacks1 = this.OnPendingActorMethodCallsUpdated;
            if (null != callbacks1)
            {
                callbacks1(pendingMtdEvtArgs);
            }

            // Update time taken to acquire actor lock
            TimeSpan lockAcquireTime = currentTime - actorLockAcquireStartTime;
            OnDiagnosticEvent<TimeSpan> callbacks2 = this.OnActorLockAcquired;
            if (null != callbacks2)
            {
                callbacks2(lockAcquireTime);
            }

            return currentTime;
        }

        internal void ReleaseActorLock(DateTime? actorLockHoldStartTime)
        {
            if (actorLockHoldStartTime.HasValue)
            {
                OnDiagnosticEvent<TimeSpan> callbacks = this.OnActorLockReleased;
                if (null != callbacks)
                {
                    TimeSpan lockHoldTime = DateTime.UtcNow - actorLockHoldStartTime.Value;
                    callbacks(lockHoldTime);
                }
            }
        }

        internal void ActorChangeRole(ReplicaRole currentRole, ReplicaRole newRole)
        {
            OnDiagnosticEvent<ChangeRoleDiagnosticData> callbacks = this.OnActorChangeRole;
            if (null != callbacks)
            {
                this.changeRoleDiagnosticData.CurrentRole = currentRole;
                this.changeRoleDiagnosticData.NewRole = newRole;
                callbacks(this.changeRoleDiagnosticData);
            }
        }

        internal void ActorActivated(ActorBase actor)
        {
            ActivationDiagnosticData activationEvtArgs = actor.DiagnosticsContext.ActivationDiagnosticData;
            activationEvtArgs.IsActivationEvent = true;
            activationEvtArgs.ActorId = actor.Id;

            OnDiagnosticEvent<ActivationDiagnosticData> callbacks = this.OnActorActivated;
            if (null != callbacks)
            {
                callbacks(activationEvtArgs);
            }
        }

        internal void ActorDeactivated(ActorBase actor)
        {
            ActivationDiagnosticData activationEvtArgs = actor.DiagnosticsContext.ActivationDiagnosticData;
            activationEvtArgs.IsActivationEvent = false;
            activationEvtArgs.ActorId = actor.Id;

            OnDiagnosticEvent<ActivationDiagnosticData> callbacks = this.OnActorDeactivated;
            if (null != callbacks)
            {
                callbacks(activationEvtArgs);
            }
        }

        internal delegate void OnDiagnosticEvent();

        internal delegate void OnDiagnosticEvent<T>(T eventData);
    }
}