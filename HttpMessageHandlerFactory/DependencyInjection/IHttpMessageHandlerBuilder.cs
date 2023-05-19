namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Http消息处理者创建者
    /// </summary>
    public interface IHttpMessageHandlerBuilder
    {
        /// <summary>
        /// 选项名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 服务集合
        /// </summary>
        IServiceCollection Services { get; }
    }
}
