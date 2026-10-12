using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BP.Controllers
{
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [Route("api/GetNotifications")]
        [ProducesResponseType(typeof(NotificationsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetNotifications()
        {
            try
            {
                return Ok(_notificationService.GetNotifications());
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception) { throw; }
        }
    }
}
