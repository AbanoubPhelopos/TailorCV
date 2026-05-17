using TailorCV.Profile.Domain;
using TailorCV.Profile.Features.Shared;

namespace TailorCV.Profile.Features.Shared;

public static class SectionDataExtensions
{
    public static SectionData ToSectionData(this ProfileSection section)
    {
        return section.Type switch
        {
            "experience" => new ExperienceSectionData(section.Id, section.Order,
                section.Items.Select(i => new ExperienceItem(
                    i.Id, i.Order, i.Company!, i.Role!, i.StartDate!.Value, i.EndDate, i.Description, i.IsCurrent)).ToList()),

            "project" => new ProjectSectionData(section.Id, section.Order,
                section.Items.Select(i => new ProjectItem(
                    i.Id, i.Order, i.Name!, i.Description, i.TechStack, i.Role, i.Url, i.StartDate, i.EndDate)).ToList()),

            "skill" => new SkillSectionData(section.Id, section.Order,
                section.Items.Select(i => new SkillItem(
                    i.Id, i.Order, i.Name!)).ToList()),

            "education" => new EducationSectionData(section.Id, section.Order,
                section.Items.Select(i => new EducationItem(
                    i.Id, i.Order, i.Institution!, i.Degree!, i.Field!, i.StartDate!.Value, i.EndDate, i.Gpa)).ToList()),

            "certification" => new CertificationSectionData(section.Id, section.Order,
                section.Items.Select(i => new CertificationItem(
                    i.Id, i.Order, i.Name!, i.Issuer!, i.Date!.Value, i.ExpiryDate, i.Url)).ToList()),

            "language" => new LanguageSectionData(section.Id, section.Order,
                section.Items.Select(i => new LanguageItem(
                    i.Id, i.Order, i.LanguageName!, i.Proficiency!)).ToList()),

            "custom" => new CustomSectionData(section.Id, section.Order,
                section.Title ?? section.Type,
                section.Items.Select(i => new CustomItem(
                    i.Id, i.Order, i.Title!, i.Subtitle,
                    i.Description?.Split('\n').ToList(), i.Url)).ToList()),

            _ => throw new InvalidOperationException($"Unknown section type: {section.Type}"),
        };
    }

    public static ProfileSection ToProfileSection(this SectionData data)
    {
        List<SectionItem> items = data switch
        {
            ExperienceSectionData d => d.Items.Select(i => new SectionItem
            {
                Id = i.Id, Order = i.Order, Company = i.Company, Role = i.Role,
                StartDate = i.StartDate, EndDate = i.EndDate, Description = i.Description, IsCurrent = i.IsCurrent,
            }).ToList(),

            ProjectSectionData d => d.Items.Select(i => new SectionItem
            {
                Id = i.Id, Order = i.Order, Name = i.Name, Description = i.Description,
                TechStack = i.TechStack, Role = i.Role, Url = i.Url, StartDate = i.StartDate, EndDate = i.EndDate,
            }).ToList(),

            SkillSectionData d => d.Items.Select(i => new SectionItem
            {
                Id = i.Id, Order = i.Order, Name = i.Name,
            }).ToList(),

            EducationSectionData d => d.Items.Select(i => new SectionItem
            {
                Id = i.Id, Order = i.Order, Institution = i.Institution, Degree = i.Degree,
                Field = i.Field, StartDate = i.StartDate, EndDate = i.EndDate, Gpa = i.Gpa,
            }).ToList(),

            CertificationSectionData d => d.Items.Select(i => new SectionItem
            {
                Id = i.Id, Order = i.Order, Name = i.Name, Issuer = i.Issuer,
                Date = i.Date, ExpiryDate = i.ExpiryDate, Url = i.Url,
            }).ToList(),

            LanguageSectionData d => d.Items.Select(i => new SectionItem
            {
                Id = i.Id, Order = i.Order, LanguageName = i.LanguageName, Proficiency = i.Proficiency,
            }).ToList(),

            CustomSectionData d => d.Items.Select(i => new SectionItem
            {
                Id = i.Id, Order = i.Order, Title = i.Title, Subtitle = i.Subtitle,
                Description = i.Description != null ? string.Join("\n", i.Description) : null, Url = i.Url,
            }).ToList(),

            _ => throw new InvalidOperationException($"Unknown section type: {data.GetType().Name}"),
        };

        return new ProfileSection
        {
            Id = data.Id,
            Type = data switch
            {
                ExperienceSectionData => "experience",
                ProjectSectionData => "project",
                SkillSectionData => "skill",
                EducationSectionData => "education",
                CertificationSectionData => "certification",
                LanguageSectionData => "language",
                CustomSectionData => "custom",
                _ => throw new InvalidOperationException($"Unknown section type: {data.GetType().Name}"),
            },
            Order = data.Order,
            Title = data is CustomSectionData csd ? csd.Title : null,
            Items = items,
        };
    }

    public static List<SectionData> ToSectionDataList(this List<ProfileSection> sections)
    {
        return sections.OrderBy(s => s.Order).Select(ToSectionData).ToList();
    }

    public static List<ProfileSection> ToProfileSectionList(this List<SectionData> sections)
    {
        return sections.Select(ToProfileSection).ToList();
    }
}
