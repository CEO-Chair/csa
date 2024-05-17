using ICSharpCode.Decompiler.Metadata;
using System.Reflection.PortableExecutable;

namespace csa;

public class PEUtil {
    public static string GetPlatformName(PEFile targetFile) {
        PEHeaders headers = targetFile!.Reader.PEHeaders;
        Machine architecture = headers.CoffHeader.Machine;
        Characteristics characteristics = headers.CoffHeader.Characteristics;
        CorFlags? corflags = headers.CorHeader?.Flags;

        switch (architecture) {
            case Machine.I386:
                if ((corflags & CorFlags.Requires32Bit) != 0)
                    return "x86";

                if ((corflags & CorFlags.Prefers32Bit) != 0)
                    return "AnyCPU (32-bit preferred)";

                // According to ECMA-335, II.25.3.3.1 CorFlags.Requires32Bit and Characteristics.Bit32Machine must be in sync
                // for assemblies containing managed code. However, this is not true for C++/CLI assemblies.
                if ((corflags & CorFlags.ILOnly) == 0 && (characteristics & Characteristics.Bit32Machine) != 0)
                    return "x86";

                return "AnyCPU (64-bit preferred)";

            case Machine.Amd64:
                return "x86_64";

            case Machine.IA64:
                return "Itanium";

            default:
                return architecture.ToString();
        }
    }
}
