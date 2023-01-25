namespace csa;

public class Program
{
    static void Main(string[] args)
    {
        AnalyzerCLI analyzerInterface = new AnalyzerCLI(args);

        (bool parseErrored, bool analyze) = analyzerInterface.Parse();

        if (parseErrored)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("An error occured.");
            Console.ResetColor();
            Console.WriteLine("For usage, try ./csa --help");
        }

        if (analyze)
        {
            AssemblyAnalyzer assemblyAnalyzer = new AssemblyAnalyzer(analyzerInterface);

            assemblyAnalyzer.Analyze();
        }
    }
}
