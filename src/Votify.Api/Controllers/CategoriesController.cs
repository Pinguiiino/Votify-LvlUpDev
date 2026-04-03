using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votify.Infrastructure;

namespace Votify.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly VotifyDbContext Context;

        public CategoriesController(VotifyDbContext context)
        {
            this.Context = context;
        }

        [HttpGet("by-event/{eventId}")]
        public async Task<IActionResult> GetByEvent(string eventId)
        {
            var categorias = await Context.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => new CategorySimpleDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(categorias);
        }
    }

    public class CategorySimpleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}