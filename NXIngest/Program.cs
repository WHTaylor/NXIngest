using System;
using System.Linq;
using log4net.Config;

namespace NXIngest
{
    static class NXIngest
    {
        public static void Main(string[] args)
        {
            BasicConfigurator.Configure();
            var (nexus, mapping, output) = ParseArgs(args);
            new NxsIngestor().IngestNexusFile(nexus, mapping, output);
        }

        private static (string, string, string) ParseArgs(string[] args)
        {
            if (args.Contains("-h") || args.Contains("--help"))
            {
                PrintHelp();
                Environment.Exit(0);
            }
            else if (args.Length < 2)
            {
                Console.Error.WriteLine("Missing required argument");
                Console.WriteLine();
                PrintHelp();
                Environment.Exit(1);
            }

            return (args[0], args[1],
                args.Length > 2 ? args[2] : "default.xml");
        }

        private static void PrintHelp()
        {
            Console.WriteLine(
                "Usage: NXIngest.exe NEXUS_FILE MAPPING_FILE [OUTPUT_FILE]");
            Console.WriteLine("NEXUS_FILE\tPath to the nexus file to be ingested");
            Console.WriteLine("MAPPING_FILE\tPath to the mapping file to be used for the output structure");
            Console.WriteLine("OUTPUT_FILE\tOptional path to write the output file to. Defaults to 'default.xml'");
        }
    }
}
