using System.ComponentModel.DataAnnotations;
using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BP.Controllers.Broker
{
    [ApiController]
    [Authorize(Policy = "Broker")]
    public class BrokerStatementsController : ControllerBase
    {
        private readonly IBrokerStatementsService _statementsService;

        public BrokerStatementsController(IBrokerStatementsService statementsService)
        {
            _statementsService = statementsService;
        }

        [HttpGet]
        [Route("api/broker/commission/statements/GetMyStatements")]
        [ProducesResponseType(typeof(StatementListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetMyStatements([FromQuery] string? agencyId = null)
        {
            try
            {
                return Ok(_statementsService.GetMyStatements(agencyId));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/commission/statements/GetStatementDetail")]
        [ProducesResponseType(typeof(StatementDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetStatementDetail([FromQuery][Required] string statementId)
        {
            try
            {
                var result = _statementsService.GetStatementDetail(statementId);
                if (result is null)
                    return NotFound(new { message = $"Statement '{statementId}' not found." });
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/broker/commission/statements/ApproveStatement")]
        [ProducesResponseType(typeof(StatementActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ApproveStatement([FromBody] ApproveStatementRequest request)
        {
            try
            {
                var result = _statementsService.ApproveStatement(request);
                if (!result.Success)
                {
                    if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                        return NotFound(result);
                    return UnprocessableEntity(result);
                }
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/broker/commission/statements/RejectStatement")]
        [ProducesResponseType(typeof(StatementActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult RejectStatement([FromBody] RejectStatementRequest request)
        {
            try
            {
                var result = _statementsService.RejectStatement(request);
                if (!result.Success)
                {
                    if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                        return NotFound(result);
                    return UnprocessableEntity(result);
                }
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/broker/commission/statements/BulkApproveStatements")]
        [ProducesResponseType(typeof(StatementActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult BulkApproveStatements([FromBody] BulkStatementRequest request)
        {
            try
            {
                return Ok(_statementsService.BulkApprove(request));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/broker/commission/statements/BulkRejectStatements")]
        [ProducesResponseType(typeof(StatementActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult BulkRejectStatements([FromBody] BulkStatementRequest request)
        {
            try
            {
                return Ok(_statementsService.BulkReject(request));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/broker/commission/statements/EmailStatement")]
        [ProducesResponseType(typeof(StatementActionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EmailStatement([FromBody] EmailStatementRequest request)
        {
            try
            {
                return Ok(await _statementsService.EmailStatementAsync(request));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/broker/commission/statements/GetStatementsSummary")]
        [ProducesResponseType(typeof(StatementsSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetStatementsSummary([FromQuery] string? agencyId = null)
        {
            try
            {
                return Ok(_statementsService.GetStatementsSummary(agencyId));
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}
