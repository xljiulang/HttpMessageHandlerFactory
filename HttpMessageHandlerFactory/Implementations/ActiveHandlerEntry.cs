using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading;

namespace HttpMessageHandlerFactory.Implementations
{
    /// <summary>
    /// 活跃的条目
    /// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/Microsoft.Extensions.Http/src/ActiveHandlerTrackingEntry.cs
    /// </summary>
    sealed class ActiveHandlerEntry
    {
        private static readonly TimerCallback timerCallback = (s) => ((ActiveHandlerEntry)s!).Timer_Tick();

        private readonly object root = new();
        private bool timerInitialized = false;

        private Timer? timer;
        private TimerCallback? callback;

        public TimeSpan Lifetime { get; }

        public NameProxy NameProxy { get; }

        public IServiceScope ServiceScope { get; }

        public LifetimeHttpHandler LifetimeHttpHandler { get; }


        public ActiveHandlerEntry(
            TimeSpan lifetime,
            NameProxy nameProxy,
            IServiceScope serviceScope,
            LifetimeHttpHandler lifetimeHttpHandler)
        {
            this.Lifetime = lifetime;
            this.NameProxy = nameProxy;
            this.ServiceScope = serviceScope;
            this.LifetimeHttpHandler = lifetimeHttpHandler;
        }


        public void StartExpiryTimer(TimerCallback callback)
        {
            if (this.Lifetime == Timeout.InfiniteTimeSpan)
            {
                return;
            }

            if (Volatile.Read(ref this.timerInitialized))
            {
                return;
            }

            this.StartExpiryTimerSlow(callback);
        }

        private void StartExpiryTimerSlow(TimerCallback callback)
        {
            Debug.Assert(Lifetime != Timeout.InfiniteTimeSpan);

            lock (this.root)
            {
                if (Volatile.Read(ref this.timerInitialized))
                {
                    return;
                }

                this.callback = callback;
                this.timer = NonCapturingTimer.Create(timerCallback, this, Lifetime, Timeout.InfiniteTimeSpan);
                this.timerInitialized = true;
            }
        }

        private void Timer_Tick()
        {
            Debug.Assert(this.callback != null);
            Debug.Assert(this.timer != null);

            lock (this.root)
            {
                if (this.timer != null)
                {
                    this.timer.Dispose();
                    this.timer = null;

                    this.callback(this);
                }
            }
        }
    }
}
