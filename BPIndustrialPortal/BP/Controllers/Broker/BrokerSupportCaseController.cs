using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers.Broker
{
    [ApiController]
    [Authorize(Policy = "Broker")]
    public class BrokerSupportCaseController : ControllerBase
    {
        private readonly IBrokerSupportCaseService _brokerSupportCaseService;

        public BrokerSupportCaseController(IBrokerSupportCaseService brokerSupportCaseService)
        {
            _brokerSupportCaseService = brokerSupportCaseService;
        }

        [HttpGet]
        [Route("api/broker/GetSupportCases")]
        [ProducesResponseType(typeof(BrokerSupportCaseListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSupportCases(
            [FromQuery] string? raisedBy = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                return Ok(_brokerSupportCaseService.GetCases(raisedBy, page, pageSize, search));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetSupportCaseSummary")]
        [ProducesResponseType(typeof(BrokerSupportCaseSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSupportCaseSummary()
        {
            try
            {
                return Ok(_brokerSupportCaseService.GetSummary());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetSupportCaseById")]
        [ProducesResponseType(typeof(BrokerSupportCase), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSupportCaseById([FromQuery][Required] string caseNumber)
        {
            try
            {
                var result = _brokerSupportCaseService.GetCaseById(caseNumber);

                if (result is null)
                    return NotFound(new { message = $"Case '{caseNumber}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/GetSupportCaseChat")]
        [ProducesResponseType(typeof(CaseChatResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSupportCaseChat([FromQuery][Required] string caseNumber)
        {
            try
            {
                var result = _brokerSupportCaseService.GetCaseChat(caseNumber);

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/broker/SendSupportCaseMessage")]
        [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult SendSupportCaseMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var result = _brokerSupportCaseService.SendMessage(request);

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
