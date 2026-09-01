using Microsoft.EntityFrameworkCore;
using Toeic.Application.Common.Dtos;
using Toeic.Application.Common.Exceptions; // ASSUMPTION: NotFoundAppException, ConflictAppException,
                                           // ValidationAppException(IReadOnlyList<string> errors) — same
                                           // namespace/shape assumed since Phase 1 Part 2 (see phase-tracker.md
                                           // "Open assumptions"). Adjust constructor calls below if the real
                                           // signatures differ.
using Toeic.Application.RubricManagement;
using Toeic.Application.RubricManagement.Dtos;
using Toeic.Domain.Entities;
using Toeic.Domain.Enums;
using Toeic.Domain.Rubrics;
using Toeic.Infrastructure.Persistence;

namespace Toeic.Infrastructure.RubricManagement;

public class RubricService : IRubricService
{
    private readonly AppDbContext _db;

    public RubricService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RubricSetDto> CreateRubricSetAsync(Guid createdByUserId, CreateRubricSetRequest request, CancellationToken ct)
    {
        var entity = new RubricSet
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            QuestionType = request.QuestionType,
            Description = request.Description,
            Version = request.Version,
            Status = RubricSetStatus.DRAFT,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.RubricSets.Add(entity);
        await _db.SaveChangesAsync(ct);

        entity.Rubrics = new List<Rubric>();
        return ToDto(entity);
    }

    public async Task<RubricSetDto> GetRubricSetAsync(Guid rubricSetId, CancellationToken ct)
    {
        var entity = await LoadAsync(rubricSetId, ct);
        return ToDto(entity);
    }

    public async Task<PagedResult<RubricSetDto>> GetRubricSetsAsync(
        QuestionType? questionType,
        RubricSetStatus? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.RubricSets.Include(r => r.Rubrics).AsQueryable();

        if (questionType is not null)
        {
            query = query.Where(r => r.QuestionType == questionType);
        }

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<RubricSetDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<RubricSetDto?> GetActiveRubricSetForQuestionTypeAsync(QuestionType questionType, CancellationToken ct)
    {
        var entity = await _db.RubricSets
            .Include(r => r.Rubrics)
            .Where(r => r.QuestionType == questionType && r.Status == RubricSetStatus.ACTIVE)
            .FirstOrDefaultAsync(ct);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<RubricSetDto> UpdateRubricSetAsync(Guid rubricSetId, UpdateRubricSetRequest request, CancellationToken ct)
    {
        var entity = await LoadAsync(rubricSetId, ct);
        EnsureDraft(entity);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task DeleteRubricSetAsync(Guid rubricSetId, CancellationToken ct)
    {
        var entity = await LoadAsync(rubricSetId, ct);
        EnsureDraft(entity);

        _db.RubricSets.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RubricSetDto> ActivateRubricSetAsync(Guid rubricSetId, CancellationToken ct)
    {
        var entity = await LoadAsync(rubricSetId, ct);

        if (entity.Status != RubricSetStatus.DRAFT)
        {
            throw new ConflictAppException("Only a DRAFT RubricSet can be activated.");
        }

        var errors = RubricSetValidation.Validate(entity.Rubrics.ToList());
        if (errors.Count > 0)
        {
            var errorDict = new Dictionary<string, string[]>
    {
        { "Rubrics", errors.ToArray() }
    };
            throw new ValidationAppException("Cấu hình RubricSet không hợp lệ.", errorDict);
        }


        //if (errors.Count > 0)
        //{
        //    throw new ValidationAppException(errors);
        //}

        // PHASE3-ISSUE-01: exactly one ACTIVE RubricSet per QuestionType at any time,
        // so the AI Assessment Service (Phase 3 Part 3) has a single, unambiguous rubric
        // to apply. Enforced here at the Application layer (defense in depth pattern
        // already used for ISSUE-06 in Phase 0); a DB partial unique index
        // `UNIQUE(QuestionType) WHERE Status = 'ACTIVE'` can be added as a follow-up
        // migration if you want the same guarantee under concurrent activations.
        var currentlyActive = await _db.RubricSets
            .Where(r => r.QuestionType == entity.QuestionType && r.Status == RubricSetStatus.ACTIVE)
            .ToListAsync(ct);

        foreach (var old in currentlyActive)
        {
            old.Status = RubricSetStatus.ARCHIVED;
            old.UpdatedAt = DateTime.UtcNow;
        }

        entity.Status = RubricSetStatus.ACTIVE;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<RubricSetDto> ArchiveRubricSetAsync(Guid rubricSetId, CancellationToken ct)
    {
        var entity = await LoadAsync(rubricSetId, ct);

        if (entity.Status != RubricSetStatus.ACTIVE)
        {
            throw new ConflictAppException("Only an ACTIVE RubricSet can be archived.");
        }

        entity.Status = RubricSetStatus.ARCHIVED;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<RubricDto> AddRubricAsync(Guid rubricSetId, Guid createdByUserId, CreateRubricRequest request, CancellationToken ct)
    {
        var set = await LoadAsync(rubricSetId, ct);
        EnsureDraft(set);

        var rubric = new Rubric
        {
            Id = Guid.NewGuid(),
            RubricSetId = rubricSetId,
            Criterion = request.Criterion,
            Level = request.Level,
            MinScore = request.MinScore,
            MaxScore = request.MaxScore,
            Description = request.Description,
            ScoringRule = request.ScoringRule,
            Weight = request.Weight,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Rubrics.Add(rubric);
        await _db.SaveChangesAsync(ct);

        return ToDto(rubric);
    }

    public async Task<RubricDto> UpdateRubricAsync(Guid rubricId, UpdateRubricRequest request, CancellationToken ct)
    {
        var rubric = await _db.Rubrics
            .Include(r => r.RubricSet)
            .FirstOrDefaultAsync(r => r.Id == rubricId, ct)
            ?? throw new NotFoundAppException($"Rubric '{rubricId}' not found.");

        EnsureDraft(rubric.RubricSet);

        rubric.Criterion = request.Criterion;
        rubric.Level = request.Level;
        rubric.MinScore = request.MinScore;
        rubric.MaxScore = request.MaxScore;
        rubric.Description = request.Description;
        rubric.ScoringRule = request.ScoringRule;
        rubric.Weight = request.Weight;
        rubric.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(rubric);
    }

    public async Task DeleteRubricAsync(Guid rubricId, CancellationToken ct)
    {
        var rubric = await _db.Rubrics
            .Include(r => r.RubricSet)
            .FirstOrDefaultAsync(r => r.Id == rubricId, ct)
            ?? throw new NotFoundAppException($"Rubric '{rubricId}' not found.");

        EnsureDraft(rubric.RubricSet);

        _db.Rubrics.Remove(rubric);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<RubricSet> LoadAsync(Guid rubricSetId, CancellationToken ct)
        => await _db.RubricSets
            .Include(r => r.Rubrics)
            .FirstOrDefaultAsync(r => r.Id == rubricSetId, ct)
            ?? throw new NotFoundAppException($"RubricSet '{rubricSetId}' not found.");

    private static void EnsureDraft(RubricSet set)
    {
        if (set.Status != RubricSetStatus.DRAFT)
        {
            throw new ConflictAppException(
                "RubricSet content (metadata or Rubric rows) can only be modified while it is DRAFT. " +
                "Archive/reactivate is not supported by design — create a new Version instead so past " +
                "assessments keep referencing the RubricSetId they were actually scored against.");
        }
    }

    private static RubricDto ToDto(Rubric r) => new()
    {
        RubricId = r.Id,
        RubricSetId = r.RubricSetId,
        Criterion = r.Criterion,
        Level = r.Level,
        MinScore = r.MinScore,
        MaxScore = r.MaxScore,
        Description = r.Description,
        ScoringRule = r.ScoringRule,
        Weight = r.Weight
    };

    private static RubricSetDto ToDto(RubricSet s) => new()
    {
        RubricSetId = s.Id,
        Name = s.Name,
        QuestionType = s.QuestionType,
        Description = s.Description,
        Version = s.Version,
        Status = s.Status,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        Rubrics = (s.Rubrics ?? new List<Rubric>())
            .OrderBy(r => r.Criterion)
            .ThenBy(r => r.Level)
            .Select(ToDto)
            .ToList()
    };
}
