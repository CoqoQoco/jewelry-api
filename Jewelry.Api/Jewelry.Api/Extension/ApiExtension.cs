using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Extension
{
    public static class ApiExtension
    {
        public static IActionResult BadRequest(this IOptions<ApiBehaviorOptions> source, ControllerContext context)
        {
            return source.Value.InvalidModelStateResponseFactory(context);
        }
    }
}
