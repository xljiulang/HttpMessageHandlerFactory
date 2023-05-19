using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;

namespace HttpMessageHandlerFactory.Implementations
{
    /// <summary>
    /// HttpMessageHandler创建器
    /// </summary>
    sealed class HttpMessageHandlerBuilder
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IOptionsMonitor<HttpMessageHandlerOptions> options;

        /// <summary>
        /// 获取或设置别名和代理
        /// </summary>
        [NotNull]
        public NameProxy? NameProxy { get; set; }

        /// <summary>
        /// 获取生命周期
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetLifetime()
        {
            return this.options.Get(this.NameProxy.Name).Lifetime;
        }

        /// <summary>
        /// HttpMessageHandler创建器
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="options"></param>
        public HttpMessageHandlerBuilder(
            IServiceProvider serviceProvider,
            IOptionsMonitor<HttpMessageHandlerOptions> options)
        {
            this.serviceProvider = serviceProvider;
            this.options = options;
        }

        /// <summary>
        /// 创建链式调用的<see cref="HttpMessageHandler"/>
        /// </summary>
        /// <returns></returns>
        public HttpMessageHandler Build()
        {
            var next = this.BuildPrimary();
            var additionalHandlers = this.options.Get(this.NameProxy.Name).AdditionalHandlers;

            for (var i = additionalHandlers.Count - 1; i >= 0; i--)
            {
                var handler = additionalHandlers[i](serviceProvider);
                handler.InnerHandler = next;
                next = handler;
            }

            return next;
        }

        /// <summary>
        /// 创建基础消息处理者
        /// </summary> 
        /// <returns></returns>
        private HttpMessageHandler BuildPrimary()
        {
            var primaryHandler = new SocketsHttpHandler
            {
                UseCookies = false
            };

            var proxyUri = this.NameProxy.ProxyUri;
            if (proxyUri == null)
            {
                primaryHandler.UseProxy = false;
            }
            else
            {
                primaryHandler.UseProxy = true;
                primaryHandler.Proxy = new WebProxy(proxyUri) { Credentials = GetCredential(proxyUri) };
            }

            var configures = this.options.Get(this.NameProxy.Name).PrimaryHandlerConfigures;
            foreach (var configure in configures)
            {
                configure(serviceProvider, primaryHandler);
            }

            return primaryHandler;
        }

        /// <summary>
        /// 获取身份
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static NetworkCredential? GetCredential(Uri uri)
        {
            var userInfo = uri.UserInfo;
            if (string.IsNullOrEmpty(userInfo))
            {
                return null;
            }

            var index = userInfo.IndexOf(':');
            if (index < 0)
            {
                return new NetworkCredential(userInfo, default(string));
            }

            var username = userInfo[..index];
            var password = userInfo[(index + 1)..];
            return new NetworkCredential(username, password);
        }
    }
}
