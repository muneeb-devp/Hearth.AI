using System.Text;

namespace Hearth.Rag.Chunking;

internal sealed class RecursiveChunker : IDocumentChunker
{
    private static readonly string[] Separators = ["\n\n", "\n", ". ", " "];

    public IEnumerable<TextChunk> Chunk(string text, ChunkerOptions options)
    {
        return SplitRecursive(text, 0, options, Separators);
    }

    private static IEnumerable<TextChunk> SplitRecursive(
        string text, int offset, ChunkerOptions opts, string[] separators)
    {
        int tokenEstimate = text.Length / 4;
        if (tokenEstimate <= opts.ChunkSize)
        {
            if (text.Length > 0)
            {
                yield return new TextChunk(text, offset, offset + text.Length);
            }

            yield break;
        }

        var sep = separators[0];
        var remaining = separators.Length > 1 ? separators[1..] : separators;

        var splits = text.Split(sep);
        var current = new StringBuilder();
        int currentOffset = offset;

        foreach (var split in splits)
        {
            bool wouldExceed = (current.Length + split.Length) / 4 > opts.ChunkSize;
            if (wouldExceed && current.Length > 0)
            {
                foreach (var chunk in SplitRecursive(current.ToString(), currentOffset, opts, remaining))
                {
                    yield return chunk;
                }

                // apply overlap: keep last overlapChars characters from current as prefix
                var overlapText = current.ToString();
                int overlapChars = opts.Overlap * 4;
                currentOffset += current.Length;
                current.Clear();
                if (overlapText.Length > overlapChars)
                {
                    var overlap = overlapText[^overlapChars..];
                    current.Append(overlap);
                    currentOffset -= overlapChars;
                }
            }

            current.Append(split).Append(sep);
        }

        if (current.Length > 0)
        {
            foreach (var chunk in SplitRecursive(current.ToString().TrimEnd(), currentOffset, opts, remaining))
            {
                yield return chunk;
            }
        }
    }
}
