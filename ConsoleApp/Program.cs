using HttpMessageHandlerFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddLogging(x => x.AddConsole());
            services.AddHttpMessageHandlerFactory("App")
                .AddHttpMessageHandler<AppHttpHandler>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(1d));

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();

            var proxyUri = default(Uri);
            var httpClient = factory.CreateClient("App", proxyUri);
            var html = await httpClient.GetStringAsync("https://github.com/xljiulang/HttpMessageHandlerFactory/blob/master/README.md");
            Console.WriteLine(html);
        }
    }
}