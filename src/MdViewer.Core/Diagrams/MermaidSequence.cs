namespace MdViewer.Core.Diagrams;

/// <summary>
/// <c>sequenceDiagram</c>: participants, messages, notes, frames and dividers.
/// Activations and colour boxes are ignored — they do not change the meaning.
/// </summary>
public static class MermaidSequence
{
    private static readonly string[] Arrows = ["-->>", "->>", "--x", "-x", "--)", "-)", "-->", "->"];

    public static bool TryParse(string source, out SequenceDiagram? diagram, out string? error)
    {
        diagram = null;
        error = null;

        var result = new SequenceDiagram();
        bool sawHeader = false;
        int number = 0;

        foreach (string rawLine in source.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            if (!sawHeader)
            {
                if (!line.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase))
                {
                    error = "not a sequence diagram";
                    return false;
                }

                sawHeader = true;
                continue;
            }

            if (line.Equals("autonumber", StringComparison.OrdinalIgnoreCase))
            {
                result.AutoNumber = true;
                continue;
            }

            if (line.StartsWith("title ", StringComparison.OrdinalIgnoreCase))
            {
                result.Title = MermaidDiagrams.Clean(line[6..]);
                continue;
            }

            if (line.StartsWith("participant ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("actor ", StringComparison.OrdinalIgnoreCase))
            {
                bool isActor = line.StartsWith("actor ", StringComparison.OrdinalIgnoreCase);
                string body = line[(isActor ? 6 : 12)..].Trim();
                string id = body;
                string? label = null;
                int asIndex = body.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
                if (asIndex > 0)
                {
                    id = body[..asIndex].Trim();
                    label = MermaidDiagrams.Clean(body[(asIndex + 4)..]);
                }

                result.Ensure(id, label, isActor);
                continue;
            }

            if (line.StartsWith("note ", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseNote(line, out SequenceNoteStep? note))
                {
                    error = $"cannot read note: {line}";
                    return false;
                }

                result.Ensure(note!.ActorA, null, false);
                if (note.ActorB is not null)
                {
                    result.Ensure(note.ActorB, null, false);
                }

                result.Steps.Add(note);
                continue;
            }

            if (line.StartsWith("activate ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("deactivate ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("box", StringComparison.OrdinalIgnoreCase) || line.StartsWith("end box", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("==") || line.StartsWith("---"))
            {
                string divider = MermaidDiagrams.Clean(line.Trim('=', '-', ' '));
                result.Steps.Add(new SequenceDividerStep(divider));
                continue;
            }

            string head = line.Split(' ', 2)[0].ToLowerInvariant();
            if (head is "loop" or "alt" or "opt" or "par" or "critical" or "break" or "rect" or "else" or "and" or "option")
            {
                string label = line.Length > head.Length ? MermaidDiagrams.Clean(line[head.Length..]) : string.Empty;
                result.Steps.Add(new SequenceFrameStep(head, label, isEnd: false, isElse: head is "else" or "and" or "option"));
                continue;
            }

            if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                result.Steps.Add(new SequenceFrameStep("end", string.Empty, isEnd: true, isElse: false));
                continue;
            }

            if (!TryParseMessage(line, out string? from, out string? to, out string? text, out bool dashed, out bool open))
            {
                error = $"cannot read message: {line}";
                return false;
            }

            result.Ensure(from!, null, false);
            result.Ensure(to!, null, false);
            number = result.AutoNumber ? number + 1 : 0;
            result.Steps.Add(new SequenceMessageStep(from!, to!, text!, dashed, open, number));
        }

        if (!sawHeader || result.Steps.Count == 0)
        {
            error = sawHeader ? "no messages" : "not a sequence diagram";
            return false;
        }

        diagram = result;
        return true;
    }

    private static bool TryParseNote(string line, out SequenceNoteStep? note)
    {
        note = null;
        int colon = line.IndexOf(':');
        if (colon < 0)
        {
            return false;
        }

        string head = line[5..colon].Trim();
        string text = MermaidDiagrams.Clean(line[(colon + 1)..]);

        string placement = "over";
        if (head.StartsWith("left of", StringComparison.OrdinalIgnoreCase))
        {
            placement = "left";
            head = head[7..].Trim();
        }
        else if (head.StartsWith("right of", StringComparison.OrdinalIgnoreCase))
        {
            placement = "right";
            head = head[8..].Trim();
        }
        else if (head.StartsWith("over", StringComparison.OrdinalIgnoreCase))
        {
            placement = "over";
            head = head[4..].Trim();
        }

        string[] actors = head.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (actors.Length == 0)
        {
            return false;
        }

        note = new SequenceNoteStep(placement, actors[0], actors.Length > 1 ? actors[1] : null, text);
        return true;
    }

    private static bool TryParseMessage(string line, out string? from, out string? to, out string? text, out bool dashed, out bool open)
    {
        from = null;
        to = null;
        text = null;
        dashed = false;
        open = false;

        int colon = line.IndexOf(':');
        string head = colon < 0 ? line : line[..colon];
        string message = colon < 0 ? string.Empty : MermaidDiagrams.Clean(line[(colon + 1)..]);

        foreach (string arrow in Arrows)
        {
            int index = head.IndexOf(arrow, StringComparison.Ordinal);
            if (index <= 0)
            {
                continue;
            }

            string left = head[..index].Trim();
            string right = head[(index + arrow.Length)..].Trim().TrimStart('+', '-');

            if (left.Length == 0 || right.Length == 0)
            {
                continue;
            }

            from = left;
            to = right;
            text = message;
            dashed = arrow.StartsWith("--", StringComparison.Ordinal);
            open = arrow is "-)" or "--)";
            return true;
        }

        return false;
    }
}

