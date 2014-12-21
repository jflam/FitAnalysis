using System;
using System.Linq;

namespace FitAnalysis
{
    class NormalizedPowerCalculator
    {
        const int DURATION = 30;

        private CircularBuffer _buffer;
        private double _power30SecondTotal;
        private double _power30SecondAverageToFourthPowerTotal;
        private int _powerReadingCount;

        public NormalizedPowerCalculator()
        {
            _buffer = new CircularBuffer(DURATION + 1);
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
}