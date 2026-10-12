using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers
{
    [ApiController]
    [Authorize(Policy = "Billing")]
    public class UsageController : ControllerBase
    {
        private readonly IUsageService _usageService;

        public UsageController(IUsageService usageService)
        {
            _usageService = usageService;
        }

        [HttpGet]
        [Route("api/GetMeterDropdown")]
        [ProducesResponseType(typeof(MeterDropdownResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMeterDropdown()
        {
            try
            {
                var result = _usageService.GetMeterDropdown();
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetMeterSummary")]
        [ProducesResponseType(typeof(MeterSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMeterSummary([FromQuery] string meterId = "all")
        {
            try
            {
                var result = _usageService.GetMeterSummary(meterId);

                if (result is null)
                    return NotFound(new { message = $"Meter '{meterId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetContractMeterDropdown")]
        [ProducesResponseType(typeof(ContractUsageMeterDropdownResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractMeterDropdown([FromQuery][Required] string contractId)
        {
            try
            {
                var result = _usageService.GetContractMeterDropdown(contractId);
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetContractMeterSummary")]
        [ProducesResponseType(typeof(ContractMeterSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractMeterSummary([FromQuery][Required] string contractId, [FromQuery] string meterId = "all")
        {
            try
            {
                var result = _usageService.GetContractMeterSummary(contractId, meterId);

                if (result is null)
                    return NotFound(new { message = $"Meter '{meterId}' not found for contract '{contractId}'." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetMeterIdsByContractId")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMeterIdsByContractId([FromQuery][Required] string contractId)
        {
            try
            {
                var result = _usageService.GetMeterIdsByContractId(contractId);
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetUsageTrend")]
        [ProducesResponseType(typeof(UsageTrendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetUsageTrend([FromQuery][Required] string contractId, [FromQuery] string meterId = "all", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var end = endDate ?? start.AddMonths(1).AddDays(-1);

                var result = _usageService.GetUsageTrend(contractId, meterId, start, end);

                if (result is null)
                    return BadRequest(new { message = $"No usage trend data found for contract '{contractId}' and meter '{meterId}'." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}

