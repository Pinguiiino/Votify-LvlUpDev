using Microsoft.AspNetCore.Mvc;
using Votify.Domain.CategoryFolder;
using Votify.Domain.VoteFolder;

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
        return Ok(categorias.Select(ToSimpleDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var categoria = await _service.GetWithDetailsAsync(id);
        if (categoria == null) return NotFound();
        return Ok(ToDto(categoria));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        try
        {
            var data = new CreateCategoryData
            {
                EventId = dto.EventId,
                Name = dto.Name,
                Description = dto.Description,
                AllowSelfVoting = dto.AllowSelfVoting,
                CombineResults = dto.CombineResults,
                JuryWeight = dto.JuryWeight,
                PublicWeight = dto.PublicWeight,
                VotingSessions = dto.VotingSessions.Select(MapToData).ToList()
            };

            var categoria = await _service.CreateAsync(data);
            return Ok(new { message = "Categoría creada", id = categoria.Id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/voting")]
    public async Task<IActionResult> UpdateVoting(string id, [FromBody] UpdateCategoryVotingDto dto)
    {
        try
        {
            var data = new UpdateCategoryVotingData
            {
                AllowSelfVoting = dto.AllowSelfVoting,
                CombineResults = dto.CombineResults,
                JuryWeight = dto.JuryWeight,
                PublicWeight = dto.PublicWeight,
                VotingSessions = dto.VotingSessions.Select(MapToData).ToList()
            };

            await _service.UpdateVotingTypeAsync(id, data);
            return Ok(new { message = "Tipos de votación actualizados." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static CreateVotingSessionData MapToData(CreateVotingSessionDto v) => new()
    {
        Name = v.Name,
        Description = v.Description,
        VoterType = Enum.Parse<VoterType>(v.VoterType, ignoreCase: true),
        EvaluationType = Enum.Parse<EvaluationType>(v.EvaluationType, ignoreCase: true),
        CriterionType = string.IsNullOrWhiteSpace(v.CriterionType)
            ? null
            : Enum.Parse<CriterionType>(v.CriterionType, ignoreCase: true),
        TopN = v.TopN,
        PointsPerVoter = v.PointsPerVoter,
        MaxPointsPerProject = v.MaxPointsPerProject,
        AllowComments = v.AllowComments,
        RequireComments = v.RequireComments,
        AllowCommentsPerCriterion = v.AllowCommentsPerCriterion,
        OpenAt = v.OpenAt,
        CloseAt = v.CloseAt,
        ReminderMinutesBeforeClose = v.ReminderMinutesBeforeClose,
        JurorEmails = v.JurorEmails,
        Criteria = v.Criteria.Select(c => new CreateCriterionData
        {
            Name = c.Name,
            Description = c.Description,
            Weight = c.Weight
        }).ToList(),
        Prizes = v.Prizes.Select(p => new CreatePrizeData
        {
            Position = p.Position,
            Name = p.Name,
            Description = p.Description
        }).ToList()
    };

    private static object ToDto(Category c) => new
    {
        c.Id,
        c.EventId,
        c.Name,
        c.Description,
        c.AllowSelfVoting,
        c.CombineResults,
        c.JuryWeight,
        c.PublicWeight,
        VotingSessions = c.VotingSessions.Select(vs => new
        {
            vs.Id,
            vs.Name,
            vs.Description,
            VoterType = vs.VoterType.ToString(),
            EvaluationType = vs.EvaluationType.ToString(),
            CriterionType = vs.CriterionType?.ToString(),
            vs.TopN,
            vs.PointsPerVoter,
            vs.MaxPointsPerProject,
            vs.AllowComments,
            vs.RequireComments,
            vs.AllowCommentsPerCriterion,
            vs.OpenAt,
            vs.CloseAt,
            JurorEmails = vs.JurorEmails,
            Criteria = vs.Criteria.Select(cr => new { cr.Id, cr.Name, cr.Weight, cr.Description }),
            Prizes = vs.Prizes.Select(p => new
            {
                p.Id,
                p.Position,
                p.Name,
                p.Description
            })
        })
    };

    private static object ToSimpleDto(Category c) => new
    {
        c.Id,
        c.Name,
        c.Description,
        c.AllowSelfVoting,
        TopNProjectsAllowed = c.VotingSessions
        .FirstOrDefault(vs => vs.EvaluationType == EvaluationType.TopN)?.TopN ?? 3,
        EvaluationType = c.VotingSessions.FirstOrDefault()?.EvaluationType.ToString()
    };
}

public class CreateCategoryDto
{
    public string EventId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool AllowSelfVoting { get; set; }
    public bool CombineResults { get; set; }
    public double? JuryWeight { get; set; }
    public double? PublicWeight { get; set; }
    public List<CreateVotingSessionDto> VotingSessions { get; set; } = new();
}

public class CreateVotingSessionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string VoterType { get; set; } = "Jury";
    public string EvaluationType { get; set; } = "TopN";
    public string? CriterionType { get; set; }
    public int? TopN { get; set; }
    public int? PointsPerVoter { get; set; }
    public int? MaxPointsPerProject { get; set; }
    public bool AllowComments { get; set; }
    public bool RequireComments { get; set; }
    public bool AllowCommentsPerCriterion { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public int? ReminderMinutesBeforeClose { get; set; }
    public List<CreateCriterionDto> Criteria { get; set; } = new();
    public List<CreatePrizeDto> Prizes { get; set; } = new();
    public List<string> JurorEmails { get; set; } = new();
}

public class CreateCriterionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Weight { get; set; }
}

public class CreatePrizeDto
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCategoryVotingDto
{
    public bool AllowSelfVoting { get; set; }
    public bool CombineResults { get; set; }
    public double? JuryWeight { get; set; }
    public double? PublicWeight { get; set; }
    public List<CreateVotingSessionDto> VotingSessions { get; set; } = new();
}