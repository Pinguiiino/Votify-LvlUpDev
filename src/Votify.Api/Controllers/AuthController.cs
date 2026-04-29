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
                var user = await _service.RegisterUserAsync(dto.Username, dto.Email, dto.Password, dto.Role);
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

                // Determina el rol
                string role = user switch
                {
                    Organizer => "Organizer",
                    Auditor => "Auditor",
                    Jury => "Jury",
                    Participant => "Participant",
                    _ => "Public"
                };

                // Genera el token con el rol
                var token = _tokenService.GenerateToken(user.Id, user.Email, role);

                return Ok(new
                {
                    token,        // <- el frontend guarda este token
                    Id = user.Id,
                    Username = user.Name,
                    Email = user.Email,
                    Role = role
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                await _service.ChangePasswordAsync(dto.UserId, dto.CurrentPassword, dto.NewPassword);
                return Ok(new { message = "Contraseña actualizada con éxito" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Error interno del servidor.");
            }
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
        public string Role { get; set; } = "GeneralUser";
    }
    public class ChangePasswordDto
    {
        public string UserId { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
