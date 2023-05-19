using System;
using System.Net;
using System.Net.Http;

namespace HttpMessageHandlerFactory
{
    /// <summary>
    /// HttpMessageHandlerFactory扩展
    /// </summary>
    public static class HttpMessageHandlerFactoryExtensions
    {
        /// <summary>
        /// 创建Http客户端
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="name">别名</param>
        /// <param name="proxyUri">支持携带UserInfo的代理地址</param>
        /// <param name="cookieContainer">cookie容器</param>
        /// <returns></returns>
        public static HttpClient CreateClient(this IHttpMessageHandlerFactory factory, string name, Uri? proxyUri = null, CookieContainer? cookieContainer = null)
        {
            var httpHandler = factory.CreateHandler(name, proxyUri, cookieContainer);
            return new HttpClient(httpHandler, disposeHandler: false);
        }

        /// <summary>
        /// 创建Http执行器
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="name">别名</param>
        /// <param name="proxyUri">支持携带UserInfo的代理地址</param>
        /// <param name="cookieContainer">cookie容器</param>
        /// <returns></returns>
        public static HttpMessageInvoker CreateInvoker(this IHttpMessageHandlerFactory factory, string name, Uri? proxyUri = null, CookieContainer? cookieContainer = null)
        {
            var httpHandler = factory.CreateHandler(name, proxyUri, cookieContainer);
            return new HttpMessageInvoker(httpHandler, disposeHandler: false);
        }

        private static HttpMessageHandler CreateHandler(this IHttpMessageHandlerFactory factory, string name, Uri? proxyUri, CookieContainer? cookieContainer)
        {
            var httpHandler = factory.CreateHandler(name, proxyUri);
            if (cookieContainer != null)
            {
                httpHandler = new CookieHttpHandler(httpHandler, cookieContainer);
            }
            return httpHandler;
        }
    }
}
