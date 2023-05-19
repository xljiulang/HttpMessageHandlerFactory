using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class AppHttpHandler : DelegatingHandler
    {
        private readonly ILogger<AppHttpHandler> logger;

        public AppHttpHandler()
            : this(NullLogger<AppHttpHandler>.Instance)
        {
        }

        public AppHttpHandler(ILogger<AppHttpHandler> logger)
        {
            this.logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"App开始请求{request.RequestUri}");
            var response = await base.SendAsync(request, cancellationToken);

            this.logger.LogInformation($"App请求{request.RequestUri}完成");
            return response;
        }
    }
}
