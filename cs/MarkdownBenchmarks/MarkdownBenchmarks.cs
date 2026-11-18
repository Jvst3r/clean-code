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

        md = new Md(parser,renderer);

        input = new string('a', countOfSymbols);

        textWithLists = string.Join("\n",Enumerable.Range(0,countOfSymbols/20).Select(i => $"- item {i}"));
        textWithTags = string.Join(" ", "__strong__ _em_");
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