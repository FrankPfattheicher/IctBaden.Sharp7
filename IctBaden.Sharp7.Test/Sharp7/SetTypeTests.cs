using System;
using Sharp7;
using Xunit;

namespace IctBaden.Sharp7.Test
{
    public class SetTypeTests
    {
        private byte[] buffer;

        private void PerpareBuffer()
        {
            buffer = new byte[64];
            for (var ix = 0; ix < buffer.Length; ix++)
            {
                buffer[ix] = 0x55;
            }
        }

        [Fact]
        public void SetShortStringAtShouldSetMaxLengthAndCurrentLength()
        {
            const string text = "test";
            const int offset  = 8;
            const int maxLength = 10;

            PerpareBuffer();
            S7.SetStringAt(buffer, offset, maxLength, text);

            Assert.Equal(maxLength, buffer[offset]);
            Assert.Equal(text.Length, buffer[offset+1]);
        }

        [Fact]
        public void SetLongStringAtShouldSetMaxLengthAndCurrentLength()
        {
            const string text = "test-1234567890";
            const int offset = 8;
            const int maxLength = 10;

            PerpareBuffer();
            S7.SetStringAt(buffer, offset, maxLength, text);

            Assert.Equal(maxLength, buffer[offset]);
            Assert.Equal(text.Length, buffer[offset + 1]);
        }
    }
}
