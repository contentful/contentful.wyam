using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contentful.Wyam
{
    /// <summary>
    /// Class containing meta-data keys for the Contentful documents.
    /// </summary>
    public static class ContentfulKeys
    {
        public static string EntryId => "ContentfulId";
        public static string EntryLocale => "ContentfulLocale";
        public static string IncludedAssets => "ContentfulIncludedAssets";
        public static string IncludedEntries => "ContentfulIncludedEntries";
    }
}
