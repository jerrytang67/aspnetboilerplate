using System;
using Abp.Dependency;
using Microsoft.Extensions.Logging;
using Abp.Threading.Timers;

namespace Abp.Threading.BackgroundWorkers {
    /// <summary>
    /// Extends <see cref="BackgroundWorkerBase"/> to add a periodic running Timer. 
    /// </summary>
    public abstract class PeriodicBackgroundWorkerBase : BackgroundWorkerBase {
        protected readonly AbpTimer Timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeriodicBackgroundWorkerBase"/> class.
        /// </summary>
        /// <param name="timer">A timer.</param>
        protected PeriodicBackgroundWorkerBase(AbpTimer timer, IIocManager iocManager) : base(iocManager) {
            Timer = timer;
            Timer.Elapsed += Timer_Elapsed;
        }

        public override void Start() {
            base.Start();
            Timer.Start();
        }

        public override void Stop() {
            Timer.Stop();
            base.Stop();
        }

        public override void WaitToStop() {
            Timer.WaitToStop();
            base.WaitToStop();
        }

        /// <summary>
        /// Handles the Elapsed event of the Timer.
        /// </summary>
        private void Timer_Elapsed(object sender, EventArgs e) {
            try {
                DoWork();
            }
            catch (Exception ex) {
                Logger.LogWarning(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// Periodic works should be done by implementing this method.
        /// </summary>
        protected abstract void DoWork();
    }
}