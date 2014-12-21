using CommandLine;
using CommandLine.Text;
using FastFitParser.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace FitAnalysis
{
    class Options
    {
        [Option('d', "dir", Required = false, HelpText = "Directory to process")]
        public string Directory { get; set; }

        [Option('f', "file", Required = false, HelpText = "File to process")]
        public string File { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public Options(string defaultFilePath)
        {
            File = defaultFilePath;
        }
    }

    class Program
    {
        const string FIT_FILE_PATH = @"C:\Users\John\OneDrive\Garmin\2014-10-10-14-16-04.fit";

        class LapSummary
        {

        }

        static void Main(string[] args)
        {
            var options = new Options(FIT_FILE_PATH);
            if (Parser.Default.ParseArguments(args, options))
            {
                using (var stream = File.OpenRead(options.File))
                {
                    var parser = new FastParser(stream);
                    var laps = new List<LapSummary>();

                    foreach (var record in parser.GetDataRecords())
                    {
                        if (record.GlobalMessageNumber == GlobalMessageNumber.Lap)
                        {
                            var x = 42;

                        }
                        else if (record.GlobalMessageNumber == GlobalMessageNumber.Record)
                        {
                            var y = 42;

                        }
                    }
                }
            }
            Console.WriteLine("Press ENTER");
            Console.ReadLine();
        }
    }
}