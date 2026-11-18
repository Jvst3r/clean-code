using BenchmarkDotNet.Attributes;
using Markdown;
public class MarkdownBenchmarks
{
    private Md md;
    private InlineParser parser;
    private HtmlRenderer renderer;
    private string input;

    [Params(100, 1000, 10000, 100000)]
    public int countOfSymbols;
    private string textWithTags;
    private string textWithLists;

    [GlobalSetup]
    public void Setup()
    {
        parser = new InlineParser();
        renderer = new HtmlRenderer();

        md = new Md(parser, renderer);

        input = new string('a', countOfSymbols);

        var listPattern = "- item\n";
        var listRepeats = Math.Max(1, countOfSymbols / listPattern.Length);
        textWithLists = string.Concat(Enumerable.Repeat(listPattern, listRepeats));

        var tagPattern = "_word_ ";
        var tagRepeats = Math.Max(1, countOfSymbols / tagPattern.Length);
        textWithTags = string.Concat(Enumerable.Repeat(tagPattern, tagRepeats));
    }

        [Benchmark]
    public void Render_OnlyText()
    {
        md.Render(input);
    }

    [Benchmark]
    public void Render_WithTags()
    {
        md.Render(textWithTags);
    }

    [Benchmark]
    public void Render_WithLists()
    {
        md.Render(textWithLists);
    }
}