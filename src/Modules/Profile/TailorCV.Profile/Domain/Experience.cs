using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class Experience : Entity
{
    public Guid ProfileId { get; private set; }
    public string Company { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsCurrent { get; private set; }

    private Experience() { }

    public static Result<Experience> Create(
        Guid profileId,
        string company,
        string role,
        DateOnly startDate,
        DateOnly? endDate,
        string description,
        bool isCurrent)
    {
        if (string.IsNullOrWhiteSpace(company))
        {
            return Result<Experience>.Failure(Error.Validation("Company is required"));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return Result<Experience>.Failure(Error.Validation("Role is required"));
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            return Result<Experience>.Failure(Error.Validation("End date must be after start date"));
        }

        return Result<Experience>.Success(new Experience
        {
            ProfileId = profileId,
            Company = company.Trim(),
            Role = role.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Description = description ?? string.Empty,
            IsCurrent = isCurrent,
        });
    }

    public void Update(
        string company,
        string role,
        DateOnly startDate,
        DateOnly? endDate,
        string description,
        bool isCurrent)
    {
        Company = company.Trim();
        Role = role.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Description = description ?? string.Empty;
        IsCurrent = isCurrent;
    }
}
