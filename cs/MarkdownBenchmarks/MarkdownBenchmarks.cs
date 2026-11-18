using BenchmarkDotNet.Attributes;
using Markdown;
public class MarkdownBenchmarks
{
    private Md md;
    private InlineParser parser;
    private HtmlRenderer renderer;
    private string input;

    [Params(1, 100, 1000, 10000)]
    private int countOfSymbols;

    [GlobalSetup]
    public void Setup()
    {
        parser = new InlineParser();
        renderer = new HtmlRenderer();

        md = new Md(parser,renderer);

        var text = new string('a', countOfSymbols);

        var textWithTags = string.Join();
    }
}