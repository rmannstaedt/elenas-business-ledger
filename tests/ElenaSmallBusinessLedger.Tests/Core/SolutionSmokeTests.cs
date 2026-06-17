using CoreAssemblyMarker = MySmallBusinessLedger.Core.AssemblyMarker;
using InfrastructureAssemblyMarker = MySmallBusinessLedger.Infrastructure.AssemblyMarker;

namespace MySmallBusinessLedger.Tests.Core;

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
