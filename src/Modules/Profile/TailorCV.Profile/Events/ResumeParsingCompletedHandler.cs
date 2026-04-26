using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Contracts.Events;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.Interfaces;
using Contract = TailorCV.Profile.Contracts.Dto;

namespace TailorCV.Profile.Events;

public static class ResumeParsingCompletedHandler
{
    public static async Task HandleAsync(
        ResumeParsingCompleted @event,
        ProfileDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        CancellationToken ct)
    {
        ParseJob? parseJob = await dbContext.ParseJobs
            .FirstOrDefaultAsync(p => p.Id == @event.ParseJobId, ct);

        if (parseJob is null)
        {
            return;
        }

        Contract.ParsedResumeData dto = @event.Data;

        ParsedResumeData domainData = new()
        {
            Headline = dto.Headline,
            Summary = dto.Summary,
            Phone = dto.Phone,
            Location = dto.Location,
            Website = dto.Website,
            Linkedin = dto.Linkedin,
            Github = dto.Github,
            Sections = dto.Sections?.Select(s => new ParsedSection
            {
                Type = s.Type,
                Order = s.Order,
                Items = s.Items.Select(i => new ParsedItem
                {
                    Order = i.Order,
                    Company = i.Company, Role = i.Role, Description = i.Description,
                    IsCurrent = i.IsCurrent, StartDate = i.StartDate, EndDate = i.EndDate,
                    Name = i.Name, TechStack = i.TechStack, Url = i.Url,
                    Category = i.Category, SkillItems = i.SkillItems,
                    Institution = i.Institution, Degree = i.Degree, Field = i.Field, Gpa = i.Gpa,
                    Issuer = i.Issuer, Date = i.Date, ExpiryDate = i.ExpiryDate,
                    LanguageName = i.LanguageName, Proficiency = i.Proficiency,
                    Title = i.Title, Subtitle = i.Subtitle,
                }).ToList(),
            }).ToList() ?? [],
        };

        parseJob.MarkDone(domainData, dateTimeProvider.UtcNow);
        await dbContext.SaveChangesAsync(ct);
    }
}
