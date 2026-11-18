using FluentAssertions;
using Markdown;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownUnitTests
{
    [TestFixture]
    internal class MdTests
    {
        private Md md;

        [SetUp]
        public void Setup()
        {
            md = new Md(new InlineParser(), new HtmlRenderer());
        }

        [Test]
        public void Render_MixedDocument_ParsesAndRendersCorrectly()
        {
            var input = "# " +
                "Заголовок\nОбычный текст\n- " +
                "пункт 1\n- " +
                "пункт 2";

            var result = md.Render(input);

            result.Should().Be(
                "<h1>Заголовок</h1>\nОбычный текст\n" +
                "<ul>" +
                "<li>пункт 1</li>" +
                "<li>пункт 2</li>" +
                "</ul>");
        }

        [Test]
        public void Render_InlineFormattingInsideBlocks_WorksCorrectly()
        {
            var input = "# Жирный __текст__\n" +
                "- _Курсив_ в списке";

            var result = md.Render(input);

            result.Should().Contain("<h1>Жирный <strong>текст</strong></h1>");
            result.Should().Contain("<li><em>Курсив</em> в списке</li>");
        }

        [Test]
        public void Render_EmptyLines_HandlesGracefully()
        {
            var input = "# Заголовок\n\n- пункт";

            var result = md.Render(input);

            result.Should().Contain("<h1>Заголовок</h1>");
            result.Should().Contain("<ul><li>пункт</li></ul>");
        }

        [Test]
        public void Render_NullOrEmptyInput_ReturnsEmptyString()
        {
            md.Render(null).Should().BeEmpty();
            md.Render("").Should().BeEmpty();
        }
    }
}
