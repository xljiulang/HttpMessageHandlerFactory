# HttpMessageHandlerFactory
具有生命周期管理和动态Web代理的HttpMessageHandler创建工厂

![NuGet logo](https://buildstats.info/nuget/HttpMessageHandlerFactory) 

## 1 功能介绍
### 1.1 CreateHandler
```c#
/// <summary>
/// 创建用于请求的HttpMessageHandler
/// </summary>
/// <param name="name">别名</param>
/// <param name="proxyUri">支持携带UserInfo的代理地址</param> 
/// <returns></returns>
HttpMessageHandler CreateHandler(string name, Uri? proxyUri);
```
### 1.2 CreateClient
将CreateHandler()产生的`HttpMessageHandler`包装为`HttpClient`，适用于客户端直接请求。

### 1.3 CreateInvoker
将CreateHandler()产生的`HttpMessageHandler`包装为`HttpMessageInvoker`，适用于反向代理中间件（比如YARP）的请求转发。

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
### 3.1 HttpMessageHandlerFactory.Connection [过于先进无法展示]
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
