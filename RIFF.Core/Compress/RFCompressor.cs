// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Linq;
using System.Text;

namespace RIFF.Core
{
    public static class RFCompressor
    {
        private const int WRAP_LENGTH = WRAP_OFFSET_8;

        private const int WRAP_OFFSET_0 = 0;

        private const int WRAP_OFFSET_4 = sizeof(int);

        private const int WRAP_OFFSET_8 = 2 * sizeof(int);

        public static byte[] CompressBytes(byte[] input)
        {
            return Wrap(input);
        }

        public static byte[] CompressString(string input)
        {
            return CompressBytes(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] DecompressBytes(byte[] input)
        {
            return Unwrap(input);
        }

        public static string DecompressString(byte[] input)
        {
            return Encoding.UTF8.GetString(DecompressBytes(input));
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] Unwrap(
            byte[] inputBuffer, int inputOffset = 0)
        {
            var inputLength = inputBuffer.Length - inputOffset;
            if (inputLength < WRAP_LENGTH)
                throw new ArgumentException("inputBuffer size is invalid");

            var outputLength = (int)Peek4(inputBuffer, inputOffset + WRAP_OFFSET_0);
            inputLength = (int)Peek4(inputBuffer, inputOffset + WRAP_OFFSET_4);
            if (inputLength > inputBuffer.Length - inputOffset - WRAP_LENGTH)
                throw new ArgumentException("inputBuffer size is invalid or has been corrupted");

            byte[] result;

            if (inputLength >= outputLength)
            {
                result = new byte[inputLength];
                Buffer.BlockCopy(
                    inputBuffer, inputOffset + WRAP_OFFSET_8,
                    result, 0, inputLength);
            }
            else
            {
                result = new byte[outputLength];
                LZ4.LZ4Codec.Decode(
                    inputBuffer, inputOffset + WRAP_OFFSET_8, inputLength,
                    result, 0, outputLength,
                    true);
            }

            return result;
        }

        public static byte[] Wrap(
            byte[] inputBuffer, int inputOffset = 0, int inputLength = int.MaxValue)
        {
            inputLength = Math.Min(inputBuffer.Length - inputOffset, inputLength);
            if (inputLength < 0)
                throw new ArgumentException("inputBuffer size of inputLength is invalid");
            if (inputLength == 0)
                return new byte[WRAP_LENGTH];

            var outputLength = inputLength;
            var outputBuffer = new byte[outputLength];

            if (inputLength > 100000)
            {
                // high compression for large sets only
                outputLength = LZ4.LZ4Codec.EncodeHC(
                    inputBuffer, inputOffset, inputLength, outputBuffer, 0, outputLength);
            }
            else
            {
                outputLength = LZ4.LZ4Codec.Encode(
                    inputBuffer, inputOffset, inputLength, outputBuffer, 0, outputLength);
            }

            byte[] result;

            if (outputLength >= inputLength || outputLength == 0)
            {
                result = new byte[inputLength + WRAP_LENGTH];
                Poke4(result, WRAP_OFFSET_0, (uint)inputLength);
                Poke4(result, WRAP_OFFSET_4, (uint)inputLength);
                Buffer.BlockCopy(inputBuffer, inputOffset, result, WRAP_OFFSET_8, inputLength);
            }
            else
            {
                result = new byte[outputLength + WRAP_LENGTH];
                Poke4(result, WRAP_OFFSET_0, (uint)inputLength);
                Poke4(result, WRAP_OFFSET_4, (uint)outputLength);
                Buffer.BlockCopy(outputBuffer, 0, result, WRAP_OFFSET_8, outputLength);
            }

            return result;
        }

        private static uint Peek4(byte[] buffer, int offset)
        {
            // NOTE: It's faster than BitConverter.ToUInt32 (suprised? me too)
            return
                ((uint)buffer[offset]) |
                ((uint)buffer[offset + 1] << 8) |
                ((uint)buffer[offset + 2] << 16) |
                ((uint)buffer[offset + 3] << 24);
        }

        private static void Poke4(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }
    }
}
