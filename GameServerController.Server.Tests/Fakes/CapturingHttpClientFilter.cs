using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging; // Add this using directive if targeting .NET Core 3.1 or earlier
// For .NET 5+, IHttpMessageHandlerBuilderFilter is in Microsoft.Extensions.Http.Abstractions


namespace GameServerController.Server.Tests.Fakes
{
    public class CapturingHttpClientFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly CapturingDelegatingHandler _capturer;

        public CapturingHttpClientFilter(CapturingDelegatingHandler capturer)
        {
            _capturer = capturer;
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            return builder =>
            {
                next(builder);

                // ყველა HttpClient-ის pipeline-ში ვამატებთ capturing handler-ს
                builder.AdditionalHandlers.Add(_capturer);
            };
        }
    }
}
