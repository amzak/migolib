using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MigoLib.FileUpload
{
    public class UploadGCodeCommand : ChunkedCommand
    {
        private const int ChunkSize = 1024 * 20; // migo constant
        private readonly GCodeFile _file;

        public UploadGCodeCommand(GCodeFile file)
        {
            _file = file;
        }

        public override IAsyncEnumerable<CommandChunk> Chunks => GetChunks();

        private async IAsyncEnumerable<CommandChunk> GetChunks()
        {
            await using var fileStream = File.OpenRead(_file.FileName);
            int chunksCount = Math.DivRem((int) fileStream.Length, ChunkSize, out int lastChunkSize);

            using (var chunk = Chunk.Next(ChunkSize))
            {
                var size = WritePreamble(chunk);
                yield return chunk.Crop(size);
            }

            int length;

            for (int i = 0; i < chunksCount; i++)
            {
                using var chunk = Chunk.Next(ChunkSize);
                length = await fileStream.ReadAsync(chunk.Data, 0, ChunkSize)
                    .ConfigureAwait(false);
                yield return chunk.Crop(length);
            }

            if (lastChunkSize == 0)
            {
                yield break;
            }

            using (var chunk = Chunk.Next(ChunkSize))
            {
                length = await fileStream.ReadAsync(chunk.Data, 0, ChunkSize)
                    .ConfigureAwait(false);
                yield return chunk.Crop(length);
            }
        }

        private int WritePreamble(CommandChunk chunk)
        {
            var fileName = Path.GetFileName(_file.FileName);

            var writer = new StringBuilder();
            writer.Append("filestart;");
            writer.Append(_file.Size.ToString());
            writer.Append(';');
            writer.Append(fileName);
            writer.Append(';');

            var preamble = writer.ToString();
            return Encoding.UTF8.GetBytes(preamble, chunk.Data);
        }
    }
}