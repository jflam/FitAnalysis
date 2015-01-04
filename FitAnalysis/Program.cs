using CommandLine;
using CommandLine.Text;
using FastFitParser.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FitAnalysis
{
    class Options
    {
        [Option('d', "dir", Required = false, HelpText = "Directory to process")]
        public string Directory { get; set; }

        [Option('f', "file", Required = false, HelpText = "File to process")]
        public string File { get; set; }

        [Option("dump", Required = false, HelpText = "Dump heart rate data")]
        public bool DumpHeartRateData { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public Options() { }

        public Options(string defaultFilePath)
        {
            File = defaultFilePath;
        }
    }

    class Program
    {
        //const string FIT_FILE_PATH = @"C:\Users\John\OneDrive\Garmin\2014-10-10-14-16-04.fit";
        const string FIT_FILE_PATH = @"C:\Users\John\OneDrive\Garmin\2014-12-30-14-41-39.fit";
        const double FTP = 225;

        class LapSummary
        {

        }

        static void ComputeEfficiencyFactorReport(Options options)
        {
            // Parameters
            double standardDeviationThreshold = 5;
            double meanHeartRateLimit = 174; // upper limit of Z2

            var files = Directory.GetFiles(options.Directory, "*.fit");

            var timer = new Stopwatch();
            timer.Start();

            var sb = new StringBuilder();

            foreach (var file in files)
            {
                using (var stream = File.OpenRead(file))
                {
                    var parser = new FastParser(stream);
                    var efficiencyFactorCalculator = new EfficiencyFactorCalculator(new int[] { 1200 }, standardDeviationThreshold);

                    foreach (var record in parser.GetDataRecords())
                    {
                        if (record.GlobalMessageNumber == GlobalMessageNumber.Record)
                        {
                            double power, heartRate;
                            bool hasPower = record.TryGetField(RecordDef.Power, out power);
                            bool hasHeartRate = record.TryGetField(RecordDef.HeartRate, out heartRate);
                            if (hasPower && hasHeartRate)
                            {
                                efficiencyFactorCalculator.Add(power, heartRate);
                            }
                        }
                        else if (record.IsStopTimerEvent())
                        {
                            efficiencyFactorCalculator.Reset();
                        }
                    }

                    if (!efficiencyFactorCalculator.HasData)
                    {
                        //Console.WriteLine("{0} has no HR data", Path.GetFileNameWithoutExtension(file));
                    }
                    else
                    {
                        for (int i = 0; i < efficiencyFactorCalculator.Durations.Length; i++)
                        {
                            if (efficiencyFactorCalculator.StandardDeviationForDuration[i] > 0 &&
                                efficiencyFactorCalculator.MeanHeartRateForDuration[i] < meanHeartRateLimit)
                            {
                                sb.AppendLine(String.Format("{0}, Duration {1}s, EF = {2:0.000}, NP = {3:0}, Avg HR = {4:0.0} +/- {5:0.0}",
                                    Path.GetFileNameWithoutExtension(file),
                                    efficiencyFactorCalculator.Durations[i],
                                    efficiencyFactorCalculator.EfficiencyFactorForDuration[i],
                                    efficiencyFactorCalculator.NormalizedPowerForDuration[i],
                                    efficiencyFactorCalculator.MeanHeartRateForDuration[i],
                                    efficiencyFactorCalculator.StandardDeviationForDuration[i]));
                            }
                        }
                    }
                }
            }

            timer.Stop();
            Console.WriteLine(sb);
            Console.WriteLine("Total compute time: {0}ms", timer.ElapsedMilliseconds);
        }

        private static void ComputeDetailedFileReport(Options options)
        {
            using (var stream = File.OpenRead(options.File))
            {
                var parser = new FastParser(stream);
                var laps = new List<LapSummary>();
                double minimumHeartRateStandardDeviation = 4;

                var normalizedPowerCalculator = new PowerStatisticsCalculator(FTP);
                var powerCurveCalculator = new PowerCurveCalculator(new int[] { 1, 5, 10, 30, 60, 120, 240, 300, 600, 900 });
                var normalizedPowerCurveCalculator = new NormalizedPowerCurveCalculator(new int[] { 60, 120, 240, 300, 600, 900 });
                var heartRateVarianceCalculator = new HeartRateVarianceCalculator(new int[] { 600, 1200, 2400, 3600 });
                var efficiencyFactorCalculator = new EfficiencyFactorCalculator(new int[] { 600, 1200, 2400 }, minimumHeartRateStandardDeviation);

                var timer = new Stopwatch();
                timer.Start();

                foreach (var record in parser.GetDataRecords())
                {
                    if (record.GlobalMessageNumber == GlobalMessageNumber.Record)
                    {
                        double power, heartRate;
                        bool hasPower, hasHeartRate;

                        if (hasPower = record.TryGetField(RecordDef.Power, out power))
                        {
                            powerCurveCalculator.Add(power);
                            normalizedPowerCalculator.Add(power);
                            normalizedPowerCurveCalculator.Add(power);
                        }

                        if (hasHeartRate = record.TryGetField(RecordDef.HeartRate, out heartRate))
                        {
                            heartRateVarianceCalculator.Add(heartRate);
                        }

                        if (hasPower && hasHeartRate)
                        {
                            efficiencyFactorCalculator.Add(power, heartRate);
                        }
                    }
                    else if (record.IsStopTimerEvent())
                    {
                        normalizedPowerCalculator.Reset();
                        powerCurveCalculator.Reset();
                        normalizedPowerCurveCalculator.Reset();
                        heartRateVarianceCalculator.Reset();
                        efficiencyFactorCalculator.Reset();
                    }
                    else if (record.GlobalMessageNumber == GlobalMessageNumber.Lap)
                    {
                        // Dump the IDs of the fields in the field definition message
                        var x = 42;
                    }
                }

                timer.Stop();

                Console.WriteLine("Peak Average Power Curve:\n");
                for (int i = 0; i < powerCurveCalculator.Durations.Length; i++)
                {
                    Console.WriteLine("Duration: {0}s, Peak Average Power: {1:0}W",
                        powerCurveCalculator.Durations[i],
                        powerCurveCalculator.PeakAveragePowerForDuration[i]);
                }
                Console.WriteLine("\n");

                Console.WriteLine("Peak Normalized Power Curve:\n");
                for (int i = 0; i < normalizedPowerCurveCalculator.Durations.Length; i++)
                {
                    Console.WriteLine("Duration: {0}s, Peak Normalized Power: {1:0}W",
                        normalizedPowerCurveCalculator.Durations[i],
                        normalizedPowerCurveCalculator.PeakNormalizedPowerForDuration[i]);
                }
                Console.WriteLine("\n");

                Console.WriteLine("Minimum Heart Rate Variance:\n");
                for (int i = 0; i < heartRateVarianceCalculator.Durations.Length; i++)
                {
                    Console.WriteLine("Duration: {0}s, Average Heart Rate: {1:0}bpm +/- {2:0.0}",
                        heartRateVarianceCalculator.Durations[i],
                        heartRateVarianceCalculator.MeanHeartRateForDuration[i],
                        heartRateVarianceCalculator.StandardDeviationForDuration[i]);
                }
                Console.WriteLine("\n");

                Console.WriteLine("Efficiency Factor:\n");
                for (int i = 0; i < efficiencyFactorCalculator.Durations.Length; i++)
                {
                    Console.WriteLine("Duration: {0}s, NP = {1:0}W, HR = {2:0.0}+/-{3:0.0}, EF = {4:0.000}",
                        efficiencyFactorCalculator.Durations[i],
                        efficiencyFactorCalculator.NormalizedPowerForDuration[i],
                        efficiencyFactorCalculator.MeanHeartRateForDuration[i],
                        efficiencyFactorCalculator.StandardDeviationForDuration[i],
                        efficiencyFactorCalculator.EfficiencyFactorForDuration[i]);
                }
                Console.WriteLine("\n");

                Console.WriteLine("Summary statistics:\n");
                Console.WriteLine("Average Heart Rate: {0:0}bpm", heartRateVarianceCalculator.AverageHeartRate);
                Console.WriteLine("Average power: {0:0}W", powerCurveCalculator.AveragePower);
                Console.WriteLine("Normalized power: {0:0}W", normalizedPowerCalculator.NormalizedPower);
                Console.WriteLine("Intensity factor: {0:0.000}", normalizedPowerCalculator.IntensityFactor);
                Console.WriteLine("Training Stress Score: {0:0}", normalizedPowerCalculator.TrainingStressScore);
                Console.WriteLine("Processing duration: {0}ms", timer.ElapsedMilliseconds);

                // Optional part to dump captured heart rate from efficiency calculator
                if (options.DumpHeartRateData)
                {
                    for (int i = 0; i < efficiencyFactorCalculator.StandardDeviationForDuration.Length; i++)
                    {
                        if (efficiencyFactorCalculator.StandardDeviationForDuration[i] > 0)
                        {
                            for (int j = 0; j < efficiencyFactorCalculator.CapturedHeartRateTraces[i].Length; j++)
                            {
                                Console.WriteLine(efficiencyFactorCalculator.CapturedHeartRateTraces[i][j]);
                            }
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                if (!String.IsNullOrEmpty(options.File))
                {
                    ComputeDetailedFileReport(options);
                }
                else
                {
                    ComputeEfficiencyFactorReport(options);
                }
            }
        }
    }
}