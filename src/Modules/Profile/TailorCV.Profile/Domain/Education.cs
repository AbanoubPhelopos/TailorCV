using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class Education : Entity
{
    public Guid ProfileId { get; private set; }
    public string Institution { get; private set; } = string.Empty;
    public string Degree { get; private set; } = string.Empty;
    public string Field { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string Gpa { get; private set; } = string.Empty;

    private Education() { }

    public static Result<Education> Create(
        Guid profileId,
        string institution,
        string degree,
        string field,
        DateOnly startDate,
        DateOnly? endDate,
        string gpa)
    {
        if (string.IsNullOrWhiteSpace(institution))
        {
            return Result<Education>.Failure(Error.Validation("Institution is required"));
        }

        if (string.IsNullOrWhiteSpace(degree))
        {
            return Result<Education>.Failure(Error.Validation("Degree is required"));
        }

        if (string.IsNullOrWhiteSpace(field))
        {
            return Result<Education>.Failure(Error.Validation("Field is required"));
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            return Result<Education>.Failure(Error.Validation("End date must be after start date"));
        }

        return Result<Education>.Success(new Education
        {
            ProfileId = profileId,
            Institution = institution.Trim(),
            Degree = degree.Trim(),
            Field = field.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Gpa = gpa ?? string.Empty,
        });
    }

    public void Update(
        string institution,
        string degree,
        string field,
        DateOnly startDate,
        DateOnly? endDate,
        string gpa)
    {
        Institution = institution.Trim();
        Degree = degree.Trim();
        Field = field.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Gpa = gpa ?? string.Empty;
    }
}
