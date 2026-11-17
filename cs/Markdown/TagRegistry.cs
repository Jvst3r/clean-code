using Markdown.Models.TagInfos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markdown
{
    internal static class TagRegistry
    {

        private static readonly List<PairedTagInfo> pairedTags = new List<PairedTagInfo>
        {
                new PairedTagInfo("__", "strong"),
                new PairedTagInfo("_", "em")
        };

        private static readonly  List<SingleTagInfo> singleTags = new List<SingleTagInfo>
        {
                new SingleTagInfo("# ", "h1"),
                new SingleTagInfo("- ", "li")
        };

        private static CascadTagInfo listTag = new CascadTagInfo();

        public static IEnumerable<PairedTagInfo> GetPairedTags()
                => pairedTags.OrderByDescending(t => t.Length);

        public static IEnumerable<SingleTagInfo> GetSingleTags()
            => singleTags;

        public static CascadTagInfo GetCascadTag() => listTag;
    }
}
