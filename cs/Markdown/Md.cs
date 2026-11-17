using Markdown.Models.Interfaces;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Markdown
{
    /// <summary>
    /// Публичный класс - интефейс между пользователем и логикой (оркестратор это вроде называется)
    /// </summary>
    public class Md
    {
        private readonly IInlineParser parser;
        private readonly IHtmlRenderer renderer;

        public Md (IInlineParser inlineParser, IHtmlRenderer HtmlRenderer)
        {
            parser = inlineParser;
            renderer = HtmlRenderer;
        }

        public string Render(string  markdownText)
        {
            if (string.IsNullOrEmpty(markdownText))
                return string.Empty;

            var lines = markdownText.Split('\n', '\r');
            //var HtmlText = new StringBuilder()
            var tokens = parser.ParseLines(lines);
            var htmlText = renderer.Render(tokens);

            return htmlText;
        }
    }
}
