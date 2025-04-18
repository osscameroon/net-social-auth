using System.Net;

namespace SocialiteNET.UnitTests.Shared
{
    /// <summary>
    /// Mock implementation of HttpMessageHandler for testing HTTP clients
    /// </summary>
    public class HttpMessageHandlerMock : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpResponseMessage> responses = new();

        /// <summary>
        /// Setup a response for a specific HTTP method and URL
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="url">URL to match</param>
        /// <param name="content">Content to return</param>
        /// <param name="statusCode">HTTP status code (default: 200 OK)</param>
        public void SetupResponse(HttpMethod method, string url, string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            string key = $"{method}:{url}";

            HttpResponseMessage response = new(statusCode)
            {
                Content = new StringContent(content)
            };

            this.responses[key] = response;
        }

        /// <summary>
        /// Override of SendAsync to return mocked responses
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            string key = $"{request.Method}:{request.RequestUri}";

            if (this.responses.TryGetValue(key, out HttpResponseMessage? response))
            {
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                RequestMessage = request
            });
        }
    }
}