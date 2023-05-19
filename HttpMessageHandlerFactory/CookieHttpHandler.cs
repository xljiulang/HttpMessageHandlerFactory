using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMessageHandlerFactory
{
    /// <summary>
    /// cookie处理者
    /// </summary>
    sealed class CookieHttpHandler : DelegatingHandler
    {
        private const string COOKIE_HEADER = "Cookie";
        private const string SET_COOKIE_HEADER = "Set-Cookie";
        private readonly CookieContainer cookieContainer;

        public CookieHttpHandler(HttpMessageHandler innerHandler, CookieContainer cookieContainer)
        {
            this.InnerHandler = innerHandler;
            this.cookieContainer = cookieContainer;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UseCookie(request, this.cookieContainer);
            var response = base.Send(request, cancellationToken);
            SetCookie(request.RequestUri, response, this.cookieContainer);
            return response;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UseCookie(request, this.cookieContainer);
            var response = await base.SendAsync(request, cancellationToken);
            SetCookie(request.RequestUri, response, this.cookieContainer);
            return response;
        }

        /// <summary>
        /// 使用Cookie到请求
        /// </summary>
        /// <param name="reqeust"></param>
        /// <param name="cookieContainer"></param>
        private static void UseCookie(
            HttpRequestMessage reqeust,
            CookieContainer cookieContainer)
        {
            var requestUri = reqeust.RequestUri;
            if (requestUri == null || requestUri.IsAbsoluteUri == false)
            {
                return;
            }

            var cookieHeader = cookieContainer.GetCookieHeader(requestUri);
            reqeust.Headers.TryAddWithoutValidation(COOKIE_HEADER, cookieHeader);
        }


        /// <summary>
        /// 设置Cookie到CookieContainer
        /// </summary>
        /// <param name="requestUri"></param>
        /// <param name="response"></param>
        /// <param name="cookieContainer"></param>
        private static void SetCookie(
            Uri? requestUri,
            HttpResponseMessage response,
            CookieContainer cookieContainer)
        {
            if (requestUri == null ||
                response.Headers.TryGetValues(SET_COOKIE_HEADER, out var cookies) == false)
            {
                return;
            }

            foreach (var cookieHeader in cookies)
            {
                try
                {
                    cookieContainer.SetCookies(requestUri, cookieHeader);
                }
                catch (CookieException)
                {
                }
            }
        }

    }
}

