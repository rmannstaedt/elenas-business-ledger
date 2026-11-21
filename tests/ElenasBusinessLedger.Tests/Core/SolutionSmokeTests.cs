using CoreAssemblyMarker = ElenasBusinessLedger.Core.AssemblyMarker;
using InfrastructureAssemblyMarker = ElenasBusinessLedger.Infrastructure.AssemblyMarker;

namespace ElenasBusinessLedger.Tests.Core;

public sealed class SolutionSmokeTests
{
    [Fact]
    public void TestProjectCanReferenceCoreAssembly()
    {
        var marker = new CoreAssemblyMarker();

        Assert.NotNull(marker);
    }

    [Fact]
    public void TestProjectCanReferenceInfrastructureAssembly()
    {
        var marker = new InfrastructureAssemblyMarker();

        Assert.NotNull(marker);
    }
}
