namespace Asymptote.Util.String;

public class StringReader
{
    private readonly char[] characters;
    private int cursorPos = 0;

    public StringReader(string str) => characters = str.ToCharArray();

    public bool CanRead() => cursorPos < characters.Length;
    public char Peek() => characters[cursorPos];
    public char PeekOffset(int offset) => characters[cursorPos + offset];
    public char Read() => characters[cursorPos++];
    public void Skip() => cursorPos++;

    public void SkipWhitespace()
    {
        while (CanRead() && char.IsWhiteSpace(Peek())) Skip();
    }
}