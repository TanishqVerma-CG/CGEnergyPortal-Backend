using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BP.Controllers.Broker
{
    [ApiController]
    [Authorize(Policy = "Broker")]
    public class BrokerHomeController : ControllerBase
    {
        private readonly IBrokerHomeService _brokerHomeService;

        public BrokerHomeController(IBrokerHomeService brokerHomeService)
        {
            _brokerHomeService = brokerHomeService;
        }

        [HttpGet]
        [Route("api/broker/home/GetSummaryMetrics")]
        [ProducesResponseType(typeof(BrokerSummaryMetrics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSummaryMetrics()
        {
            try
            {
                return Ok(_brokerHomeService.GetSummaryMetrics());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/home/GetRecentActivity")]
        [ProducesResponseType(typeof(BrokerRecentActivity), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRecentActivity()
        {
            try
            {
                return Ok(_brokerHomeService.GetRecentActivity());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/home/GetRenewalWatchlist")]
        [ProducesResponseType(typeof(RenewalWatchlistResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewalWatchlist(
            [FromQuery] string daysFilter = "all",
            [FromQuery] string sortBy = "daysRemaining",
            [FromQuery] string sortOrder = "asc")
        {
            try
            {
                return Ok(_brokerHomeService.GetRenewalWatchlist(daysFilter, sortBy, sortOrder));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/home/ExportRenewalWatchlist")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ExportRenewalWatchlist([FromQuery] string daysFilter = "all")
        {
            try
            {
                var excelBytes = _brokerHomeService.ExportRenewalWatchlist(daysFilter);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"renewal-watchlist-{timestamp}.xlsx");
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/home/GetOpenSupportRequests")]
        [ProducesResponseType(typeof(BrokerOpenSupportRequestsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetOpenSupportRequests(
            [FromQuery] string sortBy = "createdOn",
            [FromQuery] string sortOrder = "desc")
        {
            try
            {
                return Ok(_brokerHomeService.GetOpenSupportRequests(sortBy, sortOrder));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}
