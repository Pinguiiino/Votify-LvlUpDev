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
        try
        {
            var evento = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventoId);
            if (evento == null) return NotFound("Evento no encontrado");

            var proyectosInfo = await _context.Projects
                .Where(p => p.EventId == eventoId)
                .ToDictionaryAsync(p => p.Id, p => p.Title);

            var categorias = await _context.Categories
                .Where(c => c.EventId == eventoId)
                .ToListAsync();

            var categoriasInfo = categorias.ToDictionary(c => c.Id, c => c.Name);
            var topNPorCategoria = categorias.ToDictionary(
                c => c.Id,
                c => c.TopNProjectsAllowed > 0 ? c.TopNProjectsAllowed : 3);

            var projectIds = proyectosInfo.Keys.ToList();

            var votosDelEvento = await _context.Votes
                .Where(v => projectIds.Contains(v.VotedProjectId))
                .ToListAsync();

            var usuariosQueHanVotado = votosDelEvento.Select(v => v.UserId).Distinct().Count();

            var totalRegistrados = await _context.Usuarios.CountAsync();

            var ranking = votosDelEvento
                .GroupBy(v => new { v.VotedProjectId, v.CategoryId })
                .Select(g =>
                {
                    var topN = topNPorCategoria.TryGetValue(g.Key.CategoryId, out var t) ? t : 3;

                    return new ProjectResultDto
                    {
                        Nombre = proyectosInfo.ContainsKey(g.Key.VotedProjectId) ? proyectosInfo[g.Key.VotedProjectId] : "Proyecto",
                        Categoria = categoriasInfo.ContainsKey(g.Key.CategoryId) ? categoriasInfo[g.Key.CategoryId] : "General",
                        Puntos = g.Sum(v => Math.Max(0, (topN - v.TopPosition + 1) * 10))
                    };
                })
                .Where(p => p.Puntos > 0)
                .OrderByDescending(p => p.Puntos)
                .ToList();

            return Ok(new EventDashboardDto
            {
                TotalVotantes = totalRegistrados,
                VotosEmitidos = usuariosQueHanVotado,
                Ranking = ranking
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al obtener estadísticas: {ex.Message}");
        }
    }
}
