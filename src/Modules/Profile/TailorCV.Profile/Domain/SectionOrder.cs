using TailorCV.Profile.Domain.Enums;
using TailorCV.Shared.Primitives;

namespace TailorCV.Profile.Domain;

public class SectionOrder : Entity
{
    public Guid ProfileId { get; private set; }
    public SectionType SectionType { get; private set; }
    public Guid SectionId { get; private set; }
    public int Order { get; set; }

    private SectionOrder() { }

    public static SectionOrder Create(
        Guid profileId,
        SectionType sectionType,
        Guid sectionId,
        int order)
    {
        return new SectionOrder
        {
            ProfileId = profileId,
            SectionType = sectionType,
            SectionId = sectionId,
            Order = order,
        };
    }
}
