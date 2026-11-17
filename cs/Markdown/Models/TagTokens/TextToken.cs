using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Markdown.Models.Interfaces;

namespace Markdown.Models.TagTokens
{
    public class TextToken : IToken
    {
        public string Content { get; }

        public TextToken(string content) 
        { 
            Content = content; 
        }
    }
}
