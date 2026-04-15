using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Votify.Domain.EventFolder;
using Votify.Infrastructure;

namespace Votify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly VotifyDbContext _context;

    public DashboardController(VotifyDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats/{eventoId}")]
    public async Task<ActionResult<EventDashboardDto>> GetStats(string eventoId)
    {
        // 1. Contamos TODOS los votos de la tabla (sin filtrar por ID raro)
        var votosEmitidos = await _context.Votes.CountAsync();

        // 2. Ranking con ponderaciones de los proyectos que tengan votos
        var ranking = await _context.Votes
            .GroupBy(v => v.VotedProjectId)
            .Select(g => new ProjectResultDto
            {
                // Buscamos el nombre del proyecto en la tabla Projects
                Nombre = _context.Projects.Where(p => p.Id == g.Key).Select(p => p.Title).FirstOrDefault() ?? "Proyecto",
                Categoria = "General",
                Puntos = g.Sum(v => (6 - v.TopPosition) * 10)
            })
            .OrderByDescending(p => p.Puntos)
            .Take(5)
            .ToListAsync();

        return Ok(new EventDashboardDto
        {
            TotalVotantes = 50,
            VotosEmitidos = votosEmitidos,
            Ranking = ranking
        });
    }
}