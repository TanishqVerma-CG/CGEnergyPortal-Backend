using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers
{
    [ApiController]
    [Authorize(Policy = "Billing")]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService;
        }


        [HttpGet]
        [Route("api/GetContractDropdown")]
        [ProducesResponseType(typeof(ContractDropdownResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractDropdown()
        {
            try
            {
                return Ok(_contractService.GetContractDropdown());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetContractSummary")]
        [ProducesResponseType(typeof(ContractSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractSummary([FromQuery] string contractId = "all")
        {
            try
            {
                var result = _contractService.GetContractSummary(contractId);

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
        [Route("api/GetContractHistory")]
        [ProducesResponseType(typeof(List<ContractHistoryEntry>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractHistory([FromQuery] string contractId = "all")
        {
            try
            {
                var result = _contractService.GetContractHistory(contractId);

                if (result is null)
                    return NotFound(new { message = $"Contract history for '{contractId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetContractDetailById")]
        [ProducesResponseType(typeof(ContractDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractDetailById([FromQuery][Required] string id)
        {
            try
            {
                var result = _contractService.GetContractDetailById(id);

                if (result is null)
                    return NotFound(new { message = $"Contract '{id}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetMetersByContractId")]
        [ProducesResponseType(typeof(MeterTableResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMetersByContractId([FromQuery][Required] string contractId)
        {
            try
            {
                var result = _contractService.GetMetersByContractId(contractId);

                if (result is null)
                    return NotFound(new { message = $"No meters found for contract '{contractId}'." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetContracts")]
        [ProducesResponseType(typeof(ContractTableResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContracts()
        {
            try
            {
                return Ok(_contractService.GetContracts());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetRenewContracts")]
        [ProducesResponseType(typeof(RenewContractsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewContracts()
        {
            try
            {
                return Ok(_contractService.GetRenewContracts());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetRenewalContracts")]
        [ProducesResponseType(typeof(RenewalTableResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewalContracts()
        {
            try
            {
                return Ok(_contractService.GetRenewalContracts());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetQuoteByContractID")]
        [ProducesResponseType(typeof(ContractQuoteHistory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetQuoteByContractID([FromQuery] string contractId)
        {
            try
            {
                var result = _contractService.GetQuoteByContractId(contractId);

                if (result is null)
                    return NotFound(new { message = $"No quotes found for contract '{contractId}'." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetRenewalAddressDD")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewalAddressDD()
        {
            try
            {
                return Ok(_contractService.GetRenewalAddressDropdown());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetRenewalSummary")]
        [ProducesResponseType(typeof(RenewalSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewalSummary()
        {
            try
            {
                return Ok(_contractService.GetRenewalSummary());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetRenewalQuoteDetail")]
        [ProducesResponseType(typeof(RenewalQuoteDetail), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRenewalQuoteDetail([FromQuery][Required] string quoteId)
        {
            try
            {
                var result = _contractService.GetRenewalQuoteDetail(quoteId);

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
        [Route("api/GetPricingIntelligence")]
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
                var result = _contractService.GetPricingIntelligence(contractId, snapshot, priceUnit);

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

        [HttpPost]
        [Route("api/SubmitFormalQuote")]
        [ProducesResponseType(typeof(FormalQuoteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult SubmitFormalQuote([FromBody] FormalQuoteRequest request)
        {
            try
            {
                var result = _contractService.SubmitFormalQuote(request);
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/RequestContractRenewal")]
        [ProducesResponseType(typeof(RenewContractResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult RequestContractRenewal([FromBody] RenewContractRequest request)
        {
            try
            {
                var result = _contractService.RequestContractRenewal(request);
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetPricingTrend")]
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
                var result = _contractService.GetPricingTrend(contractId);

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
        [Route("api/GetPricingSnapshotDetails")]
        [ProducesResponseType(typeof(PricingSnapshotDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPricingSnapshotDetails([FromQuery][Required] string contractId)
        {
            try
            {
                var result = _contractService.GetPricingSnapshotDetails(contractId);

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
        [Route("api/GetContractInfo")]
        [ProducesResponseType(typeof(ContractInfoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetContractInfo([FromQuery][Required] string contractId)
        {
            try
            {
                var result = _contractService.GetContractInfo(contractId);

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
    }
}
