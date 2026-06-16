using CoreAssemblyMarker = ElenaSmallBusinessLedger.Core.AssemblyMarker;
using InfrastructureAssemblyMarker = ElenaSmallBusinessLedger.Infrastructure.AssemblyMarker;

namespace ElenaSmallBusinessLedger.Tests.Core;

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
