using System;
using System.Linq;

namespace FitAnalysis
{
    class PowerStatisticsCalculator
    {
        const int DURATION = 30;

        private CircularBuffer _buffer;
        private double _power30SecondTotal;
        private double _power30SecondAverageToFourthPowerTotal;
        private int _totalCount;
        private double _ftp;

        public PowerStatisticsCalculator(double ftp)
        {
            _buffer = new CircularBuffer(DURATION + 1);
            _ftp = ftp;
        }

        public void Add(double power)
        {
            _power30SecondTotal -= _buffer.ReadNegativeOffset(DURATION - 1);
            _power30SecondTotal += power;
            _buffer.Add(power);

            if (_totalCount >= DURATION)
            {
                double power30SecondAverage = _power30SecondTotal / 30.0;
                _power30SecondAverageToFourthPowerTotal += Math.Pow(power30SecondAverage, 4);
            }

            _totalCount++;
        }

        public void Reset()
        {
            // Reset for this algorithm is a no-op, as it's intended to capture aggregate statistics
        }

        public double NormalizedPower
        {
            get { return Math.Pow(_power30SecondAverageToFourthPowerTotal / ((double)(_totalCount - DURATION)), 0.25); }
        }

        public double IntensityFactor
        {
            get { return NormalizedPower / _ftp; }
        }

        public double TrainingStressScore
        {
            get { return (_totalCount * NormalizedPower * IntensityFactor) / (_ftp * 3600) * 100; }
        }
    }

    class NormalizedPowerCurveCalculator
    {
        const int DURATION = 30;

        private int[] _durations;
        private CircularBuffer _average30SecondPowerBuffer;
        private CircularBuffer _normalizedPowerBuffer;
        private double[] _powerToFourthPowerTotalForDuration;
        private double[] _peakNormalizedPowerForDuration;
        private int[] _peakNormalizedPowerForDurationOffset;
        private double _power30SecondTotal;
        private int _count;

        public NormalizedPowerCurveCalculator(int[] durations)
        {
            _durations = durations;

            _average30SecondPowerBuffer = new CircularBuffer(DURATION + 1);
            _powerToFourthPowerTotalForDuration = new double[durations.Length];
            _peakNormalizedPowerForDuration = new double[durations.Length];
            _peakNormalizedPowerForDurationOffset = new int[durations.Length];

            int longestInterval = durations.Max();
            _normalizedPowerBuffer = new CircularBuffer(longestInterval + 1);
        }

        public void Add(double power)
        {
            _power30SecondTotal -= _average30SecondPowerBuffer.ReadNegativeOffset(DURATION - 1);
            _power30SecondTotal += power;
            _average30SecondPowerBuffer.Add(power);
            _count++;

            if (_count > DURATION)
            {
                double power30SecondAverageToFourthPower = Math.Pow(_power30SecondTotal / DURATION, 4);

                for (int i = 0; i < _durations.Length; i++)
                {
                    _powerToFourthPowerTotalForDuration[i] -= _normalizedPowerBuffer.ReadNegativeOffset(_durations[i] - 1);
                    _powerToFourthPowerTotalForDuration[i] += power30SecondAverageToFourthPower;

                    if (_count >= _durations[i])
                    {
                        double normalizedPower = Math.Pow(_powerToFourthPowerTotalForDuration[i] / _durations[i], 0.25);
                        if (normalizedPower > _peakNormalizedPowerForDuration[i])
                        {
                            _peakNormalizedPowerForDuration[i] = normalizedPower;
                            _peakNormalizedPowerForDurationOffset[i] = _count - _durations[i];
                        }
                    }
                }

                _normalizedPowerBuffer.Add(power30SecondAverageToFourthPower);
            }
        }

        public void Reset()
        {
            _count = 0;
            _power30SecondTotal = 0;
            _average30SecondPowerBuffer.Reset();
            _normalizedPowerBuffer.Reset();

            for (int i = 0; i < _durations.Length; i++)
            {
                _powerToFourthPowerTotalForDuration[i] = 0;
            }
        }

        public double[] PeakNormalizedPowerForDuration
        {
            get { return _peakNormalizedPowerForDuration; }
        }

        public int[] PeakNormalizedPowerForDurationOffset
        {
            get { return _peakNormalizedPowerForDurationOffset; }
        }

        public int[] Durations
        {
            get { return _durations; }
        }
    }

    class PowerCurveCalculator
    {
        private double[] _peakAveragePowerForDuration;
        private int[] _peakAveragePowerForDurationOffset;
        private double[] _peakTotalPowerForDuration;
        private int[] _durations;
        private CircularBuffer _buffer;
        private double _totalPower;
        private int _durationCount;
        private int _totalCount;

        public PowerCurveCalculator(int[] durations)
        {
            _durations = durations;
            _peakAveragePowerForDuration = new double[durations.Length];
            _peakAveragePowerForDurationOffset = new int[durations.Length];
            _peakTotalPowerForDuration = new double[durations.Length];

            int longestInterval = durations.Max();
            _buffer = new CircularBuffer(longestInterval + 1);
        }

        public void Add(double power)
        {
            _totalPower += power;
            _durationCount++;
            _totalCount++;

            for (int i = 0; i < _durations.Length; i++)
            {
                _peakTotalPowerForDuration[i] -= _buffer.ReadNegativeOffset(_durations[i] - 1);
                _peakTotalPowerForDuration[i] += power;
                if (_durationCount >= _durations[i])
                {
                    double averagePowerForDuration = _peakTotalPowerForDuration[i] / _durations[i];
                    if (averagePowerForDuration > _peakAveragePowerForDuration[i])
                    {
                        _peakAveragePowerForDuration[i] = averagePowerForDuration;
                        _peakAveragePowerForDurationOffset[i] = _durationCount - _durations[i];
                    }
                }
            }

            _buffer.Add(power);
        }

        public void Reset()
        {
            _durationCount = 0;
            _buffer.Reset();
            for (int i = 0; i < _durations.Length; i++)
            {
                _peakTotalPowerForDuration[i] = 0;
            }
        }

        public double AveragePower
        {
            get { return _totalPower / _totalCount; }
        }

        public double[] PeakAveragePowerForDuration
        {
            get { return _peakAveragePowerForDuration; }
        }

        public int[] PeakAveragePowerForDurationOffset
        {
            get { return _peakAveragePowerForDurationOffset; }
        }

        public int[] Durations
        {
            get { return _durations; }
        }
    }

    // Variability factor curve for heart rate. Compute the {interval} that has the lowest
    // variability over a ride. One way of doing this is to compute the average for {interval}
    // and minimize based on the standard deviation across that interval.
    // There is the (admittedly limited) one-pass algorithm described here:
    // However, given the limited variability of HR data, this should suffice
    // http://www.strchr.com/standard_deviation_in_one_pass

    public class HeartRateVarianceCalculator
    {
        private CircularBuffer _heartRateBuffer;
        private CircularBuffer _squaredHeartRateBuffer;
        private int[] _durations;
        private double _totalHeartRate;
        private double[] _totalHeartRateForDuration;
        private double[] _totalSquaredHeartRateForDuration;
        private double[] _minimumVarianceForDuration;
        private double[] _standardDeviationForDuration;
        private double[] _minimumVarianceForDurationOffset;
        private double[] _meanHeartRateForDuration;
        private int _durationCount;
        private int _totalCount;

        public HeartRateVarianceCalculator(int[] durations)
        {
            _durations = durations;
            _totalHeartRateForDuration = new double[_durations.Length];
            _totalSquaredHeartRateForDuration = new double[durations.Length];
            _minimumVarianceForDuration = new double[durations.Length];
            _standardDeviationForDuration = new double[durations.Length];
            for (int i = 0; i < durations.Length; i++)
            {
                _minimumVarianceForDuration[i] = double.MaxValue;
            }
            
            _minimumVarianceForDurationOffset = new double[durations.Length];
            _meanHeartRateForDuration = new double[durations.Length];

            int longestInterval = durations.Max();
            _heartRateBuffer = new CircularBuffer(longestInterval + 1);
            _squaredHeartRateBuffer = new CircularBuffer(longestInterval + 1);

            _totalHeartRate = 0;
        }

        public void Add(double heartRate)
        {
            double squaredHeartRate = heartRate * heartRate;
            _totalHeartRate += heartRate;
            _durationCount++;
            _totalCount++;

            for (int i = 0; i < _durations.Length; i++)
            {
                _totalHeartRateForDuration[i] -= _heartRateBuffer.ReadNegativeOffset(_durations[i] - 1);
                _totalHeartRateForDuration[i] += heartRate;

                _totalSquaredHeartRateForDuration[i] -= _squaredHeartRateBuffer.ReadNegativeOffset(_durations[i] - 1);
                _totalSquaredHeartRateForDuration[i] += squaredHeartRate;

                if (_durationCount >= _durations[i])
                {
                    double mean = _totalHeartRateForDuration[i] / _durations[i];
                    double variance = Math.Abs(_totalSquaredHeartRateForDuration[i] / _durations[i] - mean * mean);
                    if (variance < _minimumVarianceForDuration[i])
                    {
                        _minimumVarianceForDuration[i] = variance;
                        _minimumVarianceForDurationOffset[i] = _durationCount - _durations[i];
                        _meanHeartRateForDuration[i] = mean;
                        _standardDeviationForDuration[i] = Math.Sqrt(variance);
                    }
                }
            }
            _heartRateBuffer.Add(heartRate);
        }

        // Reset all currently running calculations, with the exception of the average heart rate for the activity.
        // This is typically called when the activity timer stops, either automatically or manually by the user.
        public void Reset()
        {
            _durationCount = 0;
            _heartRateBuffer.Reset();
            _squaredHeartRateBuffer.Reset();

            for (int i = 0; i < _durations.Length; i++)
            {
                _totalHeartRateForDuration[i] = 0;
                _totalSquaredHeartRateForDuration[i] = 0;
            }
        }

        public double[] MeanHeartRateForDuration
        {
            get { return _meanHeartRateForDuration; }
        }

        public double[] StandardDeviationForDuration
        {
            get { return _standardDeviationForDuration; }
        }

        public double AverageHeartRate
        {
            get { return _totalHeartRate / _totalCount; }
        }

        public int[] Durations
        {
            get { return _durations; }
        }
    }

    // This algorithm looks for the maximum NP over any time series that  
    // satisfies the minimum standard deviation for HR constraint.
    public class EfficiencyFactorCalculator
    {
        const int DURATION = 30;

        // Fields from HeartRateVarianceCalculator
        private CircularBuffer _heartRateBuffer;
        private CircularBuffer _squaredHeartRateBuffer;
        private int[] _durations;
        private double _totalHeartRate;
        private double[] _totalHeartRateForDuration;
        private double[] _totalSquaredHeartRateForDuration;
        private double[] _standardDeviationForDuration;
        private double[] _meanHeartRateForDuration;
        private double _minimumStandardDeviation;
        private double[][] _capturedHeartRateTraces;
        private int _durationCount;
        private int _totalCount;

        // Fields from NormalizedPowerCalculator
        private CircularBuffer _average30SecondPowerBuffer;
        private CircularBuffer _normalizedPowerBuffer;
        private double[] _powerToFourthPowerTotalForDuration;
        private double[] _normalizedPowerForDuration;
        private double[] _efficiencyFactorForDuration;
        private int[] _efficiencyFactorForDurationOffset;
        private double _power30SecondTotal;

        public EfficiencyFactorCalculator(int[] durations, double minimumStandardDeviation)
        {
            _durations = durations;
            _totalHeartRateForDuration = new double[_durations.Length];
            _totalSquaredHeartRateForDuration = new double[durations.Length];
            _standardDeviationForDuration = new double[durations.Length];
            _capturedHeartRateTraces = new double[durations.Length][];
            
            _meanHeartRateForDuration = new double[durations.Length];
            _minimumStandardDeviation = minimumStandardDeviation;

            int longestInterval = durations.Max();
            _heartRateBuffer = new CircularBuffer(longestInterval + 1);
            _squaredHeartRateBuffer = new CircularBuffer(longestInterval + 1);
            _normalizedPowerBuffer = new CircularBuffer(longestInterval + 1);

            _totalHeartRate = 0;

            _average30SecondPowerBuffer = new CircularBuffer(DURATION + 1);
            _powerToFourthPowerTotalForDuration = new double[durations.Length];
            _normalizedPowerForDuration = new double[durations.Length];
            _efficiencyFactorForDuration = new double[durations.Length];
            _efficiencyFactorForDurationOffset = new int[durations.Length];

            _power30SecondTotal = 0.0;
        }

        public void Add(double power, double heartRate)
        {
            double squaredHeartRate = heartRate * heartRate;
            _totalHeartRate += heartRate;

            // Compute 30s average power
            _power30SecondTotal -= _average30SecondPowerBuffer.ReadNegativeOffset(DURATION - 1);
            _power30SecondTotal += power;
            _average30SecondPowerBuffer.Add(power);

            _durationCount++;
            _totalCount++;

            // Compute normalized power 
            if (_durationCount > DURATION)
            {
                double power30SecondAverageToFourthPower = Math.Pow(_power30SecondTotal / DURATION, 4);

                for (int i = 0; i < _durations.Length; i++)
                {
                    _powerToFourthPowerTotalForDuration[i] -= _normalizedPowerBuffer.ReadNegativeOffset(_durations[i] - 1);
                    _powerToFourthPowerTotalForDuration[i] += power30SecondAverageToFourthPower;
                }

                _normalizedPowerBuffer.Add(power30SecondAverageToFourthPower);
            }

            // Compute heart rate variance
            for (int i = 0; i < _durations.Length; i++)
            {
                _totalHeartRateForDuration[i] -= _heartRateBuffer.ReadNegativeOffset(_durations[i] - 1);
                _totalHeartRateForDuration[i] += heartRate;

                _totalSquaredHeartRateForDuration[i] -= _squaredHeartRateBuffer.ReadNegativeOffset(_durations[i] - 1);
                _totalSquaredHeartRateForDuration[i] += squaredHeartRate;

                if (_durationCount >= _durations[i])
                {
                    double mean = _totalHeartRateForDuration[i] / _durations[i];
                    double variance = Math.Abs(_totalSquaredHeartRateForDuration[i] / _durations[i] - mean * mean);
                    double standardDeviation = Math.Sqrt(variance);

                    // If we meet the minimum standard deviation, look to see if we have a new NP maxima
                    if (standardDeviation < _minimumStandardDeviation)
                    {
                        double normalizedPowerForDuration = Math.Pow(_powerToFourthPowerTotalForDuration[i] / _durations[i], 0.25);
                        if (normalizedPowerForDuration > _normalizedPowerForDuration[i])
                        {
                            _normalizedPowerForDuration[i] = normalizedPowerForDuration;
                            _meanHeartRateForDuration[i] = mean;
                            _standardDeviationForDuration[i] = standardDeviation;
                            _efficiencyFactorForDuration[i] = normalizedPowerForDuration / mean;
                            _efficiencyFactorForDurationOffset[i] = _durationCount - _durations[i]; 
                            _capturedHeartRateTraces[i] = _heartRateBuffer.CaptureElements(_durations[i]);
                        }
                    }
                }
            }
            _heartRateBuffer.Add(heartRate);
        }

        public void Reset()
        {
            _durationCount = 0;
            _power30SecondTotal = 0;
            _heartRateBuffer.Reset();
            _squaredHeartRateBuffer.Reset();
            _average30SecondPowerBuffer.Reset();
            _normalizedPowerBuffer.Reset();

            for (int i = 0; i < _durations.Length; i++)
            {
                _totalHeartRateForDuration[i] = 0;
                _totalSquaredHeartRateForDuration[i] = 0;
                _powerToFourthPowerTotalForDuration[i] = 0;
            }
        }

        public double[] MeanHeartRateForDuration
        {
            get { return _meanHeartRateForDuration; }
        }

        public double[] StandardDeviationForDuration
        {
            get { return _standardDeviationForDuration; }
        }

        public double AverageHeartRate
        {
            get { return _totalHeartRate / _totalCount; }
        }

        public int[] Durations
        {
            get { return _durations; }
        }

        public double[] NormalizedPowerForDuration
        {
            get { return _normalizedPowerForDuration; }
        }

        public double[] EfficiencyFactorForDuration
        {
            get { return _efficiencyFactorForDuration; }
        }

        public int[] EfficiencyFactorForDurationOffset
        {
            get { return _efficiencyFactorForDurationOffset; }
        }

        public bool HasData
        {
            get { return _totalCount > 0; }
        }
        
        public double[][] CapturedHeartRateTraces
        {
            get { return _capturedHeartRateTraces; }
        }
    }
}