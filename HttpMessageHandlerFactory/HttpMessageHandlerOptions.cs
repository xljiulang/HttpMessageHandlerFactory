using System;
using System.Collections.Generic;
using System.Net.Http;

namespace HttpMessageHandlerFactory
{
    /// <summary>
    /// HttpMessageHandler选项
    /// </summary>
    public class HttpMessageHandlerOptions
    {
        /// <summary>
        /// 获取或设置生命周期
        /// 默认两分钟
        /// </summary>
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(2d);

        /// <summary>
        /// 获取属性记录字典
        /// </summary>
        public Dictionary<object, object> Properties { get; set; } = new();

        /// <summary>
        /// 获取额外的<see cref="DelegatingHandler"/>创建委托
        /// </summary>
        public List<Func<IServiceProvider, DelegatingHandler>> AdditionalHandlers { get; } = new();

        /// <summary>
        /// 获取基础<see cref="SocketsHttpHandler"/>的配置委托
        /// </summary>
        public List<Action<IServiceProvider, SocketsHttpHandler>> PrimaryHandlerConfigures { get; } = new();
    }
}
