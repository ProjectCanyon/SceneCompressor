using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace SceneCompressor.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var defaultConsoleColor = Console.ForegroundColor;
            var errorConsoleColor = ConsoleColor.Red;

            var app = new CommandLineApplication
            {
                FullName = "ProjectCanyon's VAM Scene Compressor",
                Description = "This tool will compress and smooth VaM scene animations using linear interpolation.",
                LongVersionGetter = () => "Version 1.0.1",
                ShortVersionGetter = () => "v1.0.1",
            };

            var sourceArg = app.Argument("source", @"Source, C:\VaM\Saves\scene\scene.json");

            var targetOption = app.Option("-t|--target",
                @"Target, C:\VaM\Saves\scene\scene-compressed.json",
                CommandOptionType.SingleValue);

            var passesOption = app.Option("-p|--passes",
                @"Compression passes, each pass will half the number of steps",
                CommandOptionType.SingleValue);

            var forceOption = app.Option("-f|--force",
                @"Force compression, force already compressed steps to processed",
                CommandOptionType.SingleValue);

            var verboseOption = app.Option("-v|--verbose",
                @"Verbose output",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(sourceArg.Value))
                {
                    app.ShowHelp();
                    return 0;
                }

                var sourceFileInfo = new FileInfo(sourceArg.Value);
                var targetFileInfo = new FileInfo(!targetOption.HasValue() ? sourceFileInfo.FullName.Replace(".json", "-compressed.json") : targetOption.Value());

                if (string.Equals(sourceFileInfo.FullName, targetFileInfo.FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    app.ShowHelp();
                    Console.ForegroundColor = errorConsoleColor;
                    Console.WriteLine("*** Cannot write to source scene, please supply a different target. I don't want any tears ;) ***");
                    Console.ForegroundColor = defaultConsoleColor;
                    return -1;
                }

                int passes = 2;
                if (!passesOption.HasValue()
                    && int.TryParse(passesOption.Value(), out int parsedPasses)
                    && parsedPasses > 0)
                {
                    passes = parsedPasses;
                }

                if (!forceOption.HasValue() || !bool.TryParse(forceOption.Value(), out bool force))
                    force = false;

                if (!verboseOption.HasValue() || !bool.TryParse(verboseOption.Value(), out bool verbose))
                    verbose = false;

                app.ShowVersion();

                var compressorOptions = new CompressionOptions
                {
                    Source = sourceFileInfo,
                    Target = targetFileInfo,
                    Passes = passes,
                    Verbose = verbose,
                    Force =  force
                };

                var compressor = new SceneCompressor(compressorOptions);

                compressor.Compress();

                return 0;
            });

            app.HelpOption("-? | -h | --help");
            app.Execute(args);

#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}
