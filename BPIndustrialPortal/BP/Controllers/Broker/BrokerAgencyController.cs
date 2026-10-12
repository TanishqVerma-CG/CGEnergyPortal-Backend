using System.ComponentModel.DataAnnotations;
using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BP.Controllers.Broker
{
    [ApiController]
    [Authorize(Policy = "Broker")]
    public class BrokerAgencyController : ControllerBase
    {
        private readonly IBrokerAgencyService _agencyService;

        public BrokerAgencyController(IBrokerAgencyService agencyService)
        {
            _agencyService = agencyService;
        }

        [HttpGet]
        [Route("api/broker/commission/agency/SearchBrokers")]
        [ProducesResponseType(typeof(BrokerSearchResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult SearchBrokers(
            [FromQuery] string? contactName = null,
            [FromQuery] string? agencyName = null)
        {
            try
            {
                return Ok(_agencyService.SearchBrokers(contactName, agencyName));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/commission/agency/GetAgencyDashboard")]
        [ProducesResponseType(typeof(AgencyDashboardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAgencyDashboard([FromQuery][Required] string agencyId)
        {
            try
            {
                var result = _agencyService.GetAgencyDashboard(agencyId);
                if (result is null)
                    return NotFound(new { message = $"Agency '{agencyId}' not found." });
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
