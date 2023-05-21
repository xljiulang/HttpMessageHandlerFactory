using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using System;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// <see cref="IHttpMessageHandlerBuilder"/>的Polly扩展
    /// https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0#use-polly-based-handlers
    /// </summary>
    public static class PollyHttpMessageHandlerBuilderExtensions
    {
        /// <summary>
        /// 添加<see cref="PolicyHttpMessageHandler"/>到请求管道 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddPolicyHandler(
            this IHttpMessageHandlerBuilder builder,
            IAsyncPolicy<HttpResponseMessage> policy)
        {
            return builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policy));
        }

        /// <summary>
        /// 添加<see cref="PolicyHttpMessageHandler"/>到请求管道 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policySelector">策略选择器</param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddPolicyHandler(
            this IHttpMessageHandlerBuilder builder,
            Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            return builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policySelector));
        }

        /// <summary>
        /// 添加<see cref="PolicyHttpMessageHandler"/>到请求管道 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policySelector">策略选择器</param>
        /// <returns></returns> 
        public static IHttpMessageHandlerBuilder AddPolicyHandler(
            this IHttpMessageHandlerBuilder builder,
            Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            return builder.AddHttpMessageHandler((services) =>
            {
                return new PolicyHttpMessageHandler((request) => policySelector(services, request));
            });
        }

        /// <summary>
        /// 添加<see cref="PolicyHttpMessageHandler"/>到请求管道 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policyFactory">策略工厂</param>
        /// <param name="keySelector">策略选择器</param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddPolicyHandler(
            this IHttpMessageHandlerBuilder builder,
            Func<IServiceProvider, HttpRequestMessage, string, IAsyncPolicy<HttpResponseMessage>> policyFactory,
            Func<HttpRequestMessage, string> keySelector)
        {
            return builder.AddHttpMessageHandler((services) =>
            {
                var registry = services.GetRequiredService<IPolicyRegistry<string>>();
                return new PolicyHttpMessageHandler((request) =>
                {
                    var key = keySelector(request);

                    if (registry.TryGet<IAsyncPolicy<HttpResponseMessage>>(key, out var policy))
                    {
                        return policy;
                    }

                    var newPolicy = policyFactory(services, request, key);
                    registry[key] = newPolicy;
                    return newPolicy;
                });
            });
        }

        /// <summary>
        /// 添加<see cref="PolicyHttpMessageHandler"/>到请求管道 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policyKey">通过AddPolicyRegistry创建的policyRegistry记录的策略名称</param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddPolicyHandlerFromRegistry(
            this IHttpMessageHandlerBuilder builder,
            string policyKey)
        {
            return builder.AddHttpMessageHandler((services) =>
            {
                var registry = services.GetRequiredService<IReadOnlyPolicyRegistry<string>>();
                var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>(policyKey);
                return new PolicyHttpMessageHandler(policy);
            });
        }

        /// <summary>
        /// 添加<see cref="PolicyHttpMessageHandler"/>到请求管道 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policySelector">策略选择器</param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddPolicyHandlerFromRegistry(
            this IHttpMessageHandlerBuilder builder,
            Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            return builder.AddHttpMessageHandler((services) =>
            {
                var registry = services.GetRequiredService<IReadOnlyPolicyRegistry<string>>();
                return new PolicyHttpMessageHandler((request) => policySelector(registry, request));
            });
        }

        /// <summary>
        /// 添加<see cref="PolicyHttpMessageHandler"/>到请求管道
        /// 通过执行提供的配置委托创建。策略构建器将被预配置为触发策略应用程序，以处理带有指示暂时故障的条件的失败请求。
        /// </summary>
        /// <param name="builder"><see cref="IHttpMessageHandlerBuilder"/></param>
        /// <param name="configurePolicy">用于创建<see cref="IAsyncPolicy{HttpResponseMessage}"/>的委托</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// 配置策略时请参考<see cref="PolicyHttpMessageHandler"/>的备注
        /// </para>
        /// <para>
        /// <paramref name="configurePolicy"/>的<see cref="PolicyBuilder{HttpResponseMessage}"/>已经预配置了错误，可以处理以下几类错误:
        /// <list type="bullet">
        /// <item><description><see cref="HttpRequestException"/>的网络故障</description></item>
        /// <item><description>服务端响应5XX的状态码</description></item>
        /// <item><description>408的状态码(request timeout)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// 由<paramref name="configurePolicy"/>创建的策略将被无限期地缓存到每个指定的客户端。
        /// 策略通常被设计为单例，并且可以在适当的时候共享。
        /// 要跨多个指定客户端共享策略，首先创建策略，然后根据需要将其传递给对
        /// <see cref="AddPolicyHandler(IHttpMessageHandlerBuilder, IAsyncPolicy{HttpResponseMessage})"/>
        /// 的多个调用。
        /// </para>
        /// </remarks>
        public static IHttpMessageHandlerBuilder AddTransientHttpErrorPolicy(
            this IHttpMessageHandlerBuilder builder,
            Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy)
        {             
            var policyBuilder = HttpPolicyExtensions.HandleTransientHttpError();

            // Important - cache policy instances so that they are singletons per handler.
            var policy = configurePolicy(policyBuilder);

            builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policy));
            return builder;
        }
    }
}
