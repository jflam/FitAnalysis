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
        //const string FIT_FILE_PATH = @"C:\Users\John\OneDrive\Garmin\2014-10-10-14-16-04.fit";
        const string FIT_FILE_PATH = @"C:\Users\John\OneDrive\Garmin\2014-12-25-12-27-18.fit";
        const double FTP = 225;

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

                    var normalizedPowerCalculator = new PowerStatisticsCalculator(FTP);
                    var powerCurveCalculator = new PowerCurveCalculator(new int[] {1, 5, 10, 30, 60, 120, 240, 300, 600, 900});
                    var heartRateVarianceCalculator = new HeartRateVarianceCalculator(new int[] { 600, 1200, 2400, 3600 });

                    var timer = new Stopwatch();
                    timer.Start();

                    foreach (var record in parser.GetDataRecords())
                    {
                        double power, heartRate;

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

                            if (record.TryGetField(FieldNumber.HeartRate, out heartRate))
                            {
                                heartRateVarianceCalculator.Add(heartRate);
                            }
                        }
                    }

                    timer.Stop();

                    Console.WriteLine("Peak Average Power Curve:\n");
                    for (int i = 0; i < powerCurveCalculator.Durations.Length; i++)
                    {
                        Console.WriteLine("Duration: {0}s, Peak Average Power: {1:0}W", powerCurveCalculator.Durations[i], powerCurveCalculator.PeakAveragePowerForDuration[i]);
                    }

                    Console.WriteLine("Minimum Heart Rate Variance:\n");
                    for (int i = 0; i < heartRateVarianceCalculator.Durations.Length; i++)
                    {
                        Console.WriteLine("Duration: {0}s, Average Heart Rate: {1:0}bpm +/- {2:0.0}",
                            heartRateVarianceCalculator.Durations[i],
                            heartRateVarianceCalculator.MeanHeartRateForDuration[i],
                            Math.Sqrt(Math.Abs(heartRateVarianceCalculator.VarianceForDuration[i])));
                        
                    }
                    Console.WriteLine("Average Heart Rate: {0:0}bpm", heartRateVarianceCalculator.AverageHeartRate);

                    Console.WriteLine("Average power: {0:0}W", powerCurveCalculator.AveragePower);
                    Console.WriteLine("Normalized power: {0:0}W", normalizedPowerCalculator.NormalizedPower);
                    Console.WriteLine("Intensity factor: {0:0.000}", normalizedPowerCalculator.IntensityFactor);
                    Console.WriteLine("Training Stress Score: {0:0}", normalizedPowerCalculator.TrainingStressScore);
                    Console.WriteLine("Processing duration: {0}ms", timer.ElapsedMilliseconds);
                }
            }
        }
    }
}