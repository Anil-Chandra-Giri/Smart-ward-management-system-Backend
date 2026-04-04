using Microsoft.AspNetCore.Mvc.Filters;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Services;
using System.Diagnostics;
using LogLevel = Smart_ward_management_system.Model.LogLevel;

namespace Smart_ward_management_system.Filters
{
    public class LoggingFilter : IAsyncActionFilter
    {
        private readonly ILoggingService _loggingService;
        private readonly Stopwatch _stopwatch = new();

        public LoggingFilter(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _stopwatch.Start();

            // Log request start
            var controller = context.Controller.GetType().Name;
            var action = context.ActionDescriptor.RouteValues["action"];

            await _loggingService.LogInfoAsync(
                $"Started: {controller}.{action}",
                LogCategory.System,
                new { Parameters = context.ActionArguments }
            );

            var resultContext = await next();

            _stopwatch.Stop();

            // Log request completion
            var statusCode = resultContext.HttpContext.Response.StatusCode;
            var level = statusCode >= 400 ? LogLevel.Error : LogLevel.Information;

            if (level == LogLevel.Error)
            {
                await _loggingService.LogErrorAsync(
                    $"Failed: {controller}.{action} - Status: {statusCode}",
                    null,
                    LogCategory.System,
                    new { Duration = _stopwatch.ElapsedMilliseconds }
                );
            }
            else
            {
                await _loggingService.LogInfoAsync(
                    $"Completed: {controller}.{action} - Status: {statusCode}",
                    LogCategory.System,
                    new { Duration = _stopwatch.ElapsedMilliseconds }
                );
            }
        }
    }
}
