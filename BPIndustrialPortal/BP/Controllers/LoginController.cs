using BP.IService;
using BP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BP.Controllers
{
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public LoginController(IJwtService jwtService, IUserService userService,
        IEmailService emailService, IConfiguration config)
        {
            _jwtService = jwtService;
            _userService = userService;
            _emailService = emailService;
            _config = config;
        }

        [HttpPost]
        [Route("api/Login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                var role = request.IsBroker ? "Broker" : "Billing";
                var user = _userService.ValidateUser(request.EmailId, request.Password, role);

                if (user is null)
                    return Unauthorized(new { message = "Invalid email or password." });

                var response = _jwtService.GenerateToken(user, role);
                return Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                   new { message = ex.Message });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("api/ForgotPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var user = _userService.GetUserByEmail(request.EmailId);

                if (user is null)
                    return Ok(new { message = "Password reset email sent successfully." });

                var token = _userService.GeneratePasswordResetToken(user.UserId);

                var baseUrl = _config["AppSettings:FrontendBaseUrl"];
                var resetUrl = $"{baseUrl}/auth/reset-password?token={token}";
                if(!string.IsNullOrWhiteSpace(request.Client))
                {
                    resetUrl += $"&client={Uri.EscapeDataString(request.Client)}";
                }

                var subject = "Reset Your Password";
                var body = $@"
                <p>Hi {user.Name},</p>
                <p>We received a request to reset your password. Click the link below to set a new password:</p>
                <p><a href=""{resetUrl}"">Reset Password</a></p>
                <p>If you did not request a password reset, please ignore this email.</p>";

                await _emailService.SendAsync(user.EmailId, subject, body);

                return Ok(new { message = "Password reset email sent successfully." });
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
        [Route("api/ResendForgotPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            return await ForgotPassword(request);
        }

        [HttpPost]
        [Route("api/ResetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = _userService.ResetPasswordByToken(request.Token, request.NewPassword);

                if (!result)
                    return BadRequest(new { message = "Invalid or expired password reset token." });

                return Ok(new { message = "Password has been reset successfully." });
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
        [Route("api/FindUserId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FindUserId([FromBody] FindUserIdRequest request)
        {
            try
            {
                var user = _userService.GetUserByEmail(request.EmailId);

                if (user is null)
                    return NotFound(new { message = "No account found with that email address." });

                var code = _userService.GenerateVerificationCode(user.UserId);
                var maskedEmail = MaskEmail(user.EmailId);

                var subject = $"Your Verification Code";
                var body = $@"<p>Hi {user.Name},</p>
                    <p>Use the verification code below to find your registered User ID:</p>
                    <h2 style=""letter-spacing:4px"">{code}</h2>
                     <p>If you did not request this, please ignore this email.</p>";

                await _emailService.SendAsync(user.EmailId, subject, body);

                return Ok(new { maskedEmail });
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
        [Route("api/ResendFindUserIdEmail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResendFindUserIdEmail([FromBody] FindUserIdRequest request)
        {
            return await FindUserId(request);
        }

        [HttpPost]
        [Route("api/VerifyCode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult VerifyCode([FromBody] VerifyCodeRequest request)
        {
            try
            {
                var user = _userService.GetUserByEmail(request.EmailId);

                if (user is null)
                    return NotFound(new { message = "No account found with that email address." });

                var isValid = _userService.VerifyAndConsumeVerificationCode(user.UserId, request.Code);

                if (!isValid)
                    return BadRequest(new { message = "Invalid verification code." });

                return Ok(new { userId = user.UserId });
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
        private static string MaskEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
                return email;

            var local = email[..atIndex];
            var domain = email[atIndex..];
            var visible = local[..Math.Min(2, local.Length)];
            var masked = visible + new string('*', local.Length - visible.Length);
            return masked + domain;
        }

        [HttpPost]
        [Route("api/CreateAccount")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateAccount([FromBody] CreateAccountRequest request)
        {
            try
            {
                var validationErrors = request.GetValidationErrors().ToList();
                if (validationErrors.Any())
                    return BadRequest(new { errors = validationErrors });

                var user = _userService.CreateUser(request);

                if (user is null)
                    return Conflict(new { message = "An account with this email already exists." });

                var role = request.IsBroker ? "Broker" : "Billing";
                var response = _jwtService.GenerateToken(user, role);
                return Ok(response);
            }
            catch (System.Data.DuplicateNameException ex)
            {
                return Conflict(new { message = ex.Message });
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
