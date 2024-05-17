using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using CommandLine;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

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
        if (!Path.Exists(options.InputFilePath)) {
            throw new FileNotFoundException("Input file path is invalid", options.InputFilePath);
        }

        PEFile? targetFile;

        try {
            using FileStream fileStream = new FileStream(options.InputFilePath, FileMode.Open, FileAccess.Read);

            targetFile = new PEFile(options.InputFilePath, fileStream, PEStreamOptions.PrefetchEntireImage);
        } catch (PEFileNotSupportedException e) {
            throw new BadImageFormatException("Target file is not a valid .NET Assembly", e);
        } catch (BadImageFormatException e) {
            throw new BadImageFormatException("Target file is not a valid .NET Assembly", e);
        }

        CorHeader? corHeader = targetFile.Reader.PEHeaders.CorHeader;

        string targetFramework = targetFile.DetectTargetFrameworkId();

        IAssemblyResolver assemblyResolver = new UniversalAssemblyResolver(targetFile.FileName, true, targetFramework);
        var typeSystem = new DecompilerTypeSystem(targetFile, assemblyResolver);
        var globalType = typeSystem.MainModule.TypeDefinitions.FirstOrDefault();

        Console.WriteLine($"--- Assembly Metadata ---");
        Console.WriteLine();
        Console.WriteLine($"Assembly Path: {Path.GetFullPath(options.InputFilePath)}");
        Console.WriteLine($"Assembly Name: {targetFile.FullName}");
        Console.WriteLine($".NET Version:  {targetFramework.Replace(",", ", ")}");
        Console.WriteLine($"Architecture:  {PEUtil.GetPlatformName(targetFile)}");
        if (globalType != null) {
            Console.WriteLine($"Global type:   {globalType.FullName}");
        }

        if (corHeader is not null) {
            var entrypointHandle = MetadataTokenHelpers.EntityHandleOrNil(corHeader.EntryPointTokenOrRelativeVirtualAddress);
            if (!entrypointHandle.IsNil && entrypointHandle.Kind == HandleKind.MethodDefinition) {
                var entrypoint = typeSystem.MainModule.ResolveMethod(entrypointHandle, default);
                if (entrypoint != null) {
                    Console.WriteLine($"Entry point:   {entrypoint.DeclaringType.FullName}.{entrypoint.Name}");
                }
            }
        }


        if (options.ShowTypes) {
            Console.WriteLine();
            Console.WriteLine("--- Assembly Members --- ");

            static string getIndent(int indent) {
                return new string('\t', indent);
            }

            int indent = 0;

            foreach (ITypeDefinition type in typeSystem.GetAllTypeDefinitions().Where(x => x.ParentModule?.PEFile == targetFile)) {
                if (type.DeclaringType is not null) {
                    indent++;
                }

                Console.WriteLine($"{getIndent(indent)}{type.FullName}:");

                if (options.ShowFields) {
                    indent++;
                    foreach (IField field in type.GetFields()) {
                        Console.WriteLine($"{getIndent(indent)}{field.Type.FullName} {field.Name}");
                    }
                    indent--;
                    Console.WriteLine();
                }

                if (options.ShowProperties) {
                    indent++;
                    foreach (IProperty prop in type.GetProperties()) {
                        Console.WriteLine($"{getIndent(indent)}{prop.ReturnType.FullName} {prop.Name}");
                    }
                    indent--;
                    Console.WriteLine();
                }

                if (options.ShowMethods) {
                    indent++;
                    foreach (IMethod method in type.GetMethods()) {
                        Console.Write($"{getIndent(indent)}{method.ReturnType.FullName} {method.Name}(");
                        for (int i = 0; i < method.Parameters.Count; i++) {
                            IParameter param = method.Parameters[i];

                            Console.Write($"{param.Type.FullName} {param.Name}");
                            if (i + 1 < method.Parameters.Count) {
                                Console.Write(", ");
                            }
                        }
                        Console.WriteLine(")");
                    }
                    indent--;
                    Console.WriteLine();
                }

                if (type.DeclaringType is not null) {
                    indent--;
                }
            }
        }
    }

    private static void RunDecompile(DecompileOptions options) {
        if (!Path.Exists(options.InputFilePath)) {
            throw new FileNotFoundException("Input file path is invalid", options.InputFilePath);
        }

        PEFile? targetFile;

        try {
            using FileStream fileStream = new FileStream(options.InputFilePath, FileMode.Open, FileAccess.Read);

            targetFile = new PEFile(options.InputFilePath, fileStream, PEStreamOptions.PrefetchEntireImage);
        } catch (PEFileNotSupportedException e) {
            throw new Exception("Target file is not a valid .NET Assembly", e);
        } catch (BadImageFormatException e) {
            throw new Exception("Target file is not a valid .NET Assembly", e);
        }

        if (options.OutputDirectory is null) {
            options.OutputDirectory = targetFile.Name;
        }

        options.OutputDirectory = Path.GetFullPath(options.OutputDirectory);

        Directory.CreateDirectory(options.OutputDirectory);

        Console.WriteLine($"Decompiling {targetFile.Name} into {options.OutputDirectory}");

        IAssemblyResolver assemblyResolver = new UniversalAssemblyResolver(targetFile.FileName, true, targetFile.DetectTargetFrameworkId());

        WholeProjectDecompiler projectDecompiler = new WholeProjectDecompiler(assemblyResolver) {
            MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
        };

        projectDecompiler.Settings.UseNestedDirectoriesForNamespaces = true;
        projectDecompiler.Settings.UseSdkStyleProjectFormat = true;
        projectDecompiler.Settings.CSharpFormattingOptions = FormattingOptionsFactory.CreateAllman();

        projectDecompiler.ProgressIndicator = new ConsoleProgressReporter();

        projectDecompiler.DecompileProject(targetFile, options.OutputDirectory);
    }

    sealed class ConsoleProgressReporter : IProgress<DecompilationProgress> {
        private string? Title;

        private bool Updating;

        private float LastPercent;

        public void Report(DecompilationProgress value) {
            if (Updating) {
                return;
            }

            Updating = true;

            if (value.Title != Title) {
                Title = value.Title;

                Console.WriteLine();
                Console.WriteLine($"-- {Title} --");
                Console.WriteLine($"0%  Completed");
            }

            float percent = value.UnitsCompleted / (float)value.TotalUnits;

            // Only print percentage once it has changed by > 1%
            if (MathF.Abs(LastPercent - percent) > 0.01f) {
                Console.WriteLine($"{$"{percent * 100f:F0}%",-3} Completed");
                LastPercent = percent;
            }

            Updating = false;
        }
    }

}