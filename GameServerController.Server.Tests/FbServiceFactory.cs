using GameController.FBService.Heplers;
using GameController.FBService.MiddleWares;
using GameController.FBService.Tests.Fakes;
using GameServerController.Server.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace GameController.FBService.Tests;

public class FbServiceFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {

            // 1) Capturing handler როგორც Singleton, რომ ტესტიდან წავიკითხოთ
            //--            services.AddSingleton<CapturingHttpMessageHandler>();
            //--
            //--            // 2) HttpClient, რომელიც ამ handler-ს იყენებს
            //--            services.AddSingleton(sp =>
            //--            {
            //--                var handler = sp.GetRequiredService<CapturingHttpMessageHandler>();
            //--                return new HttpClient(handler)
            //--                {
            //--                    BaseAddress = new Uri("https://graph.facebook.com/")
            //--                };
            //--            });
            //--
            //--            // 3) IHttpClientFactory ჩანაცვლება
            //--            services.RemoveAll<IHttpClientFactory>();
            //--            services.AddSingleton<IHttpClientFactory>(sp =>
            //--            {
            //--                var httpClient = sp.GetRequiredService<HttpClient>();
            //--                return new FakeHttpClientFactory(httpClient);
            //--            });
            //--            // აქ DI-ში ჩავანაცვლებთ რეალურ “გარე” დამოკიდებულებებს Fake-ებით.
            // TODO: ქვემოთ ზუსტად შენს ინტერფეისებს მოვარგებთ.

            services.AddSingleton<CapturingDelegatingHandler>();
            services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            services.AddSingleton<IHttpMessageHandlerBuilderFilter, CapturingHttpClientFilter>();



            services.RemoveAll<IFacebookSignatureValidator>();
            services.AddSingleton<IFacebookSignatureValidator, AllowAllSignatureValidator>();
            services.RemoveAll<IGlobalVarsKeeper>();
            services.AddSingleton<IGlobalVarsKeeper, InMemoryGlobalVarsKeeper>();

            // ასევე: signature validator, redis/global vars, rate limiter და ა.შ.
        });
    }
}
