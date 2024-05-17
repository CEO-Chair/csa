using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace csa;

public class AssemblyInfoAnalyzer {

    public readonly InfoOptions Options;

    public AssemblyInfoAnalyzer(InfoOptions analysisOptions) {
        Options = analysisOptions;
    }

    public void Run() {
        if (!Path.Exists(Options.InputFilePath)) {
            throw new FileNotFoundException("Input file path is invalid", Options.InputFilePath);
        }

        PEFile? targetFile;

        try {
            using FileStream fileStream = new FileStream(Options.InputFilePath, FileMode.Open, FileAccess.Read);

            targetFile = new PEFile(Options.InputFilePath, fileStream, PEStreamOptions.PrefetchEntireImage);
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
        Console.WriteLine($"Assembly Path: {Path.GetFullPath(Options.InputFilePath)}");
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


        if (Options.ShowTypes) {
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

                if (Options.ShowFields) {
                    indent++;
                    foreach (IField field in type.GetFields()) {
                        Console.WriteLine($"{getIndent(indent)}{field.Type.FullName} {field.Name}");
                    }
                    indent--;
                    Console.WriteLine();
                }

                if (Options.ShowProperties) {
                    indent++;
                    foreach (IProperty prop in type.GetProperties()) {
                        Console.WriteLine($"{getIndent(indent)}{prop.ReturnType.FullName} {prop.Name}");
                    }
                    indent--;
                    Console.WriteLine();
                }

                if (Options.ShowMethods) {
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
}