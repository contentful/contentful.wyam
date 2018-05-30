using System;
using Wyam.Common.Documents;
using System.Collections.Generic;
using System.Linq;
using Wyam.Common.Execution;
using System.Collections;
using System.IO;
using Wyam.Common.Caching;
using Wyam.Common.Configuration;
using Wyam.Common.IO;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Xunit;
using Contentful.Core;
using Contentful.Core.Models;
using Contentful.Core.Search;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Contentful.Core.Models.Management;
using Newtonsoft.Json.Linq;

namespace Contentful.Wyam.Tests
{
    public class ContentfulTests
    {
        [Fact]
        public void CallingContentfulShouldReturnCorrectNumberOfDocuments()
        {
            //Arrange
            var mockClient = new Mock<IContentfulClient>();
            mockClient.Setup(c => c.GetSpace(default(CancellationToken)))
                .ReturnsAsync(
                new Space()
                {
                    SystemProperties = new SystemProperties
                    {
                        Id = "467"
                    },
                    Locales = new List<Locale>()
                    {
                        new Locale()
                        {
                            Code = "en-US",
                            Default = true
                        }
                    }
                });

            var collection = new ContentfulCollection<Entry<dynamic>>()
            {
                Items = new List<Entry<dynamic>>()
            {
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="123" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="3456" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="62365" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="tw635" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="uer46" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="jy456" } },
            },

                IncludedAssets = new List<Asset>(),
                IncludedEntries = new List<Entry<dynamic>>()
            };
            mockClient.Setup(c => c.GetEntries(It.IsAny<QueryBuilder<Entry<dynamic>>>(), default(CancellationToken)))
            .ReturnsAsync(collection);

            var mockContext = new Mock<IExecutionContext>();


            var contentful = new Contentful(mockClient.Object).WithContentField("body");

            //Act
            var res = contentful.Execute(new List<IDocument>(), mockContext.Object);

            //Assert
            Assert.Equal(6, res.Count());
        }

        [Fact]
        public void CallingContentfulRecursivelyShouldReturnCorrectNumberOfDocuments()
        {
            //Arrange
            var mockClient = new Mock<IContentfulClient>();
            mockClient.Setup(c => c.GetSpace(default(CancellationToken)))
                .ReturnsAsync(
                new Space()
                {
                    SystemProperties = new SystemProperties
                    {
                        Id = "467"
                    },
                    Locales = new List<Locale>()
                    {
                        new Locale()
                        {
                            Code = "en-US",
                            Default = true
                        }
                    }
                });

            var collection = new ContentfulCollection<Entry<dynamic>>()
            {
                Items = new List<Entry<dynamic>>()
            {
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="123" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="3456" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="62365" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="tw635" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="uer46" } },
                new Entry<dynamic>() { Fields = new JObject(), SystemProperties = new SystemProperties() { Id="jy456" } },
            },

                IncludedAssets = new List<Asset>(),
                IncludedEntries = new List<Entry<dynamic>>(),
                Total = 24
            };
            var callCount = 0;

            mockClient.Setup(c => c.GetEntries(It.IsAny<QueryBuilder<Entry<dynamic>>>(), default(CancellationToken)))
            .ReturnsAsync(() => {

                if(callCount == 4)
                {
                    return new ContentfulCollection<Entry<dynamic>>() {
                        Items = new List<Entry<dynamic>>(),
                        IncludedAssets = new List<Asset>(),
                        IncludedEntries = new List<Entry<dynamic>>(),
                        Total = 24
                    };
                }
                callCount++;
                return collection;
            });

            var mockContext = new Mock<IExecutionContext>();


            var contentful = new Contentful(mockClient.Object).WithContentField("body").WithRecursiveCalling().WithLimit(6);

            //Act
            var res = contentful.Execute(new List<IDocument>(), mockContext.Object);

            //Assert
            Assert.Equal(24, res.Count());
            mockClient.Verify(c => c.GetEntries(It.IsAny<QueryBuilder<Entry<dynamic>>>(), default(CancellationToken)), Times.Exactly(5));
        }
    }    
}
