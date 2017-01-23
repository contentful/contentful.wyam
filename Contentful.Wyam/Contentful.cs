using Contentful.Core;
using Contentful.Core.Models;
using Contentful.Core.Search;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.Modules;

namespace Contentful.Wyam
{
    public class Contentful : IModule
    {
        private readonly IContentfulClient _client;
        private string _contentField = "";
        private string _contentTypeId = "";
        private string _locale = null;
        private int _includes = 1;

        public Contentful(string deliveryKey, string spaceId): this(deliveryKey, spaceId, false)
        {

        }

        public Contentful(string deliveryKey, string spaceId, bool usePreview)
        {
            var httpClient = new HttpClient();
            _client = new ContentfulClient(httpClient, deliveryKey, spaceId, usePreview);
        }

        public Contentful(IContentfulClient client)
        {
            _client = client;
        }

        public Contentful WithContentField(string field)
        {
            _contentField = field;
            return this;
        }

        public Contentful WithContentType(string contentTypeId)
        {
            _contentTypeId = contentTypeId;
            return this;
        }

        public Contentful WithLocale(string locale)
        {
            _locale = locale;
            return this;
        }

        public Contentful WithIncludes(int includes)
        {
            _includes = includes;
            return this;
        }


        public IEnumerable<IDocument> Execute(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            var space = _client.GetSpaceAsync().Result;
            var queryBuilder = CreateQueryBuilder();
            var entries = _client.GetEntriesCollectionAsync(queryBuilder).Result;
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

                    var metaData = items.Select(c => new KeyValuePair<string, object>(c.Key, c.Value[localeCode])).ToList();
                    metaData.Add(new KeyValuePair<string, object>("ContentfulId", $"{entry.SystemProperties.Id}"));
                    metaData.Add(new KeyValuePair<string, object>("ContentfulLocale", localeCode));
                    metaData.Add(new KeyValuePair<string, object>("ContentfulIncludedAssets", includedAssets));
                    metaData.Add(new KeyValuePair<string, object>("ContentfulIncludedEntries", includedEntries));
                    var doc = context.GetDocument(content, metaData);

                    yield return doc;
                }
            }
        }

        private QueryBuilder<Entry<dynamic>> CreateQueryBuilder() {
            var queryBuilder = QueryBuilder<Entry<dynamic>>.New.LocaleIs("*").Include(_includes);

            if (!string.IsNullOrEmpty(_contentTypeId))
            {
                queryBuilder.ContentTypeIs(_contentTypeId);
            }

            return queryBuilder;
        }
    }
}
