using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BP.Controllers
{
    [ApiController]
    [Authorize]
    public class PricingAlertController : ControllerBase
    {
        private readonly IPricingAlertService _pricingAlertService;

        public PricingAlertController(IPricingAlertService pricingAlertService)
        {
            _pricingAlertService = pricingAlertService;
        }
        [HttpPost]
        [Route("api/SetPricingAlert")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult SetPricingAlert([FromBody] PricingAlertRequest request)
        {           

            var validFrequencies = new[] { "daily", "weekly", "monthly" };
            if (!validFrequencies.Contains(request.Frequency?.ToLower()))
                return BadRequest(new { message = "Frequency must be Daily, Weekly, or Monthly." });


            var result = _pricingAlertService.ScheduleAlert(request.Email ?? string.Empty, request.Frequency);

            return Ok(new { message = result });
        }
    }
}
