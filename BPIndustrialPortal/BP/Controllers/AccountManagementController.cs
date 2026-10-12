using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers
{
    [ApiController]
    [Authorize(Policy = "Billing")]
    public class AccountManagementController : ControllerBase
    {
        private readonly IAccountManagementService _accountManagementService;

        public AccountManagementController(IAccountManagementService accountManagementService)
        {
            _accountManagementService = accountManagementService;
        }

        [HttpGet]
        [Route("api/GetAccountManagementPage")]
        [ProducesResponseType(typeof(AccountManagementPageResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAccountManagementPage()
        {
            try
            {
                return Ok(_accountManagementService.GetAccountManagementPage());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetAccountManagementSummary")]
        [ProducesResponseType(typeof(AccountManagementSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAccountManagementSummary()
        {
            try
            {
                return Ok(_accountManagementService.GetSummary());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetAllUsers")]
        [ProducesResponseType(typeof(AccountUsersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAllUsers()
        {
            try
            {
                return Ok(_accountManagementService.GetAllUsers());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        [Route("api/GetPendingInvitations")]
        [ProducesResponseType(typeof(PendingInvitationsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPendingInvitations()
        {
            try
            {
                return Ok(_accountManagementService.GetPendingInvitations());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/InviteUser")]
        [ProducesResponseType(typeof(InviteUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult InviteUser([FromBody] InviteUserRequest request)
        {
            try
            {
                var result = _accountManagementService.InviteUser(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/ReSendInviteUser")]
        [ProducesResponseType(typeof(InviteUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ReSendInviteUser([FromQuery][Required]int userId, [Required] string name, [Required]string emailId, [Required] string client)
        {
            try
            {
                var result = _accountManagementService.SendInvitationEmail(userId, name, emailId, client);
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
        [HttpPost]
        [Route("api/DeactivateUser")]
        [ProducesResponseType(typeof(DeactivateUserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DeactivateUser([FromQuery][Required] string userId)
        {
            try
            {
                var result = _accountManagementService.DeactivateUser(userId);
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
        [Route("api/ChangePermission")]
        [ProducesResponseType(typeof(ChangePermissionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ChangePermission([FromBody] ChangePermissionRequest request)
        {
            try
            {
                var result = _accountManagementService.ChangePermission(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        [Route("api/ResetAmPassword")]
        [ProducesResponseType(typeof(ResetAmPasswordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ResetAmPassword([FromBody] ResetAmPasswordRequest request)
        {
            try
            {
                var result = _accountManagementService.ResetPassword(request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}
