// DetailedLogAttribute.cs
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace GameController.FBService.Extensions
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class DetailedLogAttribute : Attribute, IPageFilter, IAsyncPageFilter
	{
		private readonly HashSet<string> _handlersToLog;

		// კონსტრუქტორში ჩაამატე რომელი handler-ები გინდა დალოგოს
		public DetailedLogAttribute(params string[] handlerNames)
		{
			_handlersToLog = handlerNames?.Length > 0
				? new HashSet<string>(handlerNames, StringComparer.OrdinalIgnoreCase)
				: null; // null = ყველას ლოგავს (როგორც ადრე იყო)
		}

		public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
		{
			var handlerName = context.HandlerMethod?.Name;

			// თუ _handlersToLog == null, ლოგავს ყველას
			// თუ არა, ლოგავს მხოლოდ იმას რაც სიაშია
			if (_handlersToLog == null || (_handlersToLog.Count > 0 && _handlersToLog.Contains(handlerName)))
			{
				LogDetails(context.HttpContext,
						  context.ActionDescriptor.DisplayName,
						  handlerName,
						  context.HandlerArguments);
			}
		}

		public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
		public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
		public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

		public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
		{
			OnPageHandlerExecuting(context);
			await next();
		}

		private void LogDetails(HttpContext httpContext, string page, string handler, IDictionary<string, object> arguments)
		{
			var request = httpContext.Request;

			var logMessage = $@"
═══════════════════════════════════════════════════════════════
⏰ {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
🔗 Path: {request.Path}
📋 Method: {request.Method}
📄 Page/Action: {page}
⚡ Handler: {handler}
📦 Parameters: {string.Join(", ", arguments.Select(p => $"{p.Key}={p.Value ?? "null"}"))}
🌐 IP: {httpContext.Connection.RemoteIpAddress}
═══════════════════════════════════════════════════════════════";

			Console.WriteLine(logMessage);
		}
	}
}