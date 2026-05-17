using TailorCV.Shared.Results;

namespace TailorCV.Templates.Domain;

public static class TemplateErrors
{
    public static Error TemplateNotFound =>
        Error.NotFound("TEMPLATE_NOT_FOUND", "Template not found");
}
