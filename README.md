# HttpMessageHandlerFactory
具有生命周期管理和动态Web代理的HttpMessageHandler创建工厂

| nuget包                              | 状态                                                                              | 说明     |
| ------------------------------------ | --------------------------------------------------------------------------------- | -------- |
| HttpMessageHandlerFactory            | ![NuGet logo](https://buildstats.info/nuget/HttpMessageHandlerFactory)            | MIT开源  |
| HttpMessageHandlerFactory.Connection | ![NuGet logo](https://buildstats.info/nuget/HttpMessageHandlerFactory.Connection) | 闭源，需要授权 |

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
### 3.1 HttpMessageHandlerFactory.Connection
为HttpMessageHandlerFactory提供自定义连接的功能。
注意此扩展项目不是免费项目，有如下限制：
* 不开放和提供源代码
* nuget包的程序集在应用程序运行2分钟后适用期结束
* 适用期结束后所有的http请求响应为423 Locked
* 需要license文件授权方可完全使用

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

## 4 开源有你
![赞助](donate2laojiu.png)