using System;
using System.Collections.Generic;
using System.Text;
using Wyam.Common.Documents;
using Wyam.Common.Meta;
using Contentful.Core.Images;
using Contentful.Core.Models;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Contentful.Wyam
{
    /// <summary>
    /// Helper extension methods for IDocument.
    /// </summary>
    public static class IDocumentExtensions
    {
        /// <summary>
        /// Gets an asset from the ContentfulIncludedAssets collection.
        /// </summary>
        /// <param name="doc">The IDocument.</param>
        /// <param name="token">The Json token representing the asset.</param>
        /// <returns>The found asset or null.</returns>
        public static Asset GetIncludedAsset(this IDocument doc, JToken token)
        {
            if (token["sys"] == null || token["sys"]["id"] == null)
            {
                return null;
            }

            return GetIncludedAssetById(doc, token["sys"]["id"].ToString());
        }

        /// <summary>
        /// Gets an asset from the ContentfulIncludedAssets collection.
        /// </summary>
        /// <param name="doc">The IDocument.</param>
        /// <param name="id">The id of the asset.</param>
        /// <returns>The found asset or null.</returns>
        public static Asset GetIncludedAssetById(this IDocument doc, string id)
        {
            var assets = doc.List<Asset>(ContentfulKeys.IncludedAssets);

            return assets.FirstOrDefault(c => c.SystemProperties.Id == id);
        }

        /// <summary>
        /// Gets an entry from the ContentfulIncludedEntries collection.
        /// </summary>
        /// <param name="doc">The IDocument.</param>
        /// <param name="token">The Json token representing the entry.</param>
        /// <returns>The found entry or null.</returns>
        public static Entry<dynamic> GetIncludedEntry(this IDocument doc, JToken token)
        {
            if (token["sys"] == null || token["sys"]["id"] == null)
            {
                return null;
            }

            return GetIncludedEntryById(doc, token["sys"]["id"].ToString());
        }

        /// <summary>
        /// Gets an entry from the ContentfulIncludedEntries collection.
        /// </summary>
        /// <param name="doc">The IDocument.</param>
        /// <param name="id">The id of the entry.</param>
        /// <returns>The found entry or null.</returns>
        public static Entry<dynamic> GetIncludedEntryById(this IDocument doc, string id)
        {
            var entries = doc.List<Entry<dynamic>>(ContentfulKeys.IncludedEntries);

            return entries.FirstOrDefault(c => c.SystemProperties.Id == id);
        }

        /// <summary>
        /// Creates an image tag for an asset.
        /// </summary>
        /// <param name="doc">The IDocument.</param>
        /// <param name="token">The Json token representing the asset.</param>
        /// <param name="alt">The alt text of the image. Will default to the title of the asset if null.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="jpgQuality">The quality of the image.</param>
        /// <param name="resizeBehaviour">How the image should resize to conform to the width and height.</param>
        /// <param name="format">The format of the image, jpg,png or webp.</param>
        /// <param name="cornerRadius">The corner radius of the image.</param>
        /// <param name="focus">The focus area of the image when resizing.</param>
        /// <param name="backgroundColor">The background color of any padding that is added to the image.</param>
        /// <returns>The image tag as a string.</returns>
        public static string ImageTagForAsset(this IDocument doc, JToken token, string alt = null,
            int? width = null, int? height = null, int? jpgQuality = null, ImageResizeBehaviour resizeBehaviour = ImageResizeBehaviour.Default,
            ImageFormat format = ImageFormat.Default, int? cornerRadius = 0, ImageFocusArea focus = ImageFocusArea.Default, string backgroundColor = null)
        {
            if (token["sys"] == null || token["sys"]["id"] == null)
            {
                return null;
            }

            return ImageTagForAsset(doc, token["sys"]["id"].ToString(), alt, width, height, jpgQuality, resizeBehaviour, format, cornerRadius, focus, backgroundColor);
        }

        /// <summary>
        /// Creates an image tag for an asset.
        /// </summary>
        /// <param name="doc">The IDocument.</param>
        /// <param name="assetId">The id of the asset.</param>
        /// <param name="alt">The alt text of the image. Will default to the title of the asset if null.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="jpgQuality">The quality of the image.</param>
        /// <param name="resizeBehaviour">How the image should resize to conform to the width and height.</param>
        /// <param name="format">The format of the image, jpg,png or webp.</param>
        /// <param name="cornerRadius">The corner radius of the image.</param>
        /// <param name="focus">The focus area of the image when resizing.</param>
        /// <param name="backgroundColor">The background color of any padding that is added to the image.</param>
        /// <returns>The image tag as a string.</returns>
        public static string ImageTagForAsset(this IDocument doc, string assetId, string alt = null,
            int? width = null, int? height = null, int? jpgQuality = null, ImageResizeBehaviour resizeBehaviour = ImageResizeBehaviour.Default,
            ImageFormat format = ImageFormat.Default, int? cornerRadius = 0, ImageFocusArea focus = ImageFocusArea.Default, string backgroundColor = null)
        {
            var asset = doc.List<Asset>(ContentfulKeys.IncludedAssets)?.FirstOrDefault(c => c.SystemProperties.Id == assetId);

            if (asset == null)
            {
                return string.Empty;
            }

            var locale = doc.Get<string>(ContentfulKeys.EntryLocale);

            var imageUrlBuilder = ImageUrlBuilder.New();

            if (width.HasValue)
            {
                imageUrlBuilder.SetWidth(width.Value);
            }

            if (height.HasValue)
            {
                imageUrlBuilder.SetHeight(height.Value);
            }

            if (jpgQuality.HasValue)
            {
                imageUrlBuilder.SetJpgQuality(jpgQuality.Value);
            }

            if (cornerRadius.HasValue)
            {
                imageUrlBuilder.SetCornerRadius(cornerRadius.Value);
            }

            imageUrlBuilder.SetResizingBehaviour(resizeBehaviour).SetFormat(format).SetFocusArea(focus).SetBackgroundColor(backgroundColor);

            if (alt == null && !string.IsNullOrEmpty(asset.TitleLocalized[locale]))
            {
                alt = asset.TitleLocalized[locale];
            }

            return $@"<img src=""{asset.FilesLocalized[locale].Url + imageUrlBuilder.Build()}"" alt=""{alt}"" height=""{height}"" width=""{width}"" />";
        }
    }
}
