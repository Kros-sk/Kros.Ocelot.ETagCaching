using Ocelot.DownstreamRouteFinder.UrlMatcher;
using System.Text;

namespace Kros.Ocelot.ETagCaching;

internal static class TagsHelper
{
    public static HashSet<string> GetTags(string[] tagTemplates, List<PlaceholderNameAndValue> templatePlaceholderNameAndValues)
    {
        var tags = new HashSet<string>();
        var tagSb = new StringBuilder();
        foreach (var tagTemplate in tagTemplates)
        {
            tagSb.Append(tagTemplate);
            foreach (var template in templatePlaceholderNameAndValues)
            {
                tagSb.Replace(template.Name, template.Value);
            }
            tags.Add(tagSb.ToString());
            tagSb.Length = 0;
        }

        return tags;
    }
}
