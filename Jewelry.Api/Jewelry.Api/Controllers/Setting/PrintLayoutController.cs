using jewelry.Model.Setting.PrintLayout;
using Jewelry.Api.Extension;
using Jewelry.Service.Setting.PrintLayout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers.Setting
{
    [Route("/[controller]")]
    [Authorize]
    [ApiController]
    public class PrintLayoutController : ApiControllerBase
    {
        private readonly IPrintLayoutService _service;

        public PrintLayoutController(IPrintLayoutService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            try
            {
                var result = await _service.GetAsync(key);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{key}")]
        public async Task<IActionResult> Save(string key, [FromBody] PrintLayoutDto dto)
        {
            try
            {
                await _service.SaveAsync(key, dto.LayoutJson);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
