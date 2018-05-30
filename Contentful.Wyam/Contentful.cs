using Contentful.Core;
using Contentful.Core.Errors;
using Contentful.Core.Models;
using Contentful.Core.Search;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.Modules;

namespace Contentful.Wyam
{
    /// <summary>
    /// Fetch content from Contentful and create new documents using the fields as metadata.
    /// </summary>
    /// <remarks>
    /// For each Entry in Contentful one output document per requested locale will be created. For each output document every field of the Entry will be available
    /// as metadata. In addition a number of other metadata properties will be set for each document.
    /// </remarks>
    /// <metadata cref="ContentfulKeys.EntryId" usage="Output" />
    /// <metadata cref="ContentfulKeys.EntryLocale" usage="Output" />
    /// <metadata cref="ContentfulKeys.IncludedAssets" usage="Output" />
    /// <metadata cref="ContentfulKeys.IncludedEntries" usage="Output" />
    /// <category>Content</category>
    public class Contentful : IModule
    {
        private readonly IContentfulClient _client;
        private string _contentField = "";
        private string _contentTypeId = "";
        private string _locale = null;
        private int _includes = 1;
        private int _limit = 100;
        private int _skip = 0;
        private bool _recursive = false;

        /// <summary>
        /// Calls the Contentful API using the specified deliverykey and spaceId.
        /// </summary>
        /// <param name="deliveryKey">The Contentful Content Delivery API key.</param>
        /// <param name="spaceId">The id of the space in Contentful from which to fetch content.</param>
        public Contentful(string deliveryKey, string spaceId): this(deliveryKey, spaceId, false)
        {

        }

        /// <summary>
        /// Calls the Contentful API using the specified deliverykey and spaceId.
        /// </summary>
        /// <param name="deliveryKey">The Contentful Content Delivery API key.</param>
        /// <param name="spaceId">The id of the space in Contentful from which to fetch content.</param>
        /// <param name="usePreview">Whether or not to use the Contentful Preview API. Note that if the preview API is used a preview API key must also be specified.</param>
        public Contentful(string deliveryKey, string spaceId, bool usePreview)
        {
            var httpClient = new HttpClient();
            _client = new ContentfulClient(httpClient, deliveryKey, "", spaceId, usePreview);
        }

        /// <summary>
        /// Calls the Contentful API using the specified IContentfulClient.
        /// </summary>
        /// <param name="client">The IContentfulClient to use when calling the Contentul API.</param>
        public Contentful(IContentfulClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client), "IContentful client cannot be null.");
        }

        /// <summary>
        /// Specifies which field of the Contentful entries that should be used as content for the documents created.
        /// </summary>
        /// <param name="field">The id of the field in Contentful.</param>
        public Contentful WithContentField(string field)
        {
            _contentField = field;
            return this;
        }

        /// <summary>
        /// Specifies that only entries of a specific content type should be fetched.
        /// </summary>
        /// <param name="contentTypeId">The id of the content type in Contentful.</param>
        public Contentful WithContentType(string contentTypeId)
        {
            _contentTypeId = contentTypeId;
            return this;
        }

        /// <summary>
        /// Specifies that only entries of a specific locale should be fetched.
        /// </summary>
        /// <Remarks>
        /// Note that by default only the default locale is fetched. If a specific locale is specified only that locale will be fetched.
        /// To fetch all locales use "*".
        /// </Remarks>
        /// <param name="locale">The locale code of the locale in Contentful. E.g. "en-US".</param>
        public Contentful WithLocale(string locale)
        {
            _locale = locale;
            return this;
        }

        /// <summary>
        /// Specifies the levels of included assets and entries that should be resolved when calling Contentful.
        /// </summary>
        /// <Remarks>
        /// Note that the maximum number of levels resolved are 10 and the default is 1.
        /// </Remarks>
        /// <param name="includes">The number of levels of references that should be resolved.</param>
        public Contentful WithIncludes(int includes)
        {
            _includes = includes;
            return this;
        }

        /// <summary>
        /// Specifies the maximum number of entries to fetch from Contentful.
        /// </summary>
        /// <param name="limit">The maximum number of entries.</param>
        public Contentful WithLimit(int limit)
        {
            _limit = limit;
            return this;
        }

        /// <summary>
        /// Specifies the number of entries to skip when fetching from Contentful.
        /// </summary>
        /// <param name="skip">The number of entries to skip.</param>
        public Contentful WithSkip(int skip)
        {
            _skip = skip;
            return this;
        }

        /// <summary>
        /// Specifies that the Contentful API should be called recursively until all entries have been fetched.
        /// </summary>
        /// <remarks>
        /// Note that this might result in several calls being made to the Contentful API. Ratelimits may apply.
        /// </remarks>
        public Contentful WithRecursiveCalling()
        {
            _recursive = true;
            return this;
        }

        /// <inheritdoc />
        public IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            Space space = null;

            try
            {
                space = _client.GetSpace().Result;
            }
            catch(AggregateException ae)
            {
                ae.Handle((ex) => {

                    if(ex is ContentfulException)
                    {
                        Trace.TraceError($"Error when fetching space from Contentful: {ex.Message}");
                        Trace.TraceError($"Details: {(ex as ContentfulException).ErrorDetails.Errors}");
                        Trace.TraceError($"ContentfulRequestId:{(ex as ContentfulException).RequestId}");
                    }

                    return false;
                });
            }

            var queryBuilder = CreateQueryBuilder();
            ContentfulCollection<Entry<dynamic>> entries = null;

            try
            {
                entries = _client.GetEntries(queryBuilder).Result;
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) => {

                    if (ex is ContentfulException)
                    {
                        Trace.TraceError($"Error when fetching entries from Contentful: {ex.Message}");
                        Trace.TraceError($"Details: {(ex as ContentfulException).ErrorDetails.Errors}");
                        Trace.TraceError($"ContentfulRequestId:{(ex as ContentfulException).RequestId}");
                    }

                    return false;
                });
            }

            if (_recursive && entries.Total > entries.Items.Count())
            {
                entries = FetchEntriesRecursively(entries);
            }

            var includedAssets = entries.IncludedAssets;
            var includedEntries = entries.IncludedEntries;

            var locales = space.Locales.Where(l => l.Default);

            if(_locale == "*")
            {
                locales = space.Locales;
            }
            else if (!string.IsNullOrEmpty(_locale))
            {
                locales = space.Locales.Where(l => l.Code == _locale);
            }
            
            if (!locales.Any())
            {
                //Warn or throw here?
                throw new ArgumentException($"Locale {_locale} not found for space. Note that locale codes are case-sensitive.");
            }

            foreach (var entry in entries)
            {
                foreach (var locale in locales)
                {
                    var localeCode = locale.Code;

                    var items = (entry.Fields as IEnumerable<KeyValuePair<string, JToken>>).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    
                    var content = items.ContainsKey(_contentField) ? items[_contentField][localeCode].ToString() : "No content";

                    if (string.IsNullOrEmpty(_contentField))
                    {
                        content = "";
                    }

                    var metaData = items.Select(c => new KeyValuePair<string, object>(c.Key, c.Value[localeCode])).ToList();
                    metaData.Add(new KeyValuePair<string, object>(ContentfulKeys.EntryId, $"{entry.SystemProperties.Id}"));
                    metaData.Add(new KeyValuePair<string, object>(ContentfulKeys.EntryLocale, localeCode));
                    metaData.Add(new KeyValuePair<string, object>(ContentfulKeys.IncludedAssets, includedAssets));
                    metaData.Add(new KeyValuePair<string, object>(ContentfulKeys.IncludedEntries, includedEntries));
                    var doc = context.GetDocument(context.GetContentStream(content), metaData);

                    yield return doc;
                }
            }
        }

        private QueryBuilder<Entry<dynamic>> CreateQueryBuilder() {
            var queryBuilder = QueryBuilder<Entry<dynamic>>.New.LocaleIs("*").Include(_includes)
                .OrderBy(SortOrderBuilder<Entry<dynamic>>.New(f => f.SystemProperties.CreatedAt).Build())
                .Limit(_limit).Skip(_skip);

            if (!string.IsNullOrEmpty(_contentTypeId))
            {
                queryBuilder.ContentTypeIs(_contentTypeId);
            }

            return queryBuilder;
        }

        private ContentfulCollection<Entry<dynamic>> FetchEntriesRecursively(ContentfulCollection<Entry<dynamic>> entries)
        {
            var entryList = new List<Entry<dynamic>>();
            var includedAssets = new List<Asset>();
            var includedEntries = new List<Entry<dynamic>>();
            var collection = new ContentfulCollection<Entry<dynamic>>()
            {
                Items = entries.Items
            };
            entryList.AddRange(entries.Items);
            includedAssets.AddRange(entries.IncludedAssets);
            includedEntries.AddRange(entries.IncludedEntries);

            _skip += _limit;

            while (collection.Count() == _limit)
            {
                collection = _client.GetEntries(CreateQueryBuilder()).Result;

                entryList.AddRange(collection.Items);
                includedAssets.AddRange(collection.IncludedAssets.Where(c => !includedAssets.Any(a => a.SystemProperties.Id == c.SystemProperties.Id)));
                includedEntries.AddRange(collection.IncludedEntries.Where(c => !includedAssets.Any(a => a.SystemProperties.Id == c.SystemProperties.Id)));

                _skip += _limit;
            }

            collection.Items = entryList;
            collection.IncludedAssets = includedAssets;
            collection.IncludedEntries = includedEntries;

            return collection;
        }
    }
}
