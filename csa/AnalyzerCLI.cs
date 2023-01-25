using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csa;
public class AnalyzerCLI
{
    public readonly string[] Arguments;

    public string? InputPath { get; private set; }

    private readonly Dictionary<string, ArgumentType> ArgumentTypeNames = new Dictionary<string, ArgumentType>()
    {
        { "-h",             ArgumentType.Help },
        { "-?",             ArgumentType.Help },
        { "--help",         ArgumentType.Help },
        { "--usage",        ArgumentType.Help },

        { "-i",             ArgumentType.InputPath },
        { "--input",        ArgumentType.InputPath },
    };

    public AnalyzerCLI(string[] args)
    {
        Arguments = args;
    }

    public (bool, bool) Parse()
    {
        if (Arguments.Length == 0)
        {
            return (true, false);
        }

        for (int i = 0; i < Arguments.Length; i++)
        {
            string arg = Arguments[i];
            string? nextArgument = i + 1 < Arguments.Length ? Arguments[i + 1] : null;

            bool skipNextArgument = false;

            if (!ArgumentTypeNames.TryGetValue(arg, out ArgumentType argumentType))
            {
                argumentType = ArgumentType.None;
            }

            switch (argumentType)
            {
                case ArgumentType.Help:
                    if (Arguments.Length == 1)
                    {
                        PrintUsage();
                        return (false, false);
                    }
                    else
                    {
                        return (true, false);
                    }

                case ArgumentType.InputPath:
                    if (nextArgument is not null)
                    {
                        InputPath = nextArgument;

                        skipNextArgument = true;
                    }
                    else
                    {
                        Console.WriteLine($"No input path entered");
                        return (true, false);
                    }
                    break;

                default:
                    if (i == 0)
                    {
                        InputPath = Arguments.First();
                    }
                    else
                    {
                        Console.WriteLine($"\"{arg}\" is not a valid argument.\nTry ./csa --help");
                        return (true, false);
                    }
                    break;
            }

            if (skipNextArgument)
            {
                i++;
            }
        }

        return (false, true);
    }

    public void PrintUsage()
    {
        Dictionary<ArgumentType, List<string>> argumentTypeMultipleParameters = new Dictionary<ArgumentType, List<string>>();

        foreach ((string name, ArgumentType argumentType) in ArgumentTypeNames)
        {
            if (!argumentTypeMultipleParameters.TryGetValue(argumentType, out List<string>? list))
            {
                list = new List<string>();

                argumentTypeMultipleParameters.Add(argumentType, list);
            }

            list.Add(name);
        }

        foreach ((ArgumentType argumentType, List<string> names) in argumentTypeMultipleParameters.OrderByDescending(x => x.Key))
        {
            string combinedNames = string.Join(", ", names);

            Console.Write($"{combinedNames}: ");

            switch (argumentType) 
            {
                case ArgumentType.Help:
                    Console.WriteLine("Prints usage");
                    break;
                case ArgumentType.InputPath:
                    Console.WriteLine("Path to input assembly");
                    break;
            }
        }
    }

    public enum ArgumentType
    {
        None,
        Help,
        InputPath
    }
}
