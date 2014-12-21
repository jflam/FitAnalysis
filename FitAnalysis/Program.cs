using CommandLine;
using CommandLine.Text;
using FastFitParser.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                    var normalizedPowerCalculator = new NormalizedPowerCalculator();
                    var powerCurveCalculator = new PowerCurveCalculator(new int[] {1, 5, 10, 30, 60, 120, 240, 300, 600, 900});

                    var timer = new Stopwatch();
                    timer.Start();

                    foreach (var record in parser.GetDataRecords())
                    {
                        double power;

                        if (record.GlobalMessageNumber == GlobalMessageNumber.Lap)
                        {
                            var x = 42;

                        }
                        else if (record.GlobalMessageNumber == GlobalMessageNumber.Record)
                        {
                            if (record.TryGetField(FieldNumber.Power, out power))
                            {
                                powerCurveCalculator.Add(power);
                                normalizedPowerCalculator.Add(power);
                            }
                        }
                    }

                    timer.Stop();

                    Console.WriteLine("NP = {0:0}", normalizedPowerCalculator.NormalizedPower);
                    Console.WriteLine("Peak Power Intervals:\n");
                    for (int i = 0; i < powerCurveCalculator.Durations.Length; i++)
                    {
                        Console.WriteLine("Duration: {0}, Max Power: {1:0}", powerCurveCalculator.Durations[i], powerCurveCalculator.PeakAveragePowerForDuration[i]);
                    }

                    Console.WriteLine("Average power: {0:0}", powerCurveCalculator.AveragePower);
                    Console.WriteLine("Number of ms: {0}", timer.ElapsedMilliseconds);
                }
            }
        }
    }
}