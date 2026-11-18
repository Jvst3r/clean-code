using FluentAssertions;
using Markdown;
using Markdown.Models;
using Markdown.Models.Interfaces;
using Markdown.Models.TagInfos;
using Markdown.Models.TagTokens;
using MarkdownUnitTests.Extensions;
using NUnit.Framework;

namespace MarkdownUnitTests
{
    [TestFixture]
    public class InlineParserTests
    {
        private IInlineParser parser;

        [SetUp]
        public void Setup()
        {
            parser = new InlineParser();
        }

        [Test]
        public void ParseToTokens_ThrowsArgumentNullException_WhenNull()
        {
            string line = null;

            var action = () => parser.ParseToTokens(line);

            action.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ParseToTokens_ReturnsEmptyList_WhenEmptyLine()
        {
            var line = "";

            var result = parser.ParseToTokens(line);

            result.Should().BeEmpty();
        }

        [TestCase("text")]
        [TestCase("   text with spaces   ")]
        [TestCase("13012007")]
        public void ParseToTokens_ReturnsListWithOneElement_WhenValidLineWithoutTags(string line)
        {
            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenBackslashBeforeTag()
        {
            var line = "\\_text\\_";

            var result = parser.ParseToTokens(line);
            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = "_text_" });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenOnlyBackslashes()
        {
            var line = "\\\\";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = "\\" });
        }

        [Test]
        public void ParseToTokens_ReturnsList_WhenSingleEmTokenWithText()
        {
            var line = "_text_";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "_" },
                Children = new[] { new { Content = "text" } }
            });
        }

        [Test]
        public void ParseToTokens_ReturnsList_WhenSingleStrongTokenWithText()
        {
            var line = "__text__";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "__" },
                Children = new[] { new { Content = "text" } }
            });
        }

        [TestCase("_")]
        [TestCase("__")]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenTagWithSpaces(string tagText)
        {
            var line = $"{tagText} text {tagText}";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });
        }

        [TestCase("_")]
        [TestCase("__")]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenUnclosedTag(string tagText)
        {
            var line = $"{tagText}text";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });

            line = $"text{tagText}";

            result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithManyTokens_WhenMultiplyTags()
        {
            var line = "_a lot_ of __tags__";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(3);

            result[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "_" },
                Children = new[] { new { Content = "a lot" } }
            });

            result[1].AsText().Should().BeEquivalentTo(new { Content = " of " });

            result[2].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "__" },
                Children = new[] { new { Content = "tags" } }
            });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithManyTagTokens_WhenStrongContainsEmInside()
        {
            var line = "__strong and _em_ text__";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "__" },
                Children = new object[]
                {
                    new { Content = "strong and " },
                    new
                    {
                        Tag = new { MarkdownSymbol = "_" },
                        Children = new[] { new { Content = "em" } }
                    },
                    new { Content = " text" }
                }
            });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithOneTagToken_WhenEmContainsStrongInside()
        {
            var result = parser.ParseToTokens("_text __strong__ text_");

            result.Should().HaveCount(1);
            result[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "_" },
                Children = new[] { new { Content = "text __strong__ text" } }
            });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenLineContainsNewline()
        {
            var result = parser.ParseToTokens("first\nsecond");

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = "first\nsecond" });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenTagSpansNewline()
        {
            var result = parser.ParseToTokens("_first\nsecond_");

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = "_first\nsecond_" });
        }

        [Test]
        public void ParseToTokens_ReturnsCorrectLists_WhenCalledForMultipleLines()
        {
            var result1 = parser.ParseToTokens("_first_");
            var result2 = parser.ParseToTokens("__second__");
            var result3 = parser.ParseToTokens("plain text");

            result1.Should().HaveCount(1);
            result1[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "_" },
                Children = new[] { new { Content = "first" } }
            });

            result2.Should().HaveCount(1);
            result2[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "__" },
                Children = new[] { new { Content = "second" } }
            });

            result3.Should().HaveCount(1);
            result3[0].AsText().Should().BeEquivalentTo(new { Content = "plain text" });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenTripleUnderscores()
        {
            var line = "___";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });
        }

        [Test]
        public void ParseToTokens_ReturnsEmToken_WhenUnderscoreAtWordStart()
        {
            var line = "_нач_але";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(2);
            result[0].AsTag().Should().BeEquivalentTo(new
            {
                Tag = new { MarkdownSymbol = "_" },
                Children = new[] { new { Content = "нач" } }
            });
            result[1].AsText().Should().BeEquivalentTo(new { Content = "але" });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenDigitsInsideUnderscores()
        {
            var line = "цифрами_12_3";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });
        }

        [Test]
        public void ParseToTokens_ReturnsListWithSingleToken_WhenTrailingBackslash()
        {
            var line = "some\\";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });
        }

        [Test] //вот дл€ этого теста нужно ввести проверку на вложенность
        public void ParseToTokens_ReturnsListWithSingleToken_WhenCrossedTags()
        {
            var line = "__пересечени€_двойных__ и одинарных_";

            var result = parser.ParseToTokens(line);

            result.Should().HaveCount(1);
            result[0].AsText().Should().BeEquivalentTo(new { Content = line });
        }

        [Test]
        public void ParseLines_ReturnsH1Token_WhenLineStartsWithHash()
        {
            var lines = new[] { "# «аголовок" };

            var result = parser.ParseLines(lines);

            result.Should().HaveCount(1);
            result[0].Should().BeEquivalentTo(new TagToken(
                new SingleTagInfo("# ", "h1"),
                new List<IToken> { new TextToken("«аголовок") }
            ));
        }

        [Test]
        public void ParseLines_ParsesInlineFormatting_WhenInsideHeader()
        {
            var lines = new[] { "# “екст с _курсивом_" };

            var result = parser.ParseLines(lines);

            result.Should().HaveCount(1);
            result[0].Should().BeEquivalentTo(new TagToken(
                new SingleTagInfo("# ", "h1"),
                new List<IToken>
                {
                    new TextToken("“екст с "),
                    new TagToken(
                            new PairedTagInfo("_", "em"),
                            new List<IToken> { new TextToken("курсивом") }
                )
                }
            ));
        }

        [Test]
        public void ParseLines_SingleListItemInUl()
        {
            var lines = new[] { "- один пункт" };

            var result = parser.ParseLines(lines);

            result.Should().HaveCount(1);
            result[0].Should().BeOfType<CascadTagToken>();
            result[0].Should().BeEquivalentTo(new CascadTagToken(
                new CascadTagInfo(),
                new List<IToken>
                {
                    new TagToken(
                        new SingleTagInfo("- ", "li"),
                        new List<IToken> { new TextToken("один пункт") }
                    )
                }
            ));
        }

        [Test]
        public void ParseLines_GroupsConsecutiveListItemsIntoSingleUl()
        {
            var lines = new[] { "- первый", "- второй", "- третий" };

            var result = parser.ParseLines(lines);

            result.Should().HaveCount(1);
            result[0].Should().BeOfType<CascadTagToken>();
            result[0].Should().BeEquivalentTo(new CascadTagToken(
                new CascadTagInfo(),
                new List<IToken>
                {
                    new TagToken(new SingleTagInfo("- ", "li"), new List<IToken> { new TextToken("первый") }),
                    new TagToken(new SingleTagInfo("- ", "li"), new List<IToken> { new TextToken("второй") }),
                    new TagToken(new SingleTagInfo("- ", "li"), new List<IToken> { new TextToken("третий") })
                }
            ));
        }

        [Test]
        public void ParseLines_ParsesInlineFormatting_WhenInsideListItem()
        {
            var lines = new[] { "- _курсив_ и __жирный__" };

            var result = parser.ParseLines(lines);

            result.Should().HaveCount(1);
            result[0].Should().BeEquivalentTo(new CascadTagToken(
                new CascadTagInfo(),
                new List<IToken>
                {
                    new TagToken(
                        new SingleTagInfo("- ", "li"),
                        new List<IToken>
                        {
                            new TagToken(new PairedTagInfo("_", "em"), new List<IToken> { new TextToken("курсив") }),
                            new TextToken(" и "),
                            new TagToken(new PairedTagInfo("__", "strong"), new List<IToken> { new TextToken("жирный") })
                        }
                    )
                }
            ));
        }
    }
}