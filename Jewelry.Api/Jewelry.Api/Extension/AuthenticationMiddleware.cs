using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Jewelry.Api.Extension
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (SecurityTokenValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = JsonConvert.SerializeObject(new
                {
                    status = 401,
                    message = "Invalid token"
                });

                await context.Response.WriteAsync(response);
            }
        }
    }
}
