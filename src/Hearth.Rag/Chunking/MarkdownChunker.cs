namespace Hearth.Rag.Chunking;

// Splits markdown on heading boundaries and includes the heading as context in each chunk.
// Falls back to RecursiveChunker for sections that exceed ChunkSize.
internal sealed class MarkdownChunker : IDocumentChunker
{
    public IEnumerable<TextChunk> Chunk(string text, ChunkerOptions options)
    {
        var sections = ParseSections(text);
        var recursive = new RecursiveChunker();

        foreach (var (heading, body, startIdx) in sections)
        {
            var sectionText = string.IsNullOrEmpty(heading) ? body : $"{heading}\n{body}";
            sectionText = sectionText.TrimEnd();

            if (sectionText.Length == 0)
            {
                continue;
            }

            if (sectionText.Length / 4 <= options.ChunkSize)
            {
                yield return new TextChunk(sectionText, startIdx, startIdx + sectionText.Length);
            }
            else
            {
                // Body too large — recurse into it, prepending heading to each sub-chunk
                int bodyOffset = startIdx + (string.IsNullOrEmpty(heading) ? 0 : heading.Length + 1);
                foreach (var subChunk in recursive.Chunk(body.TrimEnd(), options))
                {
                    var contextual = string.IsNullOrEmpty(heading)
                        ? subChunk.Text
                        : $"{heading}\n{subChunk.Text}";
                    yield return new TextChunk(contextual,
                        bodyOffset + subChunk.StartIndex,
                        bodyOffset + subChunk.EndIndex);
                }
            }
        }
    }

    private static List<(string Heading, string Body, int StartIdx)> ParseSections(string text)
    {
        var results = new List<(string, string, int)>();
        var lines = text.Split('\n');
        var currentHeading = string.Empty;
        var bodyLines = new List<string>();
        int offset = 0;
        int sectionStart = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith('#'))
            {
                if (bodyLines.Count > 0 || !string.IsNullOrEmpty(currentHeading))
                {
                    results.Add((currentHeading, string.Join('\n', bodyLines), sectionStart));
                    bodyLines.Clear();
                }

                currentHeading = line;
                sectionStart = offset;
            }
            else
            {
                bodyLines.Add(line);
            }

            offset += line.Length + 1; // +1 for \n
        }

        if (bodyLines.Count > 0 || !string.IsNullOrEmpty(currentHeading))
        {
            results.Add((currentHeading, string.Join('\n', bodyLines), sectionStart));
        }

        return results;
    }
}
