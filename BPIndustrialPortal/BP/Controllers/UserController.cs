using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BP.Controllers
{
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Route("api/GetUserDetailById")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetUserDetailById([FromQuery][Required]int id)
        {
            try
            {
                var user = _userService.GetUserById(id);

                if (user is null)
                    return NotFound(new { message = $"User with Id {id} not found." });

                var response = new User
                {
                    UserId                = user.UserId,
                    Name                   = user.Name,
                    EmailId                = user.EmailId,
                    ContactNumber          = user.ContactNumber,
                    PrimaryBusinessNumber  = user.PrimaryBusinessNumber,
                    Address1               = user.Address1,
                    Address2               = user.Address2,
                    City                   = user.City,
                    State                  = user.State,
                    Zipcode                = user.Zipcode,
                    Country                = user.Country,
                    Role                    = user.Role,
                    OrganizationName        = user.OrganizationName
                };

                return Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,  new { message = ex.Message });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("api/UpdateUserDetail")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult UpdateUserDetail([FromBody] UpdateUser user)
        {
            try
            {
               var  upUser = _userService.UpdateUserDetail(user);
                if (upUser is null)
                    return NotFound(new { message = $"User with Id {user.UserId} not found." });
                var response = new UpdateUser
                {
                    UserId                = upUser.UserId,
                    Name                   = upUser.Name,
                    EmailId                = upUser.EmailId,
                    ContactNumber          = upUser.ContactNumber,
                    PrimaryBusinessNumber  = upUser.PrimaryBusinessNumber,
                    Role                    = upUser.Role,
                    OrganizationName        = upUser.OrganizationName
                };
                return Ok(response);


            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("api/UpdateUserAddress")]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult UpdateUserAddress([FromBody] UserAddress userAddress)
        {
            try
            {
                var upUser = _userService.UpdateUserAddress(userAddress);
                if (upUser is null)
                    return NotFound(new { message = $"User with Id {userAddress.UserId} not found." });
                var response = new UserAddress
                {
                    UserId                = upUser.UserId,
                    Address1               = upUser.Address1,
                    Address2               = upUser.Address2,
                    City                   = upUser.City,
                    State                  = upUser.State,
                    Zipcode                = upUser.Zipcode,
                    Country                = upUser.Country
                };
                return Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("api/ChangePassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult ChangePassword([FromBody] ChangePassword request)
        {
            try
            {
                var result = _userService.ChangePassword(request);

                if (!result)
                    return BadRequest(new { message = "User not found or current password is incorrect." });

                return Ok(new { message = "Password changed successfully." });
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
