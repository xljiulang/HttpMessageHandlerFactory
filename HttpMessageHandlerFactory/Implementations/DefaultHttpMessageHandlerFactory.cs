using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;

namespace HttpMessageHandlerFactory.Implementations
{
    /// <summary>
    /// 默认的Http消息处理者工厂
    /// </summary>
    sealed class DefaultHttpMessageHandlerFactory : IHttpMessageHandlerFactory
    {
        private readonly NameRegistration nameRegistration;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ExpiredHandlerEntryCleaner expiredHandlerEntryCleaner;

        /// <summary>
        /// 过期回调
        /// </summary>
        private readonly TimerCallback expiryCallback;

        /// <summary>
        /// LazyOf(ActiveHandlerEntry)缓存
        /// </summary>
        private readonly ConcurrentDictionary<NameProxy, Lazy<ActiveHandlerEntry>> activeHandlerEntries = new();

        /// <summary>
        /// Http消息处理者工厂
        /// </summary>
        /// <param name="nameRegistration"></param>
        /// <param name="serviceScopeFactory"></param>
        /// <param name="expiredHandlerEntryCleaner"></param>
        public DefaultHttpMessageHandlerFactory(
            NameRegistration nameRegistration,
            IServiceScopeFactory serviceScopeFactory,
            ExpiredHandlerEntryCleaner expiredHandlerEntryCleaner)
        {
            this.nameRegistration = nameRegistration;
            this.serviceScopeFactory = serviceScopeFactory;
            this.expiredHandlerEntryCleaner = expiredHandlerEntryCleaner;

            this.expiryCallback = this.ExpiryTimer_Tick;
        }

        /// <summary>
        /// 创建用于请求的HttpMessageHandler
        /// </summary>
        /// <param name="name">别名</param>
        /// <param name="proxyUri">支持携带UserInfo的代理地址</param> 
        /// <returns></returns>
        public HttpMessageHandler CreateHandler(string name, Uri? proxyUri)
        {
            if (this.nameRegistration.Contains(name) == false)
            {
                throw new InvalidOperationException($"尚未登记别名为 {name} 的HttpMessageHandler");
            }

            var nameProxy = new NameProxy(name, proxyUri);
            var ativeEntry = this.activeHandlerEntries.GetOrAdd(nameProxy, this.CreateActiveHandlerEntryLazy).Value;
            ativeEntry.StartExpiryTimer(this.expiryCallback);
            return ativeEntry.LifetimeHttpHandler;
        }

        /// <summary>
        /// 创建LazyOf(ActiveHandlerEntry)
        /// </summary>
        /// <param name="nameProxy"></param>
        /// <returns></returns>
        private Lazy<ActiveHandlerEntry> CreateActiveHandlerEntryLazy(NameProxy nameProxy)
        {
            return new Lazy<ActiveHandlerEntry>(() => this.CreateActiveHandlerEntry(nameProxy), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// 创建ActiveHandlerEntry
        /// </summary>
        /// <param name="nameProxy"></param> 
        /// <returns></returns>
        private ActiveHandlerEntry CreateActiveHandlerEntry(NameProxy nameProxy)
        {
            var serviceScope = this.serviceScopeFactory.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;

            var builder = serviceProvider.GetRequiredService<HttpMessageHandlerBuilder>();
            builder.NameProxy = nameProxy;
            var httpHandler = builder.Build();
            var lifetime = builder.GetLifetime();

            var lifeTimeHandler = new LifetimeHttpHandler(httpHandler);
            return new ActiveHandlerEntry(lifetime, nameProxy, serviceScope, lifeTimeHandler);
        }


        /// <summary>
        /// 过期timer回调
        /// </summary>
        /// <param name="state"></param>
        private void ExpiryTimer_Tick(object? state)
        {
            var ativeEntry = (ActiveHandlerEntry)state!;

            // The timer callback should be the only one removing from the active collection. If we can't find
            // our entry in the collection, then this is a bug.
            var removed = this.activeHandlerEntries.TryRemove(ativeEntry.NameProxy, out Lazy<ActiveHandlerEntry>? found);
            Debug.Assert(removed, "Entry not found. We should always be able to remove the entry");
            Debug.Assert(object.ReferenceEquals(ativeEntry, found!.Value), "Different entry found. The entry should not have been replaced");

            // At this point the handler is no longer 'active' and will not be handed out to any new clients.
            // However we haven't dropped our strong reference to the handler, so we can't yet determine if
            // there are still any other outstanding references (we know there is at least one).

            // We use a different state object to track expired handlers. This allows any other thread that acquired
            // the 'active' entry to use it without safety problems.
            var expiredEntry = new ExpiredHandlerEntry(ativeEntry);
            this.expiredHandlerEntryCleaner.Add(expiredEntry);
        }
    }
}
