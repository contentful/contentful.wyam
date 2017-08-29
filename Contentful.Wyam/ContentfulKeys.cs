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
        /// <summary>
        /// The id of the Entry. Note that this is not guaranteed to unique as there will be one document created per requested locale of the Entry.
        /// A unique combination would be <see cref="EntryId"/> and <see cref="EntryLocale"/>. The string version of this key is "ContentfulId".
        /// </summary>
        /// <type><see cref="string"/></type>
        public const string EntryId = "ContentfulId";

        /// <summary>
        /// The locale of the Entry. The string version of this key is "ContentfulLocale".
        /// </summary>
        /// <type><see cref="string"/></type>
        public const string EntryLocale = "ContentfulLocale";

        /// <summary>
        /// The included assets of the Entry. Refer to the Contentful .NET SDK documentation for more details.
        /// The string version of this key is "ContentfulIncludedAssets".
        /// </summary>
        /// <type><c>IEnumerable&lt;Asset&gt;</c></type>
        public const string IncludedAssets = "ContentfulIncludedAssets";

        /// <summary>
        /// The included referenced entries of the Entry. Refer to the Contentful .NET SDK documentation for more details.
        /// The string version of this key is "ContentfulIncludedEntries".
        /// </summary>
        /// <type><c>IEnumerable&lt;Entry&lt;dynamic&gt;&gt;</c></type>
        public const string IncludedEntries = "ContentfulIncludedEntries";
    }
}
