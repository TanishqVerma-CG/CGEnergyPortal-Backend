using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BP.Controllers.Broker
{
    [ApiController]
    [Authorize(Policy = "Broker")]
    public class BrokerDealsController : ControllerBase
    {
        private readonly IBrokerDealsService _dealsService;

        public BrokerDealsController(IBrokerDealsService dealsService)
        {
            _dealsService = dealsService;
        }

        [HttpGet]
        [Route("api/broker/commission/deals/GetMyDeals")]
        [ProducesResponseType(typeof(DealsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMyDeals([FromQuery] string? agencyId = null)
        {
            try
            {
                return Ok(_dealsService.GetMyDeals(agencyId));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/commission/deals/ExportDeals")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ExportDeals([FromQuery] string? agencyId = null)
        {
            try
            {
                var bytes = _dealsService.ExportDeals(agencyId);
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "deals.xlsx");
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}
