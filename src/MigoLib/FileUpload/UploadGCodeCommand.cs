using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MigoLib.FileUpload
{
    public class UploadGCodeCommand : ChunkedCommand
    {
        public const int ChunkSize = 1152; // migo constant
        private readonly GCodeFile _file;

        public UploadGCodeCommand(GCodeFile file)
        {
            _file = file;
        }

        public override IAsyncEnumerable<ReadOnlyMemory<byte>> Chunks => GetChunks();

        private async IAsyncEnumerable<ReadOnlyMemory<byte>> GetChunks()
        {
            await using var fileStream = File.OpenRead(_file.FileName);
            int chunksCount = Math.DivRem((int) fileStream.Length, ChunkSize, out int lastChunkSize);

            var bytes = new byte[ChunkSize];
            var buffer = new Memory<byte>(bytes);
            int length;

            var preambleLength = WritePreamble(bytes);
            yield return buffer.Slice(0, preambleLength);

            for (int i = 0; i < chunksCount; i++)
            {
                length = await fileStream.ReadAsync(buffer).ConfigureAwait(false);
                yield return buffer.Slice(0, length);
            }

            if (lastChunkSize == 0)
            {
                yield break;
            }

            length = await fileStream.ReadAsync(buffer).ConfigureAwait(false);
            yield return buffer.Slice(0, length);
        }
        
        private int WritePreamble(byte[] buffer)
        {
            var fileName = Path.GetFileName(_file.FileName);

            var writer = new StringBuilder();
            writer.Append("filestart;");
            writer.Append(_file.Size.ToString());
            writer.Append(';');
            writer.Append(fileName);
            writer.Append(';');

            var preamble = writer.ToString();
            return Encoding.UTF8.GetBytes(preamble, buffer);
        }
    }
}