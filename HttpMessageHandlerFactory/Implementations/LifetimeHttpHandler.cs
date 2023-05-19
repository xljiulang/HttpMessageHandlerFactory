using System.Net.Http;

namespace HttpMessageHandlerFactory.Implementations
{
    /// <summary>
    /// 表示自主管理生命周期的的HttpMessageHandler
    /// </summary>
    sealed class LifetimeHttpHandler : DelegatingHandler
    {
        /// <summary>
        /// 具有生命周期的HttpHandler
        /// </summary> 
        /// <param name="httpHandler"></param> 
        public LifetimeHttpHandler(HttpMessageHandler httpHandler)
        {
            this.InnerHandler = httpHandler;
        }

        /// <summary>
        /// 这里不释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
        }
    }
}
