using System.Reflection;

namespace csa.Tests;

public class TestInfo {
    [SetUp]
    public void Setup() {

    }

    [Test]
    public void TestGetInfoOnCurrentAssembly() {
        csa.Program.Main(["info", $"{Assembly.GetExecutingAssembly().Location}"]);
    }

    [Test]
    public void TestOnInvalidAssembly() {
        Assert.Throws<BadImageFormatException>(() => {
            // definetly not a valid assembly
            csa.Program.Main(["info", $"{Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "csa.Tests.runtimeconfig.json")}"]);
        });
    }
}