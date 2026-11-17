using NUnit.Framework;
using Markdown;
using FluentAssertions;
using Markdown.Models.Interfaces;
using Markdown.Models;
using Markdown.Models.TagTokens;
using Markdown.Models.TagInfos;

namespace MarkdownUnitTests
{
    [TestFixture]
    internal class HtmlRendererTests
    {
        private HtmlRenderer renderer;

        [SetUp]
        public void Setup()
        {
            renderer = new HtmlRenderer();
        }

        [Test]
        public void Render_ThrowsException_WhenInvalidData()
        {
            List<IToken> tokens = null;
            var action = () => renderer.Render(tokens);

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Render_ReturnsEmptyString_WhenEmptyList()
        {
            var tokens = new List<IToken>();

            var result = renderer.Render(tokens);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public void Render_ReturnsBackslashLine()
        {
            var tokens = new List<IToken> { new TextToken("\\") };

            var result = renderer.Render(tokens);

            result.Should().Be("\\");
        }

        private static IEnumerable<TestCaseData> TokensWithoutChildrenCases()
        {
            yield return new TestCaseData(new List<IToken> { new TextToken("text without tags йоу") },
                                          "text without tags йоу").SetName("SingleText");

            yield return new TestCaseData(new List<IToken> { new TextToken("x") },
                                          "x").SetName("SingleChar");

            yield return new TestCaseData(new List<IToken> { new TextToken("       x       ") },
                                          "       x       ").SetName("TextWithSpaces");

            yield return new TestCaseData(new List<IToken> { new TextToken("0123456789") },
                                          "0123456789").SetName("Digits");

            //вот такого в моем понимании быть не может, но пусть будет как тест
            yield return new TestCaseData(new List<IToken> { new TextToken("a"), new TextToken("b"), new TextToken("c") },
                                          "abc").SetName("Multi Text Tokens");
        }

        [TestCaseSource(nameof(TokensWithoutChildrenCases), Category = "OnlyTextTokens")]
        public void Render_ReturnsConcatenatedText_WhenTokensWithoutChildren(List<IToken> tokens, string expected)
        {
            var result = renderer.Render(tokens);
            result.Should().Be(expected);
        }

        private static IEnumerable<TestCaseData> TokensWithChildrenCases()
        {
            yield return new TestCaseData(
                new List<IToken>
                {
                    new TagToken(new PairedTagInfo("_", "em"), new List<IToken> { new TextToken("text") })
                },
                "<em>text</em>"
            ).SetName("SingleEm");

            yield return new TestCaseData(
                new List<IToken>
                {
                    new TagToken(new PairedTagInfo("__", "strong"), new List<IToken> { new TextToken("text") })
                },
                "<strong>text</strong>"
            ).SetName("SingleStrong");

            yield return new TestCaseData(
                new List<IToken>
                {
                    new TextToken("i "),
                    new TagToken(new PairedTagInfo("__", "strong"), new List<IToken> { new TextToken("am") }),
                    new TextToken(" senya")
                },
                "i <strong>am</strong> senya"
            ).SetName("TextTagText");

            yield return new TestCaseData(
                new List<IToken>
                {
                    new TagToken(new PairedTagInfo("_", "em"), new List<IToken> { new TextToken("a") }),
                    new TagToken(new PairedTagInfo("__", "strong"), new List<IToken> { new TextToken("b") })
                },
                "<em>a</em><strong>b</strong>"
            ).SetName("TwoTagsInRow");
        }

        [Test, TestCaseSource(nameof(TokensWithChildrenCases), Category = "TokensWithChilren")]
        public void Render_ReturnsTextWithHtmlTags_WhenTokensWithChildren(List<IToken> tokens, string expected)
        {
            var result = renderer.Render(tokens);
            result.Should().Be(expected);
        }

        private static IEnumerable<TestCaseData> DeepNestingCases()
        {
            yield return new TestCaseData(
                new List<IToken>
                {
                    new TagToken(new PairedTagInfo("__", "strong"), new List<IToken>
                    {
                        new TagToken(new PairedTagInfo("_", "em"), new List<IToken> { new TextToken("text") })
                    })
                },
                "<strong><em>text</em></strong>"
            ).SetName("EmInsideStrong");

            yield return new TestCaseData(
                new List<IToken>
                {
                    new TagToken(new PairedTagInfo("_", "em"), new List<IToken>
                    {
                        new TextToken("text "),
                        new TextToken("__strong__ text")
                    })
                },
                "<em>text __strong__ text</em>"
            ).SetName("StrongInsideEmAsPlainText");

            yield return new TestCaseData(
                new List<IToken>
                {
                    new TagToken(new PairedTagInfo("__", "strong"), new List<IToken>
                    {
                        new TextToken("strong and "),
                        new TagToken(new PairedTagInfo("_", "em"), new List<IToken> { new TextToken("em") }),
                        new TextToken(" text")
                    })
                },
                "<strong>strong and <em>em</em> text</strong>"
            ).SetName("EmInsideStrongWithText");
        }

        [Test, TestCaseSource(nameof(DeepNestingCases), Category = "NestingTags")]
        public void Render_ReturnsTextWithHtmlTags_WhenNestingTags(List<IToken> tokens, string expected)
        {
            var result = renderer.Render(tokens);
            result.Should().Be(expected);
        }


        [Test]
        public void RenderTextToken_ReturnsExactContent_WhenTextTokenOnly()
        {
            var token = new TextToken("Hello World");

            var result = renderer.RenderTextToken(token);

            result.Should().Be("Hello World");

        }

        [Test]
        public void RenderTagToken_WrapsChildrenCorrectly()
        {
            var tag = new PairedTagInfo("_", "em");
            var children = new List<IToken> { new TextToken("em") };
            var token = new TagToken(tag, children);

            var result = renderer.RenderTagToken(token);

            result.Should().Be("<em>em</em>");
        }

        [Test]
        public void RenderTagToken_HandlesNestedTokens()
        {
            var strongTag = new PairedTagInfo("__", "strong");
            var emTag = new PairedTagInfo("_", "em");

            var children = new List<IToken>
            {
                new TextToken("Bold and "),
                new TagToken(emTag, new List<IToken> { new TextToken("em") })
            };
            var token = new TagToken(strongTag, children);

            var result = renderer.RenderTagToken(token);

            result.Should().Be("<strong>Bold and <em>em</em></strong>");
        }

        [Test]
        public void Render_CreatesValidListHtml_WhenGivenListTokens()
        {
            var ulTag = new CascadTagInfo();
            var liTag = new SingleTagInfo("- ", "li");

            var tokens = new List<IToken>
            {
                new TagToken(ulTag, new List<IToken>
                {
                    new TagToken(liTag, new List<IToken> { new TextToken("Первый пункт") }),
                    new TagToken(liTag, new List<IToken> { new TextToken("Второй пункт") })
                })
            };

            var result = renderer.Render(tokens);

            result.Should().Be("<ul><li>Первый пункт</li><li>Второй пункт</li></ul>");
        }
    }
}