using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

namespace csa;

public class AssemblyDecompiler {

    public readonly DecompileOptions Options;

    public AssemblyDecompiler(DecompileOptions options) {
        Options = options;
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
            throw new Exception("Target file is not a valid .NET Assembly", e);
        } catch (BadImageFormatException e) {
            throw new Exception("Target file is not a valid .NET Assembly", e);
        }

        if (Options.OutputDirectory is null) {
            Options.OutputDirectory = targetFile.Name;
        }

        Options.OutputDirectory = Path.GetFullPath(Options.OutputDirectory);

        Directory.CreateDirectory(Options.OutputDirectory);

        Console.WriteLine($"Decompiling {targetFile.Name} into {Options.OutputDirectory}");

        IAssemblyResolver assemblyResolver = new UniversalAssemblyResolver(targetFile.FileName, true, targetFile.DetectTargetFrameworkId());

        WholeProjectDecompiler projectDecompiler = new WholeProjectDecompiler(assemblyResolver);

        projectDecompiler.Settings.UseNestedDirectoriesForNamespaces = true;
        projectDecompiler.Settings.UseSdkStyleProjectFormat = true;
        projectDecompiler.Settings.RemoveDeadCode = true;
        // Don't work properly and don't support expressions
        projectDecompiler.Settings.SwitchExpressions = false;
        
        projectDecompiler.Settings.CSharpFormattingOptions = Options.FormattingStyle switch {
            FormattingStyle.Patch => CreatePatchFormat(),
            FormattingStyle.KRStyle => FormattingOptionsFactory.CreateKRStyle(),
            FormattingStyle.Allman => FormattingOptionsFactory.CreateAllman(),
            FormattingStyle.Empty => FormattingOptionsFactory.CreateEmpty(),
            FormattingStyle.GNU => FormattingOptionsFactory.CreateGNU(),
            FormattingStyle.Mono => FormattingOptionsFactory.CreateMono(),
            FormattingStyle.SharpDevelop => FormattingOptionsFactory.CreateSharpDevelop(),
            FormattingStyle.Whitesmiths => FormattingOptionsFactory.CreateWhitesmiths(),
            _ => FormattingOptionsFactory.CreateKRStyle()
        };

        projectDecompiler.ProgressIndicator = new ConsoleProgressReporter();

        projectDecompiler.DecompileProject(targetFile, Options.OutputDirectory);
    }

    private static CSharpFormattingOptions CreatePatchFormat() {
        CSharpFormattingOptions format = FormattingOptionsFactory.CreateKRStyle();

        format.ClassBraceStyle = BraceStyle.EndOfLine;
        format.ConstructorBraceStyle = BraceStyle.EndOfLine;
        format.DestructorBraceStyle = BraceStyle.EndOfLine;
        format.EnumBraceStyle = BraceStyle.EndOfLine;
        format.EventBraceStyle = BraceStyle.EndOfLine;
        format.InterfaceBraceStyle = BraceStyle.EndOfLine;
        format.MethodBraceStyle = BraceStyle.EndOfLine;
        format.PropertyBraceStyle = BraceStyle.EndOfLine;
        format.StatementBraceStyle = BraceStyle.EndOfLine;
        format.StructBraceStyle = BraceStyle.EndOfLine;
        format.AnonymousMethodBraceStyle = BraceStyle.EndOfLine;
        format.EventAddBraceStyle = BraceStyle.EndOfLine;
        format.EventRemoveBraceStyle = BraceStyle.EndOfLine;
        format.PropertyGetBraceStyle = BraceStyle.EndOfLine;
        format.PropertySetBraceStyle = BraceStyle.EndOfLine;
    
        // Arrays should have a new line for every entry, since it's easier to insert values in patches that way
        format.ArrayInitializerWrapping = Wrapping.WrapAlways;
        format.ArrayInitializerBraceStyle = BraceStyle.EndOfLine;

        format.MinimumBlankLinesBetweenMembers = 1;

        return format;
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