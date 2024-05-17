using CommandLine;

namespace csa;

[Verb("info", isDefault: false, HelpText = "Get basic info about an assembly")]
public class InfoOptions {
    [Value(0, MetaName = "input", Required = true, HelpText = "File path to assembly")]
    public string InputFilePath { get; set; } = "";

    [Option("types", Default = false, SetName = "members")]
    public bool ShowTypes { get; set; }

    [Option("methods", Default = false, SetName = "members")]
    public bool ShowMethods { get; set; }

    [Option("fields", Default = false, SetName = "members")]
    public bool ShowFields { get; set; }

    [Option("props", Default = false, SetName = "members")]
    public bool ShowProperties { get; set; }
}

[Verb("decompile", isDefault: false, HelpText = "Decompile an assembly")]
public class DecompileOptions {
    [Value(0, MetaName = "input", Required = true, HelpText = "File path to assembly")]
    public string InputFilePath { get; set; } = "";

    [Option("output", Default = null, HelpText = "Output directory for decompiled project")]
    public string? OutputDirectory { get; set; }
}

public class Program {
    public static void Main(string[] args) {
        Parser.Default.ParseArguments<InfoOptions, DecompileOptions>(args)
            .WithParsed<InfoOptions>(options => RunInfo(options))
            .WithParsed<DecompileOptions>(options => RunDecompile(options));
    }

    private static void RunInfo(InfoOptions options) {
        AssemblyInfoAnalyzer analyzer = new AssemblyInfoAnalyzer(options);

        analyzer.Run();
    }

    private static void RunDecompile(DecompileOptions options) {
        AssemblyDecompiler decompiler = new AssemblyDecompiler(options);

        decompiler.Run();
    }
}
