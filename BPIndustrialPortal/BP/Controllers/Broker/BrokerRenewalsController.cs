using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers.Broker
{
    [ApiController]
    [Authorize(Policy = "Broker")]
    public class BrokerRenewalsController : ControllerBase
    {
        private readonly IBrokerRenewalsService _brokerRenewalsService;

        public BrokerRenewalsController(IBrokerRenewalsService brokerRenewalsService)
        {
            _brokerRenewalsService = brokerRenewalsService;
        }


        [HttpGet]
        [Route("api/broker/renewals/GetRenewalsSummary")]
        [ProducesResponseType(typeof(BrokerRenewalsSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewalsSummary()
        {
            try
            {
                return Ok(_brokerRenewalsService.GetRenewalsSummary());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/renewals/GetCustomerRenewalsList")]
        [ProducesResponseType(typeof(BrokerCustomerRenewalsListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerRenewalsList(
            [FromQuery] string sortBy = "customer",
            [FromQuery] string sortOrder = "asc")
        {
            try
            {
                return Ok(_brokerRenewalsService.GetCustomerRenewalsList(sortBy, sortOrder));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/renewals/GetCustomerRenewalSummary")]
        [ProducesResponseType(typeof(BrokerCustomerRenewalSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerRenewalSummary([FromQuery][Required] string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return BadRequest(new { message = "companyName is required." });
            try
            {
                return Ok(_brokerRenewalsService.GetCustomerRenewalSummary(companyName));
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
        [Route("api/broker/renewals/GetCustomerContracts")]
        [ProducesResponseType(typeof(BrokerCustomerContractsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCustomerContracts(
            [FromQuery][Required] string companyName,
            [FromQuery] string serviceAddress = "all",
            [FromQuery] string expiringContract = "all",
            [FromQuery] string sortBy = "daysRemaining",
            [FromQuery] string sortOrder = "asc")
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return BadRequest(new { message = "companyName is required." });
            try
            {
                return Ok(_brokerRenewalsService.GetCustomerContracts(companyName, serviceAddress, expiringContract, sortBy, sortOrder));
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
        [Route("api/broker/renewals/GetServiceAddressDropdown")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetServiceAddressDropdown([FromQuery][Required] string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return BadRequest(new { message = "companyName is required." });
            try
            {
                return Ok(_brokerRenewalsService.GetServiceAddressDropdown(companyName));
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
        [Route("api/broker/renewals/GetContractDropdown")]
        [ProducesResponseType(typeof(List<BrokerContractDropdownOption>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractDropdown([FromQuery][Required] string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return BadRequest(new { message = "companyName is required." });
            try
            {
                return Ok(_brokerRenewalsService.GetContractDropdown(companyName));
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
        [Route("api/broker/renewals/GetQuoteByContractId")]
        [ProducesResponseType(typeof(BrokerContractQuoteHistory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetQuoteByContractId([FromQuery][Required] string contractId)
        {
            if (string.IsNullOrWhiteSpace(contractId))
                return BadRequest(new { message = "contractId is required." });
            try
            {
                var result = _brokerRenewalsService.GetQuoteByContractId(contractId);
                if (result is null)
                    return NotFound(new { message = $"Contract '{contractId}' not found." });
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/renewals/GetRenewalQuoteDetail")]
        [ProducesResponseType(typeof(RenewalQuoteDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewalQuoteDetail([FromQuery][Required] string quoteId)
        {
            if (string.IsNullOrWhiteSpace(quoteId))
                return BadRequest(new { message = "quoteId is required." });
            try
            {
                var result = _brokerRenewalsService.GetRenewalQuoteDetail(quoteId);
                if (result is null)
                    return NotFound(new { message = $"Quote '{quoteId}' not found." });
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        private static readonly HashSet<string> ValidSnapshots =
            new(StringComparer.OrdinalIgnoreCase) { "today", "3days", "1week", "1month" };

        private static readonly HashSet<string> ValidPriceUnits =
            new(StringComparer.OrdinalIgnoreCase) { "$/MWh", "$/kWh" };


        [HttpGet]
        [Route("api/broker/renewals/GetPricingIntelligence")]
        [ProducesResponseType(typeof(PricingIntelligenceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPricingIntelligence(
            [FromQuery][Required] string contractId,
            [FromQuery][Required] string snapshot,
            [FromQuery] string priceUnit = "$/MWh")
        {
            if (string.IsNullOrWhiteSpace(contractId))
                return BadRequest(new { message = "contractId is required." });
            if (string.IsNullOrWhiteSpace(snapshot))
                return BadRequest(new { message = "snapshot is required." });
            if (!ValidSnapshots.Contains(snapshot))
                return BadRequest(new { message = "snapshot must be one of: today, 3days, 1week, 1month." });
            if (!ValidPriceUnits.Contains(priceUnit))
                return BadRequest(new { message = "priceUnit must be one of: $/MWh, $/kWh." });

            try
            {
                var result = _brokerRenewalsService.GetPricingIntelligence(contractId, snapshot, priceUnit);

                if (result is null)
                    return NotFound(new { message = $"Pricing intelligence data for contract '{contractId}' with snapshot '{snapshot}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/renewals/GetPricingTrend")]
        [ProducesResponseType(typeof(PricingTrendResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPricingTrend([FromQuery][Required] string contractId)
        {
            if (string.IsNullOrWhiteSpace(contractId))
                return BadRequest(new { message = "contractId is required." });

            try
            {
                var result = _brokerRenewalsService.GetPricingTrend(contractId);

                if (result is null)
                    return NotFound(new { message = $"Pricing trend data for contract '{contractId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/renewals/GetPricingSnapshotDetails")]
        [ProducesResponseType(typeof(PricingSnapshotDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPricingSnapshotDetails([FromQuery][Required] string contractId)
        {
            try
            {
                var result = _brokerRenewalsService.GetPricingSnapshotDetails(contractId);

                if (result is null)
                    return NotFound(new { message = $"Pricing snapshot details for contract '{contractId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/renewals/GetContractDetails")]
        [ProducesResponseType(typeof(BrokerContractDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractDetails([FromQuery][Required] string contractId)
        {
            if (string.IsNullOrWhiteSpace(contractId))
                return BadRequest(new { message = "contractId is required." });

            try
            {
                var result = _brokerRenewalsService.GetContractDetails(contractId);

                if (result is null)
                    return NotFound(new { message = $"Contract '{contractId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/broker/renewals/ApproveQuote")]
        [ProducesResponseType(typeof(QuoteStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ApproveQuote([FromBody] QuoteStatusRequest request)
        {
            try
            {
                var result = _brokerRenewalsService.ApproveQuote(request.QuoteId);
                return Ok(result);
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

        [HttpPost]
        [Route("api/broker/renewals/RejectQuote")]
        [ProducesResponseType(typeof(QuoteStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult RejectQuote([FromBody] QuoteStatusRequest request)
        {
            try
            {
                var result = _brokerRenewalsService.RejectQuote(request.QuoteId);
                return Ok(result);
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
    }
}
