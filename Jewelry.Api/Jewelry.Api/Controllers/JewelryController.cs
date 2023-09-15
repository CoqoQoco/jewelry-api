using jewelry.Model.Exceptions;
using jewelry.Model.Helper.ResponseReadExcelProduct.cs;
using Jewelry.Api.Extension;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Jewelry.Service.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class JewelryController : ApiControllerBase
    {
        private readonly ILogger<JewelryController> _logger;
        private readonly IReadExcelProduct _IReadExcelProduct;
        private readonly IUserService _IUserService;
        public JewelryController(ILogger<JewelryController> logger,
            IReadExcelProduct IReadExcelProduct,
            IUserService IUserService,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _IReadExcelProduct = IReadExcelProduct;
            _IUserService = IUserService;
        }

        #region *** API ***
        #region *** Test User ***
        [Route("GetUsers")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<TbmAccount>))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult GetUsers()
        {
            try
            {
                var response = _IUserService.GetUsers();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        #endregion
        #region *** Product Excel ***
        [Route("ImportFileProduct")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ResponseReadExcelProduct))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ImportFileProduct([FromForm] RequestReadExcelProduct request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ModelStateBadRequest();
                }

                if (!_IReadExcelProduct.IsExcel(request.ExcelFile))
                {
                    throw new HandleException("Invalid file format. Only .xlsx files are allowed.");
                }

                var response = _IReadExcelProduct.Process(request.ExcelFile);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        } 
        #endregion
        #endregion
    }
}
