#pragma warning disable CA1308
using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Dto;

namespace TailorCV.CVGenerator.Worker.Infrastructure.Scoring;

public sealed class MatchScoreCalculator : IMatchScoreCalculator
{
    public MatchScoreData Calculate(ProfileSnapshotData profile, JobSnapshotData job)
    {
        HashSet<string> profileSkills = ExtractProfileSkills(profile);
        HashSet<string> jobSkills = job.RequiredSkills
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        HashSet<string> matching = new(profileSkills.Intersect(jobSkills));
        HashSet<string> missing = new(jobSkills.Except(profileSkills));

        int percentage = jobSkills.Count == 0
            ? 100
            : (int)Math.Round((double)matching.Count / jobSkills.Count * 100);

        return new MatchScoreData(
            Math.Clamp(percentage, 0, 100),
            matching.ToList(),
            missing.ToList());
    }

    private static HashSet<string> ExtractProfileSkills(ProfileSnapshotData profile)
    {
        HashSet<string> skills = new(StringComparer.OrdinalIgnoreCase);

        foreach (ProfileSectionSnapshot section in profile.Sections)
        {
            if (section.Type.Equals("skill", StringComparison.OrdinalIgnoreCase))
            {
                foreach (SectionItemSnapshot item in section.Items)
                {
                    if (item.SkillItems is not null)
                    {
                        foreach (string skill in item.SkillItems)
                        {
                            if (!string.IsNullOrWhiteSpace(skill))
                            {
                                skills.Add(skill.Trim().ToLowerInvariant());
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        skills.Add(item.Name.Trim().ToLowerInvariant());
                    }
                }
            }

            if (section.Type.Equals("experience", StringComparison.OrdinalIgnoreCase) ||
                section.Type.Equals("project", StringComparison.OrdinalIgnoreCase))
            {
                foreach (SectionItemSnapshot item in section.Items)
                {
                    if (item.TechStack is not null)
                    {
                        foreach (string tech in item.TechStack)
                        {
                            if (!string.IsNullOrWhiteSpace(tech))
                            {
                                skills.Add(tech.Trim().ToLowerInvariant());
                            }
                        }
                    }
                }
            }
        }

        return skills;
    }
}
