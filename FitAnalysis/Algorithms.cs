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
        private int _powerReadingCount;
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

            if (_powerReadingCount >= DURATION)
            {
                double power30SecondAverage = _power30SecondTotal / 30.0;
                _power30SecondAverageToFourthPowerTotal += Math.Pow(power30SecondAverage, 4);
            }

            _powerReadingCount++;
        }

        public double NormalizedPower
        {
            get { return Math.Pow(_power30SecondAverageToFourthPowerTotal / ((double)(_powerReadingCount - DURATION)), 0.25); }
        }

        public double IntensityFactor
        {
            get { return NormalizedPower / _ftp; }
        }

        public double TrainingStressScore
        {
            get { return (_powerReadingCount * NormalizedPower * IntensityFactor) / (_ftp * 3600) * 100; }
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
        private int _count;

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
            _count++;

            for (int i = 0; i < _durations.Length; i++)
            {
                _peakTotalPowerForDuration[i] -= _buffer.ReadNegativeOffset(_durations[i] - 1);
                _peakTotalPowerForDuration[i] += power;
                if (_count >= _durations[i])
                {
                    double averagePowerForDuration = _peakTotalPowerForDuration[i] / _durations[i];
                    if (averagePowerForDuration > _peakAveragePowerForDuration[i])
                    {
                        _peakAveragePowerForDuration[i] = averagePowerForDuration;
                        _peakAveragePowerForDurationOffset[i] = _count - _durations[i];
                    }
                }
            }

            _buffer.Add(power);
        }

        public double AveragePower
        {
            get { return _totalPower / _count; }
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
        private double[] _minimumVarianceOffsetForDuration;
        private double[] _meanHeartRateForDuration;

        private int _count;

        public HeartRateVarianceCalculator(int[] durations)
        {
            _durations = durations;
            _totalHeartRateForDuration = new double[_durations.Length];
            _totalSquaredHeartRateForDuration = new double[durations.Length];
            _minimumVarianceForDuration = new double[durations.Length];
            for (int i = 0; i < durations.Length; i++)
            {
                _minimumVarianceForDuration[i] = double.MaxValue;
            }
            
            _minimumVarianceOffsetForDuration = new double[durations.Length];
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
            _count++;

            for (int i = 0; i < _durations.Length; i++)
            {
                _totalHeartRateForDuration[i] -= _heartRateBuffer.ReadNegativeOffset(_durations[i] - 1);
                _totalHeartRateForDuration[i] += heartRate;

                _totalSquaredHeartRateForDuration[i] -= _squaredHeartRateBuffer.ReadNegativeOffset(_durations[i] - 1);
                _totalSquaredHeartRateForDuration[i] += squaredHeartRate;

                if (_count >= _durations[i])
                {
                    double mean = _totalHeartRateForDuration[i] / _durations[i];
                    double variance = Math.Abs(_totalSquaredHeartRateForDuration[i] / _durations[i] - mean * mean);
                    if (variance < _minimumVarianceForDuration[i])
                    {
                        _minimumVarianceForDuration[i] = variance;
                        _minimumVarianceOffsetForDuration[i] = _count - _durations[i];
                        _meanHeartRateForDuration[i] = mean;
                    }
                }
            }
        }

        public double[] MeanHeartRateForDuration
        {
            get { return _meanHeartRateForDuration; }
        }

        public double[] VarianceForDuration
        {
            get { return _minimumVarianceForDuration; }
        }

        public double AverageHeartRate
        {
            get { return _totalHeartRate / _count; }
        }

        public int[] Durations
        {
            get { return _durations; }
        }
    }
}