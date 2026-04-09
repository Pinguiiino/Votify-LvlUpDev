using Microsoft.AspNetCore.Mvc;
using Votify.Domain.CategoryFolder;

namespace Votify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _service;

    public CategoriesController(CategoryService service)
    {
        _service = service;
    }

    [HttpGet("by-event/{eventId}")]
    public async Task<IActionResult> GetByEvent(string eventId)
    {
        var categorias = await _service.GetByEventAsync(eventId);
        return Ok(categorias.Select(c => new CategorySimpleDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        }));
    }
}

public class CategorySimpleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}