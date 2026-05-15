using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Votify.Api.Services;
using Votify.Domain.UserFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;
        private readonly TokenService _tokenService;

        public AuthController(AuthService service, TokenService tokenService)
        {
            _service = service;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var user = await _service.RegisterUserAsync(dto.Username, dto.Email, dto.Password);
                return Ok(new { message = "Cuenta creada con éxito" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _service.LoginAsync(dto.Email, dto.Password);

                string role = "User";

                var token = _tokenService.GenerateToken(user.Id, user.Email, role);

                return Ok(new
                {
                    Id = user.Id,
                    Username = user.Name,
                    Email = user.Email,
                    Role = role,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            var exists = await _service.CheckEmailExistsAsync(email);
            return Ok(exists);
        }

        [HttpGet("user-by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            var user = await _service.GetUserByEmailAsync(email);
            if (user == null) return NotFound();

            return Ok(new { user.Id, Role = "User" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                await _service.ChangePasswordAsync(dto.UserId, dto.CurrentPassword, dto.NewPassword);
                return Ok(new { message = "Contraseña cambiada con éxito." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public class LoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RegisterDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class ChangePasswordDto
        {
            public string UserId { get; set; } = string.Empty;
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }
    }
}