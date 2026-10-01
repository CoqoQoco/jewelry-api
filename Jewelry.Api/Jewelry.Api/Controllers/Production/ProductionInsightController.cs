using Jewelry.Api.Extension;
using Jewelry.Service.Production.Insight;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Wip = jewelry.Model.Production.Insight.Wip;
using WipTrend = jewelry.Model.Production.Insight.WipTrend;
using DueRiskPlans = jewelry.Model.Production.Insight.DueRiskPlans;
using StageLeadTime = jewelry.Model.Production.Insight.StageLeadTime;
using AbnormalDwellPlans = jewelry.Model.Production.Insight.AbnormalDwellPlans;
using StageStandards = jewelry.Model.Production.Insight.StageStandards;
using SaveStageStandards = jewelry.Model.Production.Insight.SaveStageStandards;
using StalePlans = jewelry.Model.Report.Executive.StalePlans;
using Delivery = jewelry.Model.Production.Insight.Delivery;
using DeliveryAtRiskPlans = jewelry.Model.Production.Insight.DeliveryAtRiskPlans;
using DeliveryLatePlans = jewelry.Model.Production.Insight.DeliveryLatePlans;
using StuckAfterCostCardPlans = jewelry.Model.Production.Insight.StuckAfterCostCardPlans;
using DeliveryTarget = jewelry.Model.Production.Insight.DeliveryTarget;
using SaveDeliveryTarget = jewelry.Model.Production.Insight.SaveDeliveryTarget;

namespace Jewelry.Api.Controllers.Production
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductionInsightController : ApiControllerBase
    {
        private readonly IProductionInsightService _service;

        public ProductionInsightController(IProductionInsightService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Wip")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Wip.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Wip([FromBody] Wip.Request request)
        {
            try
            {
                var response = await _service.Wip(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("WipTrend")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(WipTrend.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> WipTrend([FromBody] WipTrend.Request request)
        {
            try
            {
                var response = await _service.WipTrend(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StalePlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StalePlans([FromBody] StalePlans.Request request)
        {
            try
            {
                var response = await _service.StalePlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("DueRiskPlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DueRiskPlans([FromBody] DueRiskPlans.Request request)
        {
            try
            {
                var response = await _service.DueRiskPlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StageLeadTime")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(StageLeadTime.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StageLeadTime([FromBody] StageLeadTime.Request request)
        {
            try
            {
                var response = await _service.StageLeadTime(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("AbnormalDwellPlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AbnormalDwellPlans([FromBody] AbnormalDwellPlans.Request request)
        {
            try
            {
                var response = await _service.AbnormalDwellPlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StageStandards")]
        [HttpGet]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<StageStandards.Item>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StageStandards()
        {
            try
            {
                var response = await _service.GetStageStandards();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StageStandardHistory")]
        [HttpGet]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<StageStandards.Item>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StageStandardHistory([FromQuery] string deptKey)
        {
            try
            {
                var response = await _service.GetStageStandardHistory(deptKey);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("SaveStageStandards")]
        [HttpPost]
        [RequirePermission("production:standard-edit")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SaveStageStandards([FromBody] SaveStageStandards.Request request)
        {
            try
            {
                await _service.SaveStageStandards(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Delivery")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Delivery.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Delivery([FromBody] Delivery.Request request)
        {
            try
            {
                var response = await _service.Delivery(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("DeliveryAtRiskPlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeliveryAtRiskPlans([FromBody] DeliveryAtRiskPlans.Request request)
        {
            try
            {
                var response = await _service.DeliveryAtRiskPlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("DeliveryLatePlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeliveryLatePlans([FromBody] DeliveryLatePlans.Request request)
        {
            try
            {
                var response = await _service.DeliveryLatePlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StuckAfterCostCardPlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StuckAfterCostCardPlans([FromBody] StuckAfterCostCardPlans.Request request)
        {
            try
            {
                var response = await _service.StuckAfterCostCardPlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("DeliveryTarget")]
        [HttpGet]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DeliveryTarget.Item))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeliveryTarget()
        {
            try
            {
                var response = await _service.GetDeliveryTarget();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("DeliveryTargetHistory")]
        [HttpGet]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<DeliveryTarget.Item>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeliveryTargetHistory()
        {
            try
            {
                var response = await _service.GetDeliveryTargetHistory();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("SaveDeliveryTarget")]
        [HttpPost]
        [RequirePermission("production:standard-edit")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SaveDeliveryTarget([FromBody] SaveDeliveryTarget.Request request)
        {
            try
            {
                await _service.SaveDeliveryTarget(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
