using Microsoft.AspNetCore.SignalR;

namespace GameController.Server.Hubs
{
    public class LogHubFilter : IHubFilter
    {
        private readonly ILogger<LogHubFilter> _logger;
        private readonly bool _isTrackerLogEnabled;

        public LogHubFilter(ILogger<LogHubFilter> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Get the value from appsettings.json
            _isTrackerLogEnabled = configuration.GetValue<bool>("TrackerLogEnabled");
        }

        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            if (_isTrackerLogEnabled)
            {
                var methodName = invocationContext.HubMethodName;
                var hubName = invocationContext.Hub.GetType().Name;
                _logger.LogDebug($"TrackerLog: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{hubName}.{methodName} started.");
            }

            try
            {
                var result = await next(invocationContext);

                if (_isTrackerLogEnabled)
                {
                    _logger.LogDebug($"TrackerLog: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{invocationContext.Hub.GetType().Name}.{invocationContext.HubMethodName} finished.");
                }

                return result;
            }
            catch (Exception ex)
            {
                if (_isTrackerLogEnabled)
                {
                    _logger.LogError(ex, $"TrackerLog: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{invocationContext.Hub.GetType().Name}.{invocationContext.HubMethodName} failed with exception.");
                }
                throw;
            }
        }
    }
}