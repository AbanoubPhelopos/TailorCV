using System.Reflection;
using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Domain.Users;
using TailorCV.Infrastructure.Database;
using Web.Api;

namespace TailorCV.Architecture.Tests;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(User).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
}
