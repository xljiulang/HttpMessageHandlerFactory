using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace HttpMessageHandlerFactory.Implementations
{
    /// <summary>
    /// 已过期的条目清除器
    /// https://github.com/dotnet/runtime/blob/v7.0.0/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs
    /// </summary>
    sealed partial class ExpiredHandlerEntryCleaner
    {
        private static readonly TimeSpan cleanupInterval = TimeSpan.FromSeconds(10d);
        private static readonly TimerCallback cleanupCallback = s => ((ExpiredHandlerEntryCleaner)s!).CleanupTimer_Tick();

        private Timer? cleanupTimer;
        private readonly object cleanupTimerLock = new();
        private readonly object cleanupActiveLock = new();
        private readonly ConcurrentQueue<ExpiredHandlerEntry> expiredHandlerEntries = new();
        private readonly ILogger<ExpiredHandlerEntryCleaner> logger;

        /// <summary>
        /// 已过期的条目清除器
        /// </summary>
        public ExpiredHandlerEntryCleaner()
            : this(NullLogger<ExpiredHandlerEntryCleaner>.Instance)
        {
        }

        /// <summary>
        /// 已过期的条目清除器
        /// </summary>
        /// <param name="logger"></param>
        public ExpiredHandlerEntryCleaner(ILogger<ExpiredHandlerEntryCleaner> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 添加过期条目
        /// </summary>
        /// <param name="expiredEntry"></param>
        public void Add(ExpiredHandlerEntry expiredEntry)
        {
            Log.HandlerExpired(this.logger, expiredEntry.NameProxy.Name);

            this.expiredHandlerEntries.Enqueue(expiredEntry);
            this.StartCleanupTimer();
        }

        /// <summary>
        /// 启动清洁
        /// </summary>
        private void StartCleanupTimer()
        {
            lock (this.cleanupTimerLock)
            {
                this.cleanupTimer ??= NonCapturingTimer.Create(cleanupCallback, this, cleanupInterval, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// 停止清洁
        /// </summary>
        private void StopCleanupTimer()
        {
            lock (this.cleanupTimerLock)
            {
                this.cleanupTimer!.Dispose();
                this.cleanupTimer = null;
            }
        }


        private void CleanupTimer_Tick()
        {
            // Stop any pending timers, we'll restart the timer if there's anything left to process after cleanup.
            //
            // With the scheme we're using it's possible we could end up with some redundant cleanup operations.
            // This is expected and fine.
            //
            // An alternative would be to take a lock during the whole cleanup process. This isn't ideal because it
            // would result in threads executing ExpiryTimer_Tick as they would need to block on cleanup to figure out
            // whether we need to start the timer.
            this.StopCleanupTimer();

            if (!Monitor.TryEnter(this.cleanupActiveLock))
            {
                // We don't want to run a concurrent cleanup cycle. This can happen if the cleanup cycle takes
                // a long time for some reason. Since we're running user code inside Dispose, it's definitely
                // possible.
                //
                // If we end up in that position, just make sure the timer gets started again. It should be cheap
                // to run a 'no-op' cleanup.
                this.StartCleanupTimer();
                return;
            }

            try
            {

                var initialCount = expiredHandlerEntries.Count;
                Log.CleanupCycleStart(this.logger, initialCount);

                var disposedCount = 0;
                for (var i = 0; i < initialCount; i++)
                {
                    // Since we're the only one removing from _expired, TryDequeue must always succeed.
                    this.expiredHandlerEntries.TryDequeue(out ExpiredHandlerEntry? entry);
                    Debug.Assert(entry != null, "Entry was null, we should always get an entry back from TryDequeue");

                    if (entry.CanDispose)
                    {
                        try
                        {
                            entry.InnerHandler.Dispose();
                            entry.ServiceScope.Dispose();
                            disposedCount++;
                        }
                        catch (Exception ex)
                        {
                            Log.CleanupItemFailed(this.logger, entry.NameProxy.Name, ex);
                        }
                    }
                    else
                    {
                        // If the entry is still live, put it back in the queue so we can process it
                        // during the next cleanup cycle.
                        this.expiredHandlerEntries.Enqueue(entry);
                    }
                }
                Log.CleanupCycleEnd(this.logger, disposedCount, this.expiredHandlerEntries.Count);
            }
            finally
            {
                Monitor.Exit(this.cleanupActiveLock);
            }

            // We didn't totally empty the cleanup queue, try again later.
            if (!expiredHandlerEntries.IsEmpty)
            {
                this.StartCleanupTimer();
            }
        }

        static partial class Log
        {
            [LoggerMessage(0, LogLevel.Debug, "Starting HttpMessageHandler cleanup cycle with {initialCount} items")]
            public static partial void CleanupCycleStart(ILogger logger, int initialCount);

            [LoggerMessage(1, LogLevel.Debug, "Ending HttpMessageHandler cleanup, processed {disposedCount} items, remaining {remaining} items")]
            public static partial void CleanupCycleEnd(ILogger logger, int disposedCount, int remaining);

            [LoggerMessage(2, LogLevel.Debug, "HttpMessageHandler.Dispose() threw an unhandled exception for '{name}'")]
            public static partial void CleanupItemFailed(ILogger logger, string name, Exception exception);

            [LoggerMessage(3, LogLevel.Debug, "HttpMessageHandler expired for '{name}'")]
            public static partial void HandlerExpired(ILogger logger, string name);
        }
    }
}
