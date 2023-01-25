using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace csa;
public class AssemblyAnalyzer
{
    public readonly AnalyzerCLI AnalyzerCLI;

    public string InputPath
    {
        get
        {
            if (File.Exists(AnalyzerCLI.InputPath))
            {
                return AnalyzerCLI.InputPath;
            }
            else
            {
                string fullPath = Path.GetFullPath(AnalyzerCLI.InputPath!);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return AnalyzerCLI.InputPath!;
        }
    }

    private PEFile? TargetFile;

    public AssemblyAnalyzer(AnalyzerCLI analyzerCLI)
    {
        AnalyzerCLI = analyzerCLI;
    }

    public void Analyze()
    {
        if (!Path.Exists(InputPath))
            throw new FileNotFoundException("InputPath is invalid", InputPath);

        TargetFile = ModuleReader.ReadModule(InputPath);

        if (!TargetFile.IsAssembly)
        {
            Console.WriteLine("Target file is not a valid .NET binary.");
            return;
        }

        PrintTargetFileInformation();
    }

    private void PrintTargetFileInformation()
    {
        if (TargetFile is null)
            return;

        Console.WriteLine($"{InputPath}");
        Console.WriteLine($"{TargetFile.FullName}");
        Console.WriteLine($".NET Version: {TargetFile.DetectTargetFrameworkId().Replace(",", ", ")}");
        Console.WriteLine($"Architecture: {GetPlatformName()}");
    }

    public string GetPlatformName()
    {
        PEHeaders headers = TargetFile!.Reader.PEHeaders;
        Machine architecture = headers.CoffHeader.Machine;
        Characteristics characteristics = headers.CoffHeader.Characteristics;
        CorFlags? corflags = headers.CorHeader?.Flags;

        switch (architecture)
        {
            case Machine.I386:
                if ((corflags & CorFlags.Prefers32Bit) != 0)
                    return "AnyCPU";

                if ((corflags & CorFlags.Requires32Bit) != 0)
                    return "x86";

                // According to ECMA-335, II.25.3.3.1 CorFlags.Requires32Bit and Characteristics.Bit32Machine must be in sync
                // for assemblies containing managed code. However, this is not true for C++/CLI assemblies.
                if ((corflags & CorFlags.ILOnly) == 0 && (characteristics & Characteristics.Bit32Machine) != 0)
                    return "x86";
                return "AnyCPU";

            case Machine.Amd64:
                return "x64";

            case Machine.IA64:
                return "Itanium";

            default:
                return architecture.ToString();
        }
    }
}
