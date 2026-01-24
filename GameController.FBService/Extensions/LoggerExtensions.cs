using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace GameController.FBService.Extensions
{
	public static class LoggerExtensions
	{
		public static void LogInformationWithCaller(this ILogger logger, string message,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int lineNumber = 0)
		{
			var eventId = new EventId(lineNumber, memberName);
			logger.LogInformation(eventId, "{Timestamp} {Message}", DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff"), message);
		}

		// სხვა ლეველებისთვისაც შეგვიძლია overload-ები:
		public static void LogErrorWithCaller(this ILogger logger, string message, Exception? ex = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int lineNumber = 0)
		{
			var eventId = new EventId(lineNumber, memberName);
			logger.LogError(eventId, ex, "{Timestamp} {Message}", DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff"), message);
		}

		public static void LogWarningWithCaller(this ILogger logger, string message, Exception? ex = null,
			[CallerMemberName] string memberName = "",
			[CallerLineNumber] int lineNumber = 0)
		{
			//return;
			var eventId = new EventId(lineNumber, memberName);
			logger.LogWarning(eventId, ex, "{Timestamp} {Message}", DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff"), message);
		}
		// მსგავსი LogWarningWithCaller, LogDebugWithCaller...
	}
}