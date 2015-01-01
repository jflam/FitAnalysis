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

        public void Reset()
        {
            _position = 0;
            // TODO: http://stackoverflow.com/questions/5943850/fastest-way-to-fill-an-array-with-a-single-value
            for (int i = 0; i < _elements.Length; i++)
            {
                _elements[i] = 0;
            }
        }

        public double[] CaptureElements(int count)
        {
            if (count > _elements.Length)
            {
                throw new ArgumentException("Attempting to retrieve more elements than exists in the buffer");
            }

            var result = new double[count];
            if (count > _position)
            {
                // We wrap around to back end of circular buffer ... 
                // Copy the back end of the array first
                int elementsToCopyFromBackEnd = -(_position - count);
                Array.Copy(_elements, _elements.Length - elementsToCopyFromBackEnd, result, 0, elementsToCopyFromBackEnd);
                Array.Copy(_elements, 0, result, elementsToCopyFromBackEnd, _position);
            }
            else
            {
                // No wrap to back end of circular buffer ... single copy
                Array.Copy(_elements, _position - count, result, 0, count);
            }

            return result;
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