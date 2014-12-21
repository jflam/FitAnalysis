using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FitAnalysis.Tests
{
    [TestClass]
    public class CircularBufferTests
    {
        const int BUFFER_SIZE = 10;

        [TestMethod]
        public void TestAddingElementsNoWrap()
        {
            var buffer = new CircularBuffer(BUFFER_SIZE);
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                buffer.Add(i);
            }

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                Assert.AreEqual(i, buffer.Elements[i]);
            }
        }

        [TestMethod]
        public void TestAddingElementsSingleWrap()
        {
            var buffer = new CircularBuffer(BUFFER_SIZE);
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                buffer.Add(i);
            }

            buffer.Add(42);
            Assert.AreEqual(42, buffer.Elements[0]);
            Assert.AreEqual(1, buffer.Elements[1]);
        }

        [TestMethod]
        public void TestReadNegativeOffsetNoWrap()
        {
            var buffer = new CircularBuffer(BUFFER_SIZE);
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                buffer.Add(i);
            }

            Assert.AreEqual(BUFFER_SIZE - 1, buffer.CurrentElement);
            Assert.AreEqual(BUFFER_SIZE - 2, buffer.ReadNegativeOffset(1));
            Assert.AreEqual(0, buffer.ReadNegativeOffset(BUFFER_SIZE - 1));
        }

        [TestMethod]
        public void TestReadNegativeOffsetWrap()
        {
            var buffer = new CircularBuffer(BUFFER_SIZE);
            for (int i = 0; i < BUFFER_SIZE + 5; i++)
            {
                buffer.Add(i);
            }

            Assert.AreEqual(BUFFER_SIZE + 4, buffer.CurrentElement);
            Assert.AreEqual(BUFFER_SIZE, buffer.ReadNegativeOffset(4));
            Assert.AreEqual(BUFFER_SIZE - 1, buffer.ReadNegativeOffset(5));
            Assert.AreEqual(BUFFER_SIZE - 2, buffer.ReadNegativeOffset(6));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestReadNegativeOffsetException()
        {
            var buffer = new CircularBuffer(BUFFER_SIZE);
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                buffer.Add(i);
            }

            buffer.ReadNegativeOffset(BUFFER_SIZE);
        }
    }
}