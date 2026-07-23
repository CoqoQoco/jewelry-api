using jewelry.Model.Customer;
using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanGet;
using jewelry.Model.ProductionPlan.ProductionPlanReport;
using jewelry.Model.Worker;
using jewelry.Model.Worker.Create;
using jewelry.Model.Worker.GoldLossSlip;
using jewelry.Model.Worker.GoldLossTangSlip;
using jewelry.Model.Worker.List;
using jewelry.Model.Worker.Report;
using jewelry.Model.Worker.TrackingWorker;
using jewelry.Model.Worker.Update;
using jewelry.Model.Worker.WorkerWages;
using Jewelry.Api.Extension;
using Jewelry.Service.Customer;
using Jewelry.Service.Worker;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkerController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IWorkerService _service;
        private readonly IWorkerGoldLossSlipService _goldLossSlipService;
        private readonly IGoldLossTangSlipService _goldLossTangSlipService;

        public WorkerController(ILogger<MoldController> logger,
            IWorkerService service,
            IWorkerGoldLossSlipService goldLossSlipService,
            IGoldLossTangSlipService goldLossTangSlipService,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
            _goldLossSlipService = goldLossSlipService;
            _goldLossTangSlipService = goldLossTangSlipService;
        }

        [Route("GetWorkerProductionType")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(MasterWorkerProductionTypeResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult GetWorkerProductionType()
        {
            try
            {
                var response = _service.GetWorkerProductionType();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Search")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<ListWorkerProductionResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult Search([FromBody] ListWorkerProductionRequest request)
        {
            try
            {
                var report = _service.Search(request.Search);
                return report.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("Create")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateProductionWorkerRequest request)
        {
            try
            {
                var response = await _service.Create(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("Update")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Update([FromBody] UpdateProductionWorkerRequest request)
        {
            try
            {
                var response = await _service.Update(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SearchWorkerWages")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(SearchWorkerWagesResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult SearchWorkerWages([FromBody] SearchWorkerWagesRequest request)
        {
            try
            {
                var response = _service.SearchWorkerWages(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SearchWorkerActiveStatus")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(SearchWorkerWagesResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult SearchWorkerActiveStatus([FromBody] SearchWorkerWagesRequest request)
        {
            try
            {
                var response = _service.SearchWorkerActiveStatus(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }


        [Route("ReportWorkerWages")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<ReportWorkerWagesRequest>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult Report([FromBody] ReportWorkerWagesRequest request)
        {
            try
            {
                var report = _service.Report(request.Search);
                return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("ReportWorkerWagesByWorker")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<ReportWorkerWagesByWorkerResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult ReportByWorker([FromBody] ReportWorkerWagesRequest request)
        {
            try
            {
                var report = _service.ReportByWorker(request.Search);
                return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("ReportWorkerSummeryReportWages")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(ReportWorkerSummeryResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult SummeryReport([FromBody] ReportWorkerWagesRequest request)
        {
            try
            {
                var report = _service.SummeryReport(request.Search);
                return Ok(report);

            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }


        [Route("TrackingWorkerRequest")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<TrackingWorkerResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult TrackingWorkerRequest([FromBody] TrackingWorkerRequest request)
        {
            try
            {
                var report = _service.TrackingWorker(request.Search);
                return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("CreateGoldLossSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(GoldLossSlipResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateGoldLossSlip([FromBody] CreateGoldLossSlipRequest request)
        {
            try
            {
                var response = await _goldLossSlipService.CreateSlip(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ListGoldLossSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(List<GoldLossSlipSummaryResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult ListGoldLossSlip([FromBody] ListGoldLossSlipRequest request)
        {
            try
            {
                var response = _goldLossSlipService.ListSlips(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetGoldLossSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(GoldLossSlipResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult GetGoldLossSlip([FromBody] GetGoldLossSlipRequest request)
        {
            try
            {
                var response = _goldLossSlipService.GetSlip(request.Id);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("CancelGoldLossSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CancelGoldLossSlip([FromBody] CancelGoldLossSlipRequest request)
        {
            try
            {
                await _goldLossSlipService.CancelSlip(request.Id);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SearchGoldLossTangJobs")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(List<SearchGoldLossTangJobsResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult SearchGoldLossTangJobs([FromBody] SearchGoldLossTangJobsRequest request)
        {
            try
            {
                var response = _goldLossTangSlipService.SearchJobs(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("CreateGoldLossTangSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(GoldLossTangSlipResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateGoldLossTangSlip([FromBody] CreateGoldLossTangSlipRequest request)
        {
            try
            {
                var response = await _goldLossTangSlipService.CreateSlip(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ListGoldLossTangSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(List<GoldLossTangSlipSummaryResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult ListGoldLossTangSlip([FromBody] ListGoldLossTangSlipRequest request)
        {
            try
            {
                var response = _goldLossTangSlipService.ListSlips(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ReportGoldLossTangByWorker")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<ReportGoldLossTangByWorkerResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult ReportGoldLossTangByWorker([FromBody] ReportGoldLossTangByWorkerRequest request)
        {
            try
            {
                var report = _goldLossTangSlipService.ReportByWorker(request.Search);
                return report.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("GetGoldLossTangSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(GoldLossTangSlipResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult GetGoldLossTangSlip([FromBody] GetGoldLossTangSlipRequest request)
        {
            try
            {
                var response = _goldLossTangSlipService.GetSlip(request.Id);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("CancelGoldLossTangSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CancelGoldLossTangSlip([FromBody] CancelGoldLossTangSlipRequest request)
        {
            try
            {
                await _goldLossTangSlipService.CancelSlip(request.Id);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("WagesByProcess")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Worker.WagesByProcess.SearchResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult WagesByProcess([FromBody] jewelry.Model.Worker.WagesByProcess.SearchRequest request)
        {
            try
            {
                var response = _service.WagesByProcess(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("WagesMonthlyTrend")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Worker.WagesMonthlyTrend.SearchResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult WagesMonthlyTrend([FromBody] jewelry.Model.Worker.WagesMonthlyTrend.SearchRequest request)
        {
            try
            {
                var response = _service.WagesMonthlyTrend(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("UpdateGoldLossTangSlip")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(GoldLossTangSlipResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateGoldLossTangSlip([FromBody] UpdateGoldLossTangSlipRequest request)
        {
            try
            {
                var response = await _goldLossTangSlipService.UpdateSlip(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

    }
}
