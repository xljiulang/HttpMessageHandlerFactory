using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using System;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// <see cref="IHttpMessageHandlerBuilder"/>��Polly��չ
    /// https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0#use-polly-based-handlers
    /// </summary>
    public static class PollyHttpMessageHandlerBuilderExtensions
    {
        /// <summary>
        /// ���<see cref="PolicyHttpMessageHandler"/>������ܵ� 
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
        /// ���<see cref="PolicyHttpMessageHandler"/>������ܵ� 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policySelector">����ѡ����</param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddPolicyHandler(
            this IHttpMessageHandlerBuilder builder,
            Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            return builder.AddHttpMessageHandler(() => new PolicyHttpMessageHandler(policySelector));
        }

        /// <summary>
        /// ���<see cref="PolicyHttpMessageHandler"/>������ܵ� 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policySelector">����ѡ����</param>
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
        /// ���<see cref="PolicyHttpMessageHandler"/>������ܵ� 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policyFactory">���Թ���</param>
        /// <param name="keySelector">����ѡ����</param>
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
        /// ���<see cref="PolicyHttpMessageHandler"/>������ܵ� 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policyKey">ͨ��AddPolicyRegistry������policyRegistry��¼�Ĳ�������</param>
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
        /// ���<see cref="PolicyHttpMessageHandler"/>������ܵ� 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="policySelector">����ѡ����</param>
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
        /// ���<see cref="PolicyHttpMessageHandler"/>������ܵ�
        /// ͨ��ִ���ṩ������ί�д��������Թ���������Ԥ����Ϊ��������Ӧ�ó����Դ������ָʾ��ʱ���ϵ�������ʧ������
        /// </summary>
        /// <param name="builder"><see cref="IHttpMessageHandlerBuilder"/></param>
        /// <param name="configurePolicy">���ڴ���<see cref="IAsyncPolicy{HttpResponseMessage}"/>��ί��</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// ���ò���ʱ��ο�<see cref="PolicyHttpMessageHandler"/>�ı�ע
        /// </para>
        /// <para>
        /// <paramref name="configurePolicy"/>��<see cref="PolicyBuilder{HttpResponseMessage}"/>�Ѿ�Ԥ�����˴��󣬿��Դ������¼������:
        /// <list type="bullet">
        /// <item><description><see cref="HttpRequestException"/>���������</description></item>
        /// <item><description>�������Ӧ5XX��״̬��</description></item>
        /// <item><description>408��״̬��(request timeout)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// ��<paramref name="configurePolicy"/>�����Ĳ��Խ��������ڵػ��浽ÿ��ָ���Ŀͻ��ˡ�
        /// ����ͨ�������Ϊ���������ҿ������ʵ���ʱ����
        /// Ҫ����ָ���ͻ��˹�����ԣ����ȴ������ԣ�Ȼ�������Ҫ���䴫�ݸ���
        /// <see cref="AddPolicyHandler(IHttpMessageHandlerBuilder, IAsyncPolicy{HttpResponseMessage})"/>
        /// �Ķ�����á�
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
