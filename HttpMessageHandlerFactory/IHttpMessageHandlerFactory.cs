using System;
using System.Net.Http;

namespace HttpMessageHandlerFactory
{
    /// <summary>
    /// Http消息处理者工厂
    /// </summary>
    public interface IHttpMessageHandlerFactory
    {
        /// <summary>
        /// 创建用于请求的HttpMessageHandler
        /// </summary>
        /// <param name="name">别名</param>
        /// <param name="proxyUri">支持携带UserInfo的代理地址</param> 
        /// <returns></returns>
        HttpMessageHandler CreateHandler(string name, Uri? proxyUri);
    }
}
