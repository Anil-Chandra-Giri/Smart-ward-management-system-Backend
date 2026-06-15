// Filters/LoggingFilter.cs
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

        public LoggingFilter(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // ✅ FIX Bug 9: Create a NEW Stopwatch per request, not a shared instance field.
            // A field-level Stopwatch shared across concurrent requests gives wrong timings
            // and is not thread-safe. Creating it locally here is always safe.
            var stopwatch = Stopwatch.StartNew();

            var controller = context.Controller.GetType().Name;
            var action = context.ActionDescriptor.RouteValues["action"];

            await _loggingService.LogInfoAsync(
                $"Started: {controller}.{action}",
                LogCategory.System,
                new { Parameters = context.ActionArguments }
            );

            // ✅ FIX Bug 10: Capture whether an exception was thrown by checking
            // resultContext.Exception rather than reading the response status code.
            // The response status code is unreliable at this point in the pipeline
            // because the response body may not have been written yet for async actions.
            var resultContext = await next();

            stopwatch.Stop();

            if (resultContext.Exception != null && !resultContext.ExceptionHandled)
            {
                // An unhandled exception occurred — log as error
                await _loggingService.LogErrorAsync(
                    $"Failed: {controller}.{action} — unhandled exception",
                    resultContext.Exception,
                    LogCategory.System,
                    new { DurationMs = stopwatch.ElapsedMilliseconds }
                );
            }
            else
            {
                // Read status code only as supplemental info, not to decide log level
                var statusCode = resultContext.HttpContext.Response.StatusCode;
                var isClientError = statusCode >= 400 && statusCode < 500;
                var isServerError = statusCode >= 500;

                if (isServerError)
                {
                    await _loggingService.LogErrorAsync(
                        $"Failed: {controller}.{action} — Status: {statusCode}",
                        null,
                        LogCategory.System,
                        new { DurationMs = stopwatch.ElapsedMilliseconds, StatusCode = statusCode }
                    );
                }
                else if (isClientError)
                {
                    await _loggingService.LogWarningAsync(
                        $"Client error: {controller}.{action} — Status: {statusCode}",
                        LogCategory.System,
                        new { DurationMs = stopwatch.ElapsedMilliseconds, StatusCode = statusCode }
                    );
                }
                else
                {
                    await _loggingService.LogInfoAsync(
                        $"Completed: {controller}.{action} — Status: {statusCode}",
                        LogCategory.System,
                        new { DurationMs = stopwatch.ElapsedMilliseconds, StatusCode = statusCode }
                    );
                }
            }
        }
    }
}