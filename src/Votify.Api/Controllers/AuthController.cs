using Microsoft.AspNetCore.Mvc;
using Votify.Domain.UserFolder;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;

        public AuthController(AuthService service)
        {
            _service = service;
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
                // Devuelve el error (ej: "El correo ya está en uso") para que el Frontend lo muestre en rojo
                return BadRequest(ex.Message);
            }
        }
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "GeneralUser";
    }
}
