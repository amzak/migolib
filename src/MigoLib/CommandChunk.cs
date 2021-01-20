using System;
using System.Buffers;

namespace MigoLib
{
    public class Chunk
    {
        public static CommandChunk Next(int size)
        {
            var data = ArrayPool<byte>.Shared.Rent(size);

            return new CommandChunk(size)
            {
                Data = data
            };
        }
    }
    
    public struct CommandChunk: IDisposable
    {
        public byte[] Data { get; init; }

        private readonly int _size;

        public CommandChunk(int size)
        {
            Data = ArrayPool<byte>.Shared.Rent(size);
            _size = size;
        }
        
        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Data);
        }

        public ArraySegment<byte> AsSegment() => new(Data, 0, _size);

        public CommandChunk Crop(int size) =>
            new(size)
            {
                Data = Data
            };
    }
}