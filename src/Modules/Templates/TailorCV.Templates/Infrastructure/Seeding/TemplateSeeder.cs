using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TailorCV.Templates.Domain;

namespace TailorCV.Templates.Infrastructure.Seeding;

public static class TemplateSeeder
{
    public static async Task SeedAsync(TemplatesDbContext dbContext)
    {
        if (await dbContext.Templates.AnyAsync())
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<Template> templates =
        [
            Template.Create(
                "Clean Minimal",
                "A clean, minimalist template with excellent readability and generous whitespace",
                MinimalHtml,
                MinimalCss,
                string.Empty,
                "minimal",
                "modern",
                now),

            Template.Create(
                "Executive Professional",
                "A traditional professional layout ideal for corporate and executive roles",
                ProfessionalHtml,
                ProfessionalCss,
                string.Empty,
                "professional",
                "classic",
                now),

            Template.Create(
                "Bold Creative",
                "A modern creative template with strong visual hierarchy and color accents",
                CreativeHtml,
                CreativeCss,
                string.Empty,
                "creative",
                "bold",
                now),
        ];

        dbContext.Templates.AddRange(templates);
        await dbContext.SaveChangesAsync();
    }

    private const string MinimalHtml =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
        </head>
        <body>
          <div class="resume">
            <header class="resume-header">
              <h1 data-slot="name">{{name}}</h1>
              <p class="headline" data-slot="headline">{{headline}}</p>
              <p class="contact" data-slot="contact">{{email}} &bull; {{phone}} &bull; {{location}}</p>
            </header>

            <section class="resume-section" data-slot="summary">
              <h2>Summary</h2>
              <p>{{summary}}</p>
            </section>

            <section class="resume-section" data-slot="experience">
              <h2>Experience</h2>
              <div class="entry" data-slot-entry="experience">
                <div class="entry-header">
                  <strong class="entry-title">{{role}}</strong> <span class="entry-company">at {{company}}</span>
                  <span class="entry-date">{{startDate}} - {{endDate}}</span>
                </div>
                <p class="entry-description">{{description}}</p>
              </div>
            </section>

            <section class="resume-section" data-slot="skills">
              <h2>Skills</h2>
              <div class="skills-list" data-slot-items="skills">{{skill}}</div>
            </section>

            <section class="resume-section" data-slot="education">
              <h2>Education</h2>
              <div class="entry" data-slot-entry="education">
                <div class="entry-header">
                  <strong class="entry-title">{{degree}}</strong> <span class="entry-company">{{institution}}</span>
                  <span class="entry-date">{{startDate}} - {{endDate}}</span>
                </div>
              </div>
            </section>
          </div>
        </body>
        </html>
        """;

    private const string MinimalCss =
        """
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Inter', 'Helvetica Neue', Arial, sans-serif; color: #2d3436; line-height: 1.6; padding: 40px; }
        .resume { max-width: 800px; margin: 0 auto; }
        .resume-header { margin-bottom: 32px; border-bottom: 2px solid #636e72; padding-bottom: 16px; }
        .resume-header h1 { font-size: 32px; font-weight: 300; letter-spacing: 1px; color: #2d3436; }
        .headline { font-size: 16px; color: #636e72; margin-top: 4px; }
        .contact { font-size: 13px; color: #b2bec3; margin-top: 8px; }
        .resume-section { margin-bottom: 24px; }
        .resume-section h2 { font-size: 14px; text-transform: uppercase; letter-spacing: 2px; color: #636e72; margin-bottom: 12px; border-bottom: 1px solid #dfe6e9; padding-bottom: 4px; }
        .entry { margin-bottom: 16px; }
        .entry-header { display: flex; justify-content: space-between; align-items: baseline; }
        .entry-title { font-size: 15px; }
        .entry-company { color: #636e72; font-size: 14px; }
        .entry-date { font-size: 13px; color: #b2bec3; }
        .entry-description { font-size: 14px; margin-top: 6px; color: #636e72; }
        .skills-list { display: flex; flex-wrap: wrap; gap: 8px; }
        .skills-list span { background: #f5f6fa; padding: 4px 12px; border-radius: 4px; font-size: 13px; color: #2d3436; }
        """;

    private const string ProfessionalHtml =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
        </head>
        <body>
          <div class="resume">
            <header class="resume-header">
              <div class="header-content">
                <h1 data-slot="name">{{name}}</h1>
                <p class="headline" data-slot="headline">{{headline}}</p>
              </div>
              <div class="header-contact" data-slot="contact">
                <p>{{email}}</p>
                <p>{{phone}}</p>
                <p>{{location}}</p>
              </div>
            </header>

            <section class="resume-section" data-slot="summary">
              <h2>Professional Summary</h2>
              <p>{{summary}}</p>
            </section>

            <section class="resume-section" data-slot="experience">
              <h2>Professional Experience</h2>
              <div class="entry" data-slot-entry="experience">
                <div class="entry-header">
                  <div>
                    <strong class="entry-title">{{role}}</strong>
                    <span class="entry-company"> — {{company}}</span>
                  </div>
                  <span class="entry-date">{{startDate}} - {{endDate}}</span>
                </div>
                <p class="entry-description">{{description}}</p>
              </div>
            </section>

            <section class="resume-section" data-slot="skills">
              <h2>Core Competencies</h2>
              <div class="skills-list" data-slot-items="skills">{{skill}}</div>
            </section>

            <section class="resume-section" data-slot="education">
              <h2>Education</h2>
              <div class="entry" data-slot-entry="education">
                <div class="entry-header">
                  <div>
                    <strong class="entry-title">{{degree}}</strong>
                    <span class="entry-company"> — {{institution}}</span>
                  </div>
                  <span class="entry-date">{{startDate}} - {{endDate}}</span>
                </div>
              </div>
            </section>
          </div>
        </body>
        </html>
        """;

    private const string ProfessionalCss =
        """
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Georgia', 'Times New Roman', serif; color: #1a1a2e; line-height: 1.5; padding: 48px; }
        .resume { max-width: 800px; margin: 0 auto; }
        .resume-header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 3px double #1a1a2e; padding-bottom: 20px; margin-bottom: 28px; }
        .resume-header h1 { font-size: 28px; font-weight: 700; letter-spacing: 0.5px; }
        .headline { font-size: 15px; color: #4a4a6a; font-style: italic; margin-top: 4px; }
        .header-contact { text-align: right; font-size: 13px; color: #4a4a6a; }
        .header-contact p { margin-bottom: 2px; }
        .resume-section { margin-bottom: 24px; }
        .resume-section h2 { font-size: 15px; font-weight: 700; text-transform: uppercase; letter-spacing: 1.5px; color: #1a1a2e; margin-bottom: 12px; padding-bottom: 4px; border-bottom: 1px solid #1a1a2e; }
        .entry { margin-bottom: 16px; padding-left: 16px; border-left: 2px solid #e8e8f0; }
        .entry-header { display: flex; justify-content: space-between; }
        .entry-title { font-size: 15px; }
        .entry-company { color: #4a4a6a; font-size: 14px; }
        .entry-date { font-size: 12px; color: #7a7a9a; white-space: nowrap; }
        .entry-description { font-size: 14px; margin-top: 6px; color: #4a4a6a; }
        .skills-list { display: flex; flex-wrap: wrap; gap: 6px; }
        .skills-list span { border: 1px solid #1a1a2e; padding: 3px 10px; font-size: 12px; color: #1a1a2e; }
        """;

    private const string CreativeHtml =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
        </head>
        <body>
          <div class="resume">
            <aside class="resume-sidebar">
              <div class="avatar" data-slot="name">{{initials}}</div>
              <div class="sidebar-section" data-slot="contact">
                <p>{{email}}</p>
                <p>{{phone}}</p>
                <p>{{location}}</p>
              </div>
              <div class="sidebar-section" data-slot="skills">
                <h3>Skills</h3>
                <div class="skills-list" data-slot-items="skills">{{skill}}</div>
              </div>
              <div class="sidebar-section" data-slot="education">
                <h3>Education</h3>
                <div class="entry" data-slot-entry="education">
                  <strong>{{degree}}</strong>
                  <p>{{institution}}</p>
                  <p class="date">{{startDate}} - {{endDate}}</p>
                </div>
              </div>
            </aside>

            <main class="resume-main">
              <header class="main-header">
                <h1 data-slot="name">{{name}}</h1>
                <p class="headline" data-slot="headline">{{headline}}</p>
              </header>

              <section class="main-section" data-slot="summary">
                <h2>About Me</h2>
                <p>{{summary}}</p>
              </section>

              <section class="main-section" data-slot="experience">
                <h2>Experience</h2>
                <div class="entry" data-slot-entry="experience">
                  <div class="entry-header">
                    <strong>{{role}}</strong>
                    <span class="entry-company">{{company}}</span>
                  </div>
                  <span class="entry-date">{{startDate}} - {{endDate}}</span>
                  <p class="entry-description">{{description}}</p>
                </div>
              </section>
            </main>
          </div>
        </body>
        </html>
        """;

    private const string CreativeCss =
        """
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', 'Roboto', sans-serif; color: #2c3e50; line-height: 1.5; }
        .resume { display: flex; min-height: 100vh; max-width: 900px; margin: 0 auto; }
        .resume-sidebar { width: 280px; background: #6c5ce7; color: #fff; padding: 40px 24px; }
        .avatar { width: 80px; height: 80px; background: #a29bfe; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 28px; font-weight: 700; margin: 0 auto 20px; }
        .sidebar-section { margin-bottom: 24px; }
        .sidebar-section h3 { font-size: 13px; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 12px; opacity: 0.8; }
        .sidebar-section p { font-size: 13px; margin-bottom: 4px; opacity: 0.9; }
        .skills-list { display: flex; flex-wrap: wrap; gap: 6px; }
        .skills-list span { background: rgba(255,255,255,0.2); padding: 4px 10px; border-radius: 12px; font-size: 12px; }
        .entry { margin-bottom: 12px; }
        .entry strong { font-size: 13px; }
        .entry p { font-size: 12px; opacity: 0.9; }
        .entry .date { font-size: 11px; opacity: 0.7; }
        .resume-main { flex: 1; padding: 40px; }
        .main-header { margin-bottom: 32px; }
        .main-header h1 { font-size: 36px; font-weight: 700; color: #6c5ce7; }
        .headline { font-size: 18px; color: #636e72; margin-top: 4px; }
        .main-section { margin-bottom: 28px; }
        .main-section h2 { font-size: 18px; font-weight: 700; color: #6c5ce7; margin-bottom: 16px; padding-bottom: 8px; border-bottom: 3px solid #6c5ce7; }
        .entry { margin-bottom: 16px; }
        .entry-header { display: flex; gap: 8px; font-size: 15px; }
        .entry-header strong { color: #2c3e50; }
        .entry-company { color: #6c5ce7; }
        .entry-date { font-size: 12px; color: #b2bec3; display: block; margin: 4px 0; }
        .entry-description { font-size: 14px; color: #636e72; margin-top: 4px; }
        """;
}
