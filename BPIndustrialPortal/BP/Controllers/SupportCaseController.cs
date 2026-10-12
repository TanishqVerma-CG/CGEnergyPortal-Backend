using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers
{
    [ApiController]
    [Authorize]
    public class SupportCaseController : ControllerBase
    {
        private readonly ISupportCaseService _supportCaseService;

        public SupportCaseController(ISupportCaseService supportCaseService)
        {
            _supportCaseService = supportCaseService;
        }

        [HttpGet]
        [Route("api/GetSupportCases")]
        [ProducesResponseType(typeof(SupportCaseListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSupportCases()
        {
            try
            {
                return Ok(_supportCaseService.GetAllCases());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetSupportCaseById")]
        [ProducesResponseType(typeof(SupportCaseDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSupportCaseById([FromQuery][Required] string caseId)
        {
            try
            {
                var result = _supportCaseService.GetCaseById(caseId);

                if (result is null)
                    return NotFound(new { message = $"Case '{caseId}' not found." });

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/AddSupportRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddSupportRequest([FromBody] SupportRequest supportRequest)
        {
            try
            {
                if (string.Equals(supportRequest.Role, "broker", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(supportRequest.CustomerName))
                        return BadRequest(new { message = "CustomerName is required for broker support requests." });

                    var brokerRes = _supportCaseService.AddBrokerSupportRequest(supportRequest);
                    return brokerRes;
                }

                var res = _supportCaseService.AddSupportRequest(supportRequest);
                return res;
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetRequestTypes")]
        [ProducesResponseType(typeof(RequestTypeListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetRequestTypes()
        {
            try
            {
                return Ok(_supportCaseService.GetRequestTypes());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetCaseChat")]
        [ProducesResponseType(typeof(CaseChatResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetCaseChat([FromQuery][Required] string caseId)
        {
            try
            {
                var result = _supportCaseService.GetCaseChat(caseId);
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/SendMessage")]
        [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var result = _supportCaseService.SendMessage(request);
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
