﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Microsoft.ServiceFabric.Actors.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Common;

    internal class VolatileLogicalTimeManager
    {
        public interface ISnapshotHandler
        {
            Task OnSnapshotAsync(TimeSpan currentLogicalTime);
        }

        private const long DefaultLogicalTimeSnapshotIntervalInSeconds = 5;

        private TimeSpan lastSnapshot;
        private readonly Stopwatch stopwatch;
        private bool isRunning;
        private readonly RwLock rwLock;

        private readonly ISnapshotHandler handler;
        private readonly TimeSpan snapshotInterval;
        private readonly Timer timer;

        public VolatileLogicalTimeManager(ISnapshotHandler handler)
            : this(handler, TimeSpan.FromSeconds(DefaultLogicalTimeSnapshotIntervalInSeconds))
        {
        }

        public VolatileLogicalTimeManager(ISnapshotHandler handler, TimeSpan snapshotInterval)
        {
            this.lastSnapshot = TimeSpan.Zero;
            this.stopwatch = new Stopwatch();
            this.isRunning = false;
            this.rwLock = new RwLock();

            this.handler = handler;
            this.snapshotInterval = snapshotInterval;
            this.timer = new Timer(o => this.TimerCallback());

            this.stopwatch.Start();
        }

        public TimeSpan CurrentLogicalTime
        {
            get
            {
                using (this.rwLock.AcquireReadLock())
                {
                    return this.GetCurrentLogicalTime_CallerHoldsLock();
                }
            }

            set
            {
                using (this.rwLock.AcquireWriteLock())
                {
                    this.lastSnapshot = value;
                    this.stopwatch.Restart();
                }
            }
        }

        internal TimeSpan Test_GetCurrentSnapshot()
        {
            using (this.rwLock.AcquireReadLock())
            {
                return this.lastSnapshot;
            }
        }

        public void Start()
        {
            using (this.rwLock.AcquireWriteLock())
            {
                this.isRunning = true;

                this.ArmTimer_CallerHoldsLock();
            }
        }

        public void Stop()
        {
            using (this.rwLock.AcquireWriteLock())
            {
                this.isRunning = false;

                this.ArmTimer_CallerHoldsLock();
            }
        }

        private void ArmTimer()
        {
            using (this.rwLock.AcquireWriteLock())
            {
                this.ArmTimer_CallerHoldsLock();
            }
        }

        private void ArmTimer_CallerHoldsLock()
        {
            if (this.isRunning)
            {
                var elapsed = this.stopwatch.Elapsed;
                var delay = (elapsed > this.snapshotInterval) ? TimeSpan.Zero : (this.snapshotInterval - elapsed);
                this.timer.Change(delay, TimeSpan.FromMilliseconds(-1));
            }
            else
            {
                this.timer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
            }
        }

        private void TimerCallback()
        {
            this.handler.OnSnapshotAsync(
                this.SnapshotCurrentLogicalTime()).ContinueWith(t => this.ArmTimer());
        }

        private TimeSpan SnapshotCurrentLogicalTime()
        {
            using (this.rwLock.AcquireWriteLock())
            {
                var snapshot = this.GetCurrentLogicalTime_CallerHoldsLock();
                this.lastSnapshot = snapshot;
                this.stopwatch.Restart();

                return snapshot;
            }
        }

        private TimeSpan GetCurrentLogicalTime_CallerHoldsLock()
        {
            return this.lastSnapshot + this.stopwatch.Elapsed;
        }
    }
}