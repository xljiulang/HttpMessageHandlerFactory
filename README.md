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

## 3 扩展项目
### 3.1 HttpMessageHandlerFactory.Connection[过于先进无法展示]
为HttpMessageHandlerFactory提供自定义连接的功能。

#### 3.1.1 自定义域名解析
* 当无代理连接时，连接到自定义解析得到的IP
* 当使用http代理时，让代理服务器连接到自定义解析得到的IP
* 当使用socks代理时，让代理服务器连接到自定义解析得到的IP

```c#
services
    .AddHttpMessageHandlerFactory("App")
    .AddHostResolver<CustomHostResolver>();
```

```c#
sealed class CustomHostResolver : HostResolver
{
    public override ValueTask<HostPort> ResolveAsync(DnsEndPoint endpoint, CancellationToken cancellationToken)
    {
        if (endpoint.Host == "www.baidu.com")
        {
            return ValueTask.FromResult(new HostPort("14.119.104.189", endpoint.Port));
        }
        return ValueTask.FromResult(new HostPort(endpoint.Host, endpoint.Port));
    }
}
```
#### 3.1.2 自定义ssl的sni
```c#
services
    .AddHttpMessageHandlerFactory("App")
    .AddSslSniProvider<CustomSslSniProvider>();
```

```c#
sealed class CustomSslSniProvider : SslSniProvider
{
    public override ValueTask<string> GetSslSniAsync(string host, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(string.Empty);
    }

    public override bool RemoteCertificateValidationCallback(string host, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        return true;
    }
}
```
