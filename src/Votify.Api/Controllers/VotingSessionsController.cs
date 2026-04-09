 using Microsoft.AspNetCore.Mvc;
 using Votify.Domain.VoteFolder;

    namespace Votify.Api.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class VotingSessionsController : ControllerBase
        {
            [HttpGet("active/{eventId}")]
            public IActionResult GetActive(string eventId)
            {
                // PROVISIONAL: SESION ACTIVA PARA PROBAR VOTOS
                return Ok(new
                {
                    Id = "temp-session-id",
                    StartDate = DateTime.UtcNow.AddHours(-1),
                    EndDate = DateTime.UtcNow.AddHours(1),
                    IsActive = true
                });
            }
        }
    }