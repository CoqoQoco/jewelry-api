using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Extension
{
    public class ApiControllerBase : ControllerBase
    {
        private readonly IOptions<ApiBehaviorOptions> _apiBehaviorOptions;

        public ApiControllerBase(IOptions<ApiBehaviorOptions> apiBehaviorOptions)
        {
            _apiBehaviorOptions = apiBehaviorOptions;
        }

        protected IActionResult ModelStateBadRequest()
        {
            return _apiBehaviorOptions.BadRequest(ControllerContext);
        }
    }
}
