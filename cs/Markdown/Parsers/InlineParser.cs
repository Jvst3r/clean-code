using Markdown.Models;
using Markdown.Models.Interfaces;
using Markdown.Models.TagInfos;
using Markdown.Models.TagTokens;
using System.Text;
using System.Linq;

namespace Markdown
{
    /// <summary>
    /// Класс проходит посимвольно по строке и определяет текст или особые конструкции (курсив, выделение)
    /// </summary>
    internal class InlineParser : IInlineParser
    {
        private readonly List<PairedTagInfo> pairedTags; 
        private readonly SingleTagInfo headingTag;
        private readonly SingleTagInfo listItemTag;
        private readonly CascadTagInfo listTag;
        public InlineParser()
        {
            pairedTags = TagRegistry.GetPairedTags().ToList();
            headingTag = TagRegistry.GetSingleTags().Single(t => t.MarkdownPrefix == "# ");
            listItemTag = TagRegistry.GetSingleTags().Single(t => t.MarkdownPrefix == "- ");
            listTag = TagRegistry.GetCascadTag();

        }



        public List<IToken> ParseToTokens(string line)
        {
            if (line == null)
                throw new ArgumentNullException();

            if (line.Length == 0)
                return new List<IToken>();

            return ParseSegment(line, 0, line.Length);
        }

        //TO DO x2
        //метод получился огромный, его бы по хорошему потом декомпозировать, но мне сейчас западло 
        public List<IToken> ParseLines(string[] lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            var result = new List<IToken>();

            var index = 0;

            while (index < lines.Length)
            {
                var line = lines[index];

                //Список ul
                if (line.StartsWith(listItemTag.MarkdownPrefix))
                {
                    var listItems = new List<IToken>();

                    //
                    while (index < lines.Length && lines[index].StartsWith(listItemTag.MarkdownPrefix))
                    {
                        var content = lines[index].Substring(listItemTag.MarkdownPrefix.Length);

                        var children = ParseToTokens(content);

                        listItems.Add(new TagToken(listItemTag, children));

                        index++;
                    }

                    //создаем общий токен-контейнер
                    result.Add(new CascadTagToken(listTag, listItems));

                    //сохранение перевода строки
                    if (index < lines.Length)
                        result.Add(new TextToken("\n"));

                    continue;
                }

                //заголовки
                if (line.StartsWith(headingTag.MarkdownPrefix))
                {
                    string content = line.Substring(headingTag.MarkdownPrefix.Length);

                    var children = ParseToTokens(content);

                    result.Add(new TagToken(headingTag, children));
                }
                else
                {
                    //обычная строка
                    result.AddRange(ParseToTokens(line));
                }

                index++;

                if (index < lines.Length)
                    result.Add(new TextToken("\n"));
            }

            return result;
        }
        //TO DO
        //метод получился огромный, его бы по хорошему потом декомпозировать, но мне сейчас западло 
        internal List<IToken> ParseSegment(string line, int start, int end, PairedTagInfo tag = null)
        {
            var tokens = new List<IToken>();
            var text = new StringBuilder();

            for (int i = start; i < end; i++)
            {
                if (IsEscaping(line, i)) //ПРОВЕРКА ЭКРАНИРОВАНИЯ
                {
                    if (i + 1 < end) //если не конец строки
                    {
                        text.Append(line[i + 1]); //пропуск знака '\'
                        i++;
                    }
                    else
                    {
                        text.Append('\\');
                    }

                        continue;
                }

                //сюда мы придем при рекурсии быстрее, чем на проверку открывающегося тега
                if (tag != null && IsValidEndingTag(line, i, end, tag)) //ПРОВЕРКА ЗАКРЫВАЮЩЕГО ТЕГА
                {
                    if (text.Length > 0)
                    {
                        tokens.Add(new TextToken(text.ToString()));
                        text.Clear();
                    }
                    return tokens;
                }

                var openingTag = FindOpeningTag(line, i, end, tag); //ПРОВЕРКА ОТКРЫВАЮЩЕГО ТЕГА
                if (openingTag != null)
                {

                    var contentStart = i + openingTag.Length;
                    var newline = line.IndexOf('\n', contentStart, Math.Max(0, end - contentStart));
                    var contentEnd = newline >= 0 ? newline : end;

                    var closingIndex = FindClosingTag(line, contentStart, contentEnd, openingTag);

                    //if (closingIndex >= 0) //из-за неверного условия __ давало tagToken без контента
                    if (closingIndex > contentStart)
                    {
                        if (text.Length > 0)
                        {
                            tokens.Add(new TextToken(text.ToString()));
                            text.Clear();
                        }

                        var innerTokens = ParseSegment(line, i + openingTag.Length, closingIndex, openingTag);
                        tokens.Add(new TagToken(openingTag, innerTokens));

                        i = closingIndex + openingTag.Length - 1;
                        continue;
                    }

                }
                text.Append(line[i]);
            }

            if (text.Length > 0)
                tokens.Add(new TextToken(text.ToString()));

            return tokens;
        }

        private bool IsEscaping(string line, int index) => line[index] == '\\';

        private bool IsValidStartingTag(string line, int index, int end, PairedTagInfo tag)
        {
            for (var i = 0; i < tag.Length; i++)
                if (index + i >= end || line[index + i] != tag.MarkdownSymbol[i])
                    return false;

            if (IsPartOfLongerTag(line, index, end, tag))
                return false;

            var after = index + tag.Length;
            return after < end &&
                   !char.IsWhiteSpace(line[after]) &&
                   !char.IsDigit(line[after]);
        }

        private PairedTagInfo FindOpeningTag(string line, int index, int end, PairedTagInfo parentTag)
        {
            foreach (var tag in pairedTags)
            {
                if (parentTag != null && tag.Length > parentTag.Length)
                    continue;


                if (IsValidStartingTag(line, index, end, tag))
                    return tag;
            }
            return null;
        }

        private bool IsCrossed(string line, int index, int end, PairedTagInfo targetTag)
        {
            var otherTag = pairedTags.FirstOrDefault(t => t.MarkdownSymbol != targetTag.MarkdownSymbol);

            if (otherTag == null)
                return false;

            for (var i = index; i < end; i++)
            {
                if (IsEscaping(line, i))
                {
                    i++;
                    continue;
                }

                if (StartsMarkerAt(line, i, end, otherTag))
                {
                    var otherClose = -1;

                    for (var j = i + otherTag.Length; j < end; j++)
                    {
                        if (IsEscaping(line, j))
                        {
                            j++;
                            continue;
                        }

                        if (StartsMarkerAt(line, j, end, otherTag))
                        {
                            otherClose = j;
                            break;
                        }
                    }
                    if (otherClose == -1)
                    {
                        return true;
                    }

                    i = otherClose + otherTag.Length - 1;
                    continue;
                }
            }
            return false;
        }

        private int FindClosingTag(string line, int index, int end, PairedTagInfo tag)
        {
            for (var i = index; i < end; i++)
            {
                if (IsEscaping(line, i))
                {
                    i++;
                    continue;
                }

                if (IsValidEndingTag(line, i, end, tag))
                {
                    if (IsCrossed(line, index, i, tag))
                    {
                        continue;
                    }
                    return i;
                }
            }
            return -1;
        }

        private bool IsValidEndingTag(string line, int index, int end, PairedTagInfo tag)
        {
            for (var i = 0; i < tag.Length; i++)
                if (index + i >= end || line[index + i] != tag.MarkdownSymbol[i])
                    return false;

            if (IsPartOfLongerTag(line, index, end, tag))
                return false;

            //var afterIndex = index + tag.Length;
           // if (afterIndex < end && char.IsLetterOrDigit(line[afterIndex]))
             //   return false;

            var beforeIndex = index - 1;
            return beforeIndex >= 0 && !char.IsWhiteSpace(line[beforeIndex]);
        }

        private bool IsPartOfLongerTag(string line, int index, int end, PairedTagInfo tag)
        {
            foreach (var longer in pairedTags)
            {
                if (longer.Length <= tag.Length)
                    break;

                if (StartsMarkerAt(line, index, end, longer))
                    return true;

                if (StartsMarkerAt(
                    line, 
                    index - (longer.Length - tag.Length), //индекс старта тега
                    end, longer))
                    return true;
            }
            return false;
        }

        private bool StartsMarkerAt(string line, int start, int end, PairedTagInfo tag)
        {
            if (start < 0)
                return false;



            for (var i = 0; i < tag.Length; i++)
                if (start + i >= end || line[start + i] != tag.MarkdownSymbol[i])
                    return false;

            return true;
        }

    }
}