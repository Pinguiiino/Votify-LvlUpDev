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
        // 1. Obtenemos los proyectos que pertenecen a este EventId (que sí existe en tu constructor)
        var proyectosDelEvento = await _context.Projects
            .Where(p => p.EventId == eventoId)
            .ToListAsync();

        var idsProyectos = proyectosDelEvento.Select(p => p.Id).ToList();

        // 2. Contamos cuántos votos hay para esos proyectos
        var votosEmitidos = await _context.Votes
            .Where(v => idsProyectos.Contains(v.VotedProjectId))
            .CountAsync();

        // 3. Calculamos el Ranking con las PONDERACIONES
        var ranking = await _context.Votes
            .Where(v => idsProyectos.Contains(v.VotedProjectId))
            .GroupBy(v => v.VotedProjectId)
            .Select(g => new ProjectResultDto
            {
                // Buscamos el proyecto en la lista que ya bajamos a memoria para ir más rápido
                Nombre = proyectosDelEvento.FirstOrDefault(p => p.Id == g.Key).Title ?? "Sin nombre",

                // Como CategoryId no existe en tu Project, ponemos el EventId o "General" 
                // para que no de error.
                Categoria = "General",

                // Ponderación: 1º (50 pts), 2º (40 pts), etc.
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