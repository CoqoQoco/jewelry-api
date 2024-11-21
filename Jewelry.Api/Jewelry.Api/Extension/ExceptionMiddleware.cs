using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Security;

namespace Base.API.Extensions
{
    // 1. Error Response Models
    internal class ErrorDetails
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("stackTrace")]
        public string StackTrace { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }

    internal class ValidationErrorDetails : ErrorDetails
    {
        [JsonProperty("errors")]
        public IDictionary<string, string[]> Errors { get; set; }
    }

    // 2. Custom Business Exceptions
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }

    public class DbEntityValidationException : Exception
    {
        public DbEntityValidationException(string message) : base(message) { }
    }

    // 3. Main Exception Middleware
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Dictionary<Type, HttpStatusCode> _exceptionMap;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;

            _exceptionMap = new Dictionary<Type, HttpStatusCode>
        {
            { typeof(ArgumentException), HttpStatusCode.BadRequest },
            { typeof(ArgumentNullException), HttpStatusCode.BadRequest },
            { typeof(ValidationException), HttpStatusCode.BadRequest },
            { typeof(KeyNotFoundException), HttpStatusCode.NotFound },
            { typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized },
            { typeof(SecurityException), HttpStatusCode.Forbidden },
            { typeof(DbUpdateException), HttpStatusCode.BadRequest },
            { typeof(DbUpdateConcurrencyException), HttpStatusCode.Conflict },
            { typeof(BusinessRuleException), HttpStatusCode.UnprocessableEntity },
            { typeof(DomainException), HttpStatusCode.UnprocessableEntity }
        };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred: {TraceIdentifier}", context.TraceIdentifier);
                var statusCode = GetExceptionStatusCode(ex);
                await HandleExceptionAsync(context, ex, statusCode);
            }
        }

        private HttpStatusCode GetExceptionStatusCode(Exception exception)
        {
            return _exceptionMap.TryGetValue(exception.GetType(), out var statusCode)
                ? statusCode
                : HttpStatusCode.InternalServerError;
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = exception switch
            {
                ValidationException validationEx => new ValidationErrorDetails
                {
                    StatusCode = (int)statusCode,
                    Message = validationEx.Message,
                    //Errors = validationEx.Errors
                },
                _ => new ErrorDetails
                {
                    StatusCode = (int)statusCode,
                    Message = GetSafeErrorMessage(exception),
                    StackTrace = exception.StackTrace
                }
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        private string GetSafeErrorMessage(Exception ex) => ex.Message;
    }

    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}