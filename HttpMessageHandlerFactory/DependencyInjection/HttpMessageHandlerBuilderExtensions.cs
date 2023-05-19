using HttpMessageHandlerFactory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// IHttpMessageHandlerBuilder扩展
    /// </summary>
    public static class HttpMessageHandlerBuilderExtensions
    {
        /// <summary> 
        /// 配置为反向代理模式以支持YARP等框架
        /// <para>.UseCookies = false</para>
        /// <para>.AllowAutoRedirect = false</para>
        /// <para>.ActivityHeadersPropagator = null</para>
        /// <para>.AutomaticDecompression = DecompressionMethods.None</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder ConfigureAsReverseProxy(this IHttpMessageHandlerBuilder builder)
        {
            return builder.ConfigurePrimaryHttpMessageHandler(handler =>
            {
                handler.UseCookies = false;
                handler.AllowAutoRedirect = false;
                handler.ActivityHeadersPropagator = null;
                handler.AutomaticDecompression = DecompressionMethods.None;
            });
        }

        /// <summary>
        /// 设置生命周期
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="handlerLifetime"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static IHttpMessageHandlerBuilder SetHandlerLifetime(this IHttpMessageHandlerBuilder builder, TimeSpan handlerLifetime)
        {
            if (handlerLifetime <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(handlerLifetime));
            }

            builder.AddOptions().Configure(options => options.Lifetime = handlerLifetime);
            return builder;
        }


        /// <summary>
        /// 添加管道HttpMessageHandler
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddHttpMessageHandler<THandler>(this IHttpMessageHandlerBuilder builder)
            where THandler : DelegatingHandler
        {
            builder.Services.TryAddTransient<THandler>();
            return builder.AddHttpMessageHandler(serviceProvider => serviceProvider.GetRequiredService<THandler>());
        }

        /// <summary>
        /// 添加管道HttpMessageHandler
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureHandler"></param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddHttpMessageHandler(this IHttpMessageHandlerBuilder builder, Func<DelegatingHandler> configureHandler)
        {
            return builder.AddHttpMessageHandler(serviceProvider => configureHandler());
        }

        /// <summary>
        /// 添加管道HttpMessageHandler
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureHandler"></param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddHttpMessageHandler(this IHttpMessageHandlerBuilder builder, Func<IServiceProvider, DelegatingHandler> configureHandler)
        {
            builder.AddOptions().Configure(options => options.AdditionalHandlers.Add(configureHandler));
            return builder;
        }

        /// <summary>
        /// 配置主要的HttpMessageHandler
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureHandler"></param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder ConfigurePrimaryHttpMessageHandler(this IHttpMessageHandlerBuilder builder, Action<SocketsHttpHandler> configureHandler)
        {
            return builder.ConfigurePrimaryHttpMessageHandler((serviceProvider, httpHandler) => configureHandler(httpHandler));
        }

        /// <summary>
        /// 配置主要的HttpMessageHandler
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureHandler"></param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder ConfigurePrimaryHttpMessageHandler(this IHttpMessageHandlerBuilder builder, Action<IServiceProvider, SocketsHttpHandler> configureHandler)
        {
            builder.AddOptions().Configure(options => options.PrimaryHandlerConfigures.Add(configureHandler));
            return builder;
        }


        /// <summary>
        /// 添加选项
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static OptionsBuilder<HttpMessageHandlerOptions> AddOptions(this IHttpMessageHandlerBuilder builder)
        {
            return builder.Services.AddOptions<HttpMessageHandlerOptions>(builder.Name);
        }
    }
}
