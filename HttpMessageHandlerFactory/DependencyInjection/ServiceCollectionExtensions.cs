using HttpMessageHandlerFactory;
using HttpMessageHandlerFactory.Implementations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// ServiceCollection扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 创建别名的HttpMessageHandler的builder
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">别名</param>
        /// <returns></returns>
        public static IHttpMessageHandlerBuilder AddHttpMessageHandlerFactory(this IServiceCollection services, string name)
        {
            services.AddHttpMessageHandlerFactory();

            var descriptor = services.FirstOrDefault(item => item.ServiceType == typeof(NameRegistration));
            var registration = descriptor?.ImplementationInstance as NameRegistration;
            registration?.Add(name);

            return new DefaultProxyHttpClientBuilder(name, services);
        }

        /// <summary>
        /// 注册IHttpMessageHandlerFactory服务
        /// </summary>
        /// <param name="services"></param>  
        /// <returns></returns>
        public static IServiceCollection AddHttpMessageHandlerFactory(this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAddSingleton(new NameRegistration());
            services.TryAddTransient<HttpMessageHandlerBuilder>();
            services.TryAddSingleton<ExpiredHandlerEntryCleaner>();
            services.TryAddSingleton<IHttpMessageHandlerFactory, DefaultHttpMessageHandlerFactory>();
            return services;
        }


        private class DefaultProxyHttpClientBuilder : IHttpMessageHandlerBuilder
        {
            public string Name { get; }

            public IServiceCollection Services { get; }

            public DefaultProxyHttpClientBuilder(string name, IServiceCollection services)
            {
                this.Name = name;
                this.Services = services;
            }
        }
    }
}
