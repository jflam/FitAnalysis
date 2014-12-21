using System;
namespace FitAnalysis
{
    public class CircularBuffer
    {
        private double[] _elements;
        private int _maxElements;
        private int _position;

        public CircularBuffer(int maxElements)
        {
            _position = maxElements - 1;
            _maxElements = maxElements;
            _elements = new double[maxElements];
        }

        public double ReadNegativeOffset(int offset)
        {
            if (offset >= _maxElements)
            {
                throw new ArgumentException("Attempting to read at a negative offset larger than the size of the circular buffer");
            }

            int newOffset = _position - offset;
            if (newOffset < 0)
            {
                newOffset += _maxElements;
            }
            return _elements[newOffset];
        }

        public void Add(double element)
        {
            // We increment the position before we add the element
            if (_position == _maxElements - 1)
            {
                // We are wrapping
                _position = 0;
                _elements[0] = element;
            }
            else
            {
                // We are appending and over-writing
                _position++;
                _elements[_position] = element;
            }
        }

        public double CurrentElement
        {
            get { return _elements[_position]; }
        }

        public double[] Elements
        {
            get { return _elements; }
        }
    }
}