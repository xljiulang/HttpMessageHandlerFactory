# HttpMessageHandlerFactory
具有生命周期管理和动态Web代理的HttpMessageHandler创建工厂

## 1 nuget包
https://www.nuget.org/packages/HttpMessageHandlerFactory/

## 2 使用示例
```c#
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
```
