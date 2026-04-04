using System.Reflection;
using TailorCV.Modules.Identity;
using TailorCV.Modules.Identity.Domain;
using TailorCV.SharedKernel;

namespace TailorCV.Modules.Identity.UnitTests;

public abstract class BaseTest
{
    protected static Assembly ApplicationAssembly => typeof(Application.DependencyInjection).Assembly;
    protected static Assembly DomainAssembly => typeof(Domain.Users.User).Assembly;
    protected static Assembly SharedKernelAssembly => typeof(Result).Assembly;
}