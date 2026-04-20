#pragma warning disable CA1054

using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class Project : Entity
{
    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string[] TechStack { get; private set; } = [];
    public string Role { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    private Project() { }

    public static Result<Project> Create(
        Guid profileId,
        string name,
        string description,
        string[] techStack,
        string role,
        string url,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Project>.Failure(Error.Validation("Project name is required"));
        }

        if (endDate.HasValue && startDate.HasValue && endDate.Value < startDate.Value)
        {
            return Result<Project>.Failure(Error.Validation("End date must be after start date"));
        }

        return Result<Project>.Success(new Project
        {
            ProfileId = profileId,
            Name = name.Trim(),
            Description = description ?? string.Empty,
            TechStack = techStack ?? [],
            Role = role ?? string.Empty,
            Url = url ?? string.Empty,
            StartDate = startDate,
            EndDate = endDate,
        });
    }

    public void Update(
        string name,
        string description,
        string[] techStack,
        string role,
        string url,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        Name = name.Trim();
        Description = description ?? string.Empty;
        TechStack = techStack ?? [];
        Role = role ?? string.Empty;
        Url = url ?? string.Empty;
        StartDate = startDate;
        EndDate = endDate;
    }
}

#pragma warning restore CA1054
