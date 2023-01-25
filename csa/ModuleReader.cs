using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;

namespace csa;
public static class ModuleReader
{
    public static PEFile ReadModule(string path)
    {
        using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

        return new PEFile(path, fileStream, PEStreamOptions.PrefetchEntireImage);
    }
}
