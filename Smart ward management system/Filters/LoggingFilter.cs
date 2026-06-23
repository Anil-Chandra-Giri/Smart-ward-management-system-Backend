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
            var stopwatch = Stopwatch.StartNew();
            var controller = context.Controller.GetType().Name;
            var action = context.ActionDescriptor.RouteValues["action"];
            var httpMethod = context.HttpContext.Request.Method;

            // NOTE: no "Started:" log here anymore — it doubled the row count
            // for zero audit value. We only log once, after we know the outcome.

            var resultContext = await next();
            stopwatch.Stop();

            if (resultContext.Exception != null && !resultContext.ExceptionHandled)
            {
                await _loggingService.LogErrorAsync(
                    $"{controller}.{action} threw an unhandled exception",
                    resultContext.Exception,
                    LogCategory.System,
                    new { DurationMs = stopwatch.ElapsedMilliseconds, HttpMethod = httpMethod }
                );
                return;
            }

            var statusCode = resultContext.HttpContext.Response.StatusCode;

            if (statusCode >= 500)
            {
                await _loggingService.LogErrorAsync(
                    $"{controller}.{action} failed with status {statusCode}",
                    null,
                    LogCategory.System,
                    new { DurationMs = stopwatch.ElapsedMilliseconds, StatusCode = statusCode, HttpMethod = httpMethod }
                );
            }
            else if (statusCode >= 400)
            {
                await _loggingService.LogWarningAsync(
                    $"{controller}.{action} rejected the request with status {statusCode}",
                    LogCategory.System,
                    new { DurationMs = stopwatch.ElapsedMilliseconds, StatusCode = statusCode, HttpMethod = httpMethod }
                );
            }

            // Successful requests are intentionally NOT logged here anymore.
            // - Reads (GET) never need an audit entry.
            // - Mutating actions (POST/PUT/PATCH/DELETE) should log their own
            //   clear, specific message at the point where the actual entity
            //   and actor are known — see StaffService for the pattern.
            //   A generic "Completed: ControllerX.MethodY — Status: 200" told
            //   you nothing about *what* happened, only that something did.
        }
    }
}