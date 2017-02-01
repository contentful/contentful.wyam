# contentful.wyam
Contentful.wyam is a module to the [Wyam static site generator](https://wyam.io) allowing you to fetch content from the Contentful API. It is powered 
by the [Contentful .NET SDK](https://github.com/contentful/contentful.net).

## Installation

Add the following nuget package to your `config.wyam` file.

```
#n -p Contentful.Wyam
```

Note that the `-p` switch is necessary since this package is still in pre-release.

## Usage

Adding the package above gives you access to the `Contentful` module that can be used to fetch content. In your
`config.wyam` you can add it to your pipeline in this fashion.

```csharp
Pipelines.Add("Your pipeline", Contentful("<delivery_api_key>", "<space_id>"));
```

There are a number of fluent methods available to further customize what content to fetch.

Use `WithContentfield` to specify which field in your content should be used as content for your Wyam documents.

```csharp
Contentful("<delivery_api_key>", "<space_id>").WithContentField("body");
````

Use `WithContentType` to specify that only content of a specific content type should be pulled from Contentful.

```csharp
Contentful("<delivery_api_key>", "<space_id>").WithContentType("<content_type_id>");
````

Use `WithLocale` to specify that only content of a specific locale should be pulled from Contentful. By default only
content of the default locale will be fetched.

```csharp
Contentful("<delivery_api_key>", "<space_id>").WithLocale("en-GB");
````

If you want to fetch all locales, use `*` as your locale.

```csharp
Contentful("<delivery_api_key>", "<space_id>").WithLocale("*");
````

Note that this will fetch multiple copies of the same Entry. One for each locale.

Use `WithIncludes` to specify the number of levels of referenced content to fetch. Default is to fetch 1 level of referenced content. See [the Contentful documentation](https://www.contentful.com/developers/docs/references/content-delivery-api/#/reference/links) 
for more information on referenced content.

```csharp
Contentful("<delivery_api_key>", "<space_id>").WithIncludes(3);
````

Use `WithLimit` and `WithSkip` to specify the maximum number of items to fetch and to skip an arbitrary number of items. Highly useful if you 
wish to paginate your content.

```csharp
Contentful("<delivery_api_key>", "<space_id>").WithSkip(10).WithLimit(10);
````

Use `WithRecursiveCalling` if you have multple pages of content that you need to fetch. Note that this might result in several
calls being made to the Contentful API. Ratelimits may apply.

```csharp
Contentful("<delivery_api_key>", "<space_id>").WithRecursiveCalling();
```

Once the `Contentful` module has run it will output all of your entries in Contentful as Wyam documents. Available as metadata will be all of the fields of the entry.
Here's an example in a razor file.

```csharp
<div>
    Model.Get("productName")
</div>
```

This would fetch the `productName` field from the current document.

There's also four specific metadata properties that are always available: `ContentfulId`, `ContentfulLocale`, `ContentfulIncludedAssets` and `ContentfulIncludedEntries`.

`ContentfulId` and `ContentfulLocale` are simply the id and the locale of the entry of the current document in Contentful.

The `ContentfulIncludedAssets` and `ContentfulIncludedEntries` are two collections of referenced entries and assets. These correspond to the `includes` section of the json
response returned from Contentful and can be used to input images, links to PDFs or other entries in your documents.

To use them directly as metadata means alot of casting back and can result in quite bloated output.

```csharp
<img src="@(Model.List<Asset>("ContentfulIncludedAssets").First(c => c.SystemProperties.Id == (Model.Get("featuredImage") as JToken)["sys"]["id"].ToString()).FilesLocalized["en-US"].Url)" />
```

The contentful.wyam packages has a number of helper extension methods that are better suited for the task.

```csharp
@Model.ImageTagForAsset(Model.Get<JToken>("featuredImage")) 
//This will output an image tag as in the bloated example above.
```

The image tag extension method also allows you to leverage the entire powerful Contentful image API to manipulate your images. It has the following optional parameters:

* `alt` &mdash; the alt text of the image, will default to the title of the Asset in Contentful if available.
* `width` &mdash; the width of the image. 
* `height` &mdash; the height of the image. 
* `jpgQuality` &mdash; the quality of the image if it is a JPG. 
* `resizeBehaviour` &mdash; how the image should be resized if needed. 
* `format` &mdash; the format of the image, supported are JPG, PNG or WEBP. 
* `cornerRadius` &mdash; the radius of the corners. 
* `focus` &mdash; the focus area of the image. 
* `backgroundColor` &mdash; the background color of the image.

For more information on how to use the Contentful image API, refer to [the official documentation](https://www.contentful.com/developers/docs/references/images-api/).

There are also extensions methods available to get an `Asset` or `Entry<dynamic>` directly.

```csharp
@Model.GetIncludedAsset(Model.Get<JToken>("topImage")) //Accepts a JToken, extracts the id and returns an Asset from the ContentfulIncludedAssets.
@Model.GetIncludedAssetById("<asset_id>") //Returns an asset from ContentfulIncludedAssets by the specified id.

@Model.GetIncludedEntry(Model.Get<JToken>("referencedEntry")) //Accepts a JToken, extracts the id and returns an Entry<dynamic> from the ContentfulIncludedEntries.
@Model.GetIncludedEntryById("<entry_id>") //Returns an entry from ContentfulIncludedEntries by the specified id.
```

## Example pipeline

A common scenario for a content pipeline is to fetch a number of entries from contentful, parse markdown and then output them as .html files.

Here's an example of such a pipeline with a complete `config.wyam` configuration file.

```csharp
// Preprocessor directives
#n -p Wyam.Razor
#n -p Wyam.Markdown
#n -p Contentful.Wyam

// Body code
Pipelines.Add("Contentful-pipeline",
    Contentful("<delivery_api_key>","<space_id>")
    .WithContentField("body").WithContentType("<content_type_id>"), //Get all entries from contentful of the specified type, set the body field as content for each resulting document.
    Markdown(), //Parse the markdown from the documents and turn it into html.
    Meta("Body", @doc.Content), //Put the parsed HTML result into a metadata property to access later in the .cshtml files
    Merge(ReadFiles("templates/post.cshtml")), //Merge the documents with the post.cshtml template, now ready to be parsed by the Razor engine
    Razor(), //Parse the resulting documents using the Razor engine, turning them into plain .html output again.
    WriteFiles($"{@doc["slug"]}.html") //Write the files as .html files to disk
    );
```