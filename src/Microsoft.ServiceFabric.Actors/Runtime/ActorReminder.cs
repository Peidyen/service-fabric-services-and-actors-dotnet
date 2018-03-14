// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Runtime
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    internal class ActorReminder : IActorReminder
    {
        private const string TraceType = "ActorReminder";

        private readonly TimeSpan MinTimePeriod = Timeout.InfiniteTimeSpan;
        private readonly IActorManager actorManager;

        private Timer timer;

        public ActorReminder(ActorId actorId, IActorManager actorManager, IActorReminder reminder)
            : this(
                actorId,
                actorManager,
                reminder.Name,
                reminder.State,
                reminder.DueTime,
                reminder.Period)
        {
        }

        public ActorReminder(
            ActorId actorId,
            IActorManager actorManager,
            string reminderName,
            byte[] reminderState,
            TimeSpan reminderDueTime,
            TimeSpan reminderPeriod)
        {
            this.ValidateDueTime("DueTime", reminderDueTime);
            this.ValidatePeriod("Period", reminderPeriod);

            this.actorManager = actorManager;
            this.OwnerActorId = actorId;
            this.Name = reminderName;
            this.DueTime = reminderDueTime;
            this.Period = reminderPeriod;
            this.State = reminderState;

            this.timer = new Timer(this.OnReminderCallback);
        }

        internal ActorId OwnerActorId { get; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal bool IsValid()
        {
            return this.timer != null;
        }

        internal void CancelTimer()
        {
            if (this.timer != null)
            {
                this.timer.Dispose();
                this.timer = null;
            }
        }

        internal void ArmTimer(TimeSpan newDueTime)
        {
            var snap = this.timer;
            if (snap != null)
            {
                try
                {
                    snap.Change(newDueTime, Timeout.InfiniteTimeSpan);
                }
                catch (Exception e)
                {
                    this.actorManager.TraceSource.WriteErrorWithId(
                        TraceType,
                        this.actorManager.GetActorTraceId(this.OwnerActorId),
                        "Failed to arm timer for reminder {0} exception {1}",
                        this.Name,
                        e);
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            this.CancelTimer();
        }

        private void OnReminderCallback(object reminderState)
        {
            Task.Run(() => { this.actorManager.FireReminderAsync(this); });
        }

        private void ValidateDueTime(string argName, TimeSpan value)
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    argName,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        SR.TimerArgumentOutOfRange,
                        this.MinTimePeriod.TotalMilliseconds,
                        TimeSpan.MaxValue.TotalMilliseconds));
            }
        }

        private void ValidatePeriod(string argName, TimeSpan value)
        {
            if (value < this.MinTimePeriod)
            {
                throw new ArgumentOutOfRangeException(
                    argName,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        SR.TimerArgumentOutOfRange,
                        this.MinTimePeriod.TotalMilliseconds,
                        TimeSpan.MaxValue.TotalMilliseconds));
            }
        }

        ~ActorReminder()
        {
            this.Dispose(false);
        }

        #region IActorReminder Members

        public string Name { get; }

        public byte[] State { get; }

        public TimeSpan DueTime { get; }

        public TimeSpan Period { get; }

        #endregion
    }
}