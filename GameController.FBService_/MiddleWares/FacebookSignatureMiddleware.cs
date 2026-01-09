
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace GameController.FBService.MiddleWares
{
    public class FacebookSignatureMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFacebookSignatureValidator _validator;

        public FacebookSignatureMiddleware(RequestDelegate next, IFacebookSignatureValidator validator)
        {
            _next = next;
            _validator = validator;
        }


        public async Task Invoke(HttpContext context)
        {
            // მხოლოდ FB Webhook POST-ზე ვამოწმებთ სიგნატურას
            if (context.Request.Method == HttpMethods.Post)
            {
                context.Request.EnableBuffering(); // საჭიროა Body-ს წაკითხვისთვის და reset-ზე

                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                var signatureHeader = context.Request.Headers["X-Hub-Signature-256"].ToString();

                if (!_validator.IsSignatureValid(body, signatureHeader))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid Facebook Signature");
                    return;
                }
            }

            await _next(context);
        }
    }
}
