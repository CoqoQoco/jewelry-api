using jewelry.Model.Exceptions;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Jewelry.Api.Extension
{
    internal class ErrorDetails
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("stacktrace")]
        public string StackTrace { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    internal class ValidationErrorDetails
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("stacktrace")]
        public string StackTrace { get; set; }
        [JsonProperty("errors")]
        public IDictionary<string, string[]> Errors { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            try
            {
                await _next(context);
            }
            catch (HandleException ex)
            {
                await HandleBadRequestExceptionAsync(context, ex);
            }
            catch (KeyNotFoundException ex)
            {
                await HandleNotFoundExceptionAsync(context, ex);
            }
            catch (ValidationException ex)
            {
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }


        private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            //context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.WriteAsJsonAsync(new ValidationErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                //Errors = exception
            });
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            //context.Response.Body.Seek(0, SeekOrigin.Begin);
            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new ErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                StackTrace = $"Error Message : {exception.Message} {Environment.NewLine}" +
                             $"StackTrace :{exception.StackTrace}"
            });

            await context.Response.WriteAsync(jsonResponse);

            //await context.Response.WriteAsync("Error Testing Write");

        }

        //private async Task HandleRestSharpExceptionAsync(HttpContext context, RestSharpInternalException exception)
        //{
        //    context.Response.ContentType = "application/json";
        //    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        //    //context.Response.Body.Seek(0, SeekOrigin.Begin);
        //    await context.Response.WriteAsJsonAsync(new ErrorDetails()
        //    {
        //        StatusCode = context.Response.StatusCode,
        //        StackTrace = $"Error Message : {exception.Message} {Environment.NewLine}" +
        //                     $"StackTrace :{exception.StackTrace}"
        //    });
        //}

        private async Task HandleBadRequestExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            //context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.WriteAsJsonAsync(new ErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = $"Error Message : {exception.Message}"
            });
        }

        private async Task HandleNotFoundExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            //context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.WriteAsJsonAsync(new ErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = $"Error Message : {exception.Message}"
            });
        }
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
