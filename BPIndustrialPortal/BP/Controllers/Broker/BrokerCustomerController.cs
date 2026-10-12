using BP.IService;
using BP.Models;
using BP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BP.Controllers.Broker
{
    [ApiController]
    [Authorize(Policy = "Broker")]
    public class BrokerCustomerController : ControllerBase
    {
        private readonly IBrokerCustomerService _brokerCustomerService;

        public BrokerCustomerController(IBrokerCustomerService brokerCustomerService)
        {
            _brokerCustomerService = brokerCustomerService;
        }

        [HttpGet]
        [Route("api/broker/GetCustomerSummaryCards")]
        [ProducesResponseType(typeof(BrokerSummaryCards), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerSummaryCards()
        {
            try
            {
                return Ok(_brokerCustomerService.GetSummaryCards());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetCustomerPortfolio")]
        [ProducesResponseType(typeof(BrokerCustomerPortfolioResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerPortfolio(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null)
        {
            try
            {
                return Ok(_brokerCustomerService.GetCustomerPortfolio(page, pageSize, search));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }


        [HttpGet]
        [Route("api/broker/GetCustomerContract")]
        [ProducesResponseType(typeof(List<BrokerContract>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerContract([FromQuery] [Required] int customerId, [Required] string contractId)
        {
            try
            {
                return Ok(_brokerCustomerService.GetCustomerContract(customerId,contractId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetCustomerMeters")]
        [ProducesResponseType(typeof(BrokerMeterListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerMeters([FromQuery] int customerId)
        {
            try
            {
                return Ok(_brokerCustomerService.GetCustomerMeters(customerId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetCustomerContractIds")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerContractIds([FromQuery][Required] int customerId)
        {
            try
            {
                var ids = _brokerCustomerService.GetCustomerContractIds(customerId);
                return Ok(ids);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetCustomerMeterIds")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerMeterIds([FromQuery][Required] int customerId)
        {
            try
            {
                var ids = _brokerCustomerService.GetCustomerMeterIds(customerId);
                return Ok(ids);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetBrokerUsageTrend")]
        [ProducesResponseType(typeof(BrokerUsageTrendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetBrokerUsageTrend([FromQuery][Required] string meterId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var end = endDate ?? start.AddMonths(1).AddDays(-1);

                var result = _brokerCustomerService.GetBrokerUsageTrend(meterId, start, end);

                if (result is null)
                    return NotFound(new { message = $"No usage trend data found for meter '{meterId}'." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetCustomerListForDropdown")]
        [ProducesResponseType(typeof(List<BrokerCustomerDropdown>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerListForDropdown()
        {
            try
            {
                var list = _brokerCustomerService.GetCustomerListForDropdown();
                return Ok(list);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}
