using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema.Tests;

public sealed record PureQLParametersDocumentTransformerTests
{
    private static OpenApiDocument Document(params string[] components)
    {
        Dictionary<string, IOpenApiSchema> schemas = [];

        foreach (string component in components)
        {
            schemas[component] = new OpenApiSchema { Type = JsonSchemaType.Object };
        }

        return new OpenApiDocument
        {
            Components = new OpenApiComponents { Schemas = schemas },
        };
    }

    private static async Task<IDictionary<string, IOpenApiSchema>> Transform(
        OpenApiDocument document
    )
    {
        await new PureQLParametersDocumentTransformer().TransformAsync(
            document,
            null!,
            CancellationToken.None
        );

        return document.Components!.Schemas!;
    }

    private static string Reference(IOpenApiSchema schema)
    {
        return Assert.IsType<OpenApiSchemaReference>(schema).Reference.Id!;
    }

    [Fact]
    public async Task ReplacesParameterTypeWithNameAndNullable()
    {
        IOpenApiSchema schema = (await Transform(Document("IParameterType")))[
            "IParameterType"
        ];

        Assert.Equal(JsonSchemaType.Object, schema.Type);
        Assert.Equal(["name", "nullable"], schema.Properties!.Keys);
        Assert.Equal(JsonSchemaType.String, schema.Properties["name"].Type);
        Assert.Equal(JsonSchemaType.Boolean, schema.Properties["nullable"].Type);
        Assert.Equal(["name", "nullable"], schema.Required!.Order());
    }

    [Fact]
    public async Task EnumeratesTheSixteenPureQLTypeNames()
    {
        IOpenApiSchema schema = (await Transform(Document("IParameterType")))[
            "IParameterType"
        ];

        Assert.Equal(
            [
                "integer",
                "decimal",
                "string",
                "boolean",
                "date",
                "time",
                "datetime",
                "uuid",
                "integerList",
                "decimalList",
                "stringList",
                "booleanList",
                "dateList",
                "timeList",
                "datetimeList",
                "uuidList",
            ],
            schema.Properties!["name"].Enum!.Select(name => name.GetValue<string>())
        );
    }

    [Fact]
    public async Task ReplacesParameterWithTypeValueAndOptionalItems()
    {
        IOpenApiSchema schema = (await Transform(Document("IParameter")))["IParameter"];

        Assert.Equal(JsonSchemaType.Object, schema.Type);
        Assert.Equal(["type", "value", "items"], schema.Properties!.Keys);
        Assert.Equal("IParameterType", Reference(schema.Properties["type"]));
        Assert.Equal(JsonSchemaType.String, schema.Properties["value"].Type);
        Assert.Equal(JsonSchemaType.Array, schema.Properties["items"].Type);
        Assert.Equal("IParameter", Reference(schema.Properties["items"].Items!));
        Assert.Equal(["type", "value"], schema.Required!.Order());
    }

    [Fact]
    public async Task ReplacesListParameterWithRequiredItems()
    {
        IOpenApiSchema schema = (await Transform(Document("IListParameter")))[
            "IListParameter"
        ];

        Assert.Equal(JsonSchemaType.Object, schema.Type);
        Assert.Equal(["type", "value", "items"], schema.Properties!.Keys);
        Assert.Equal("IParameterType", Reference(schema.Properties["type"]));
        Assert.Equal(JsonSchemaType.String, schema.Properties["value"].Type);
        Assert.Equal(JsonSchemaType.Array, schema.Properties["items"].Type);
        Assert.Equal("IParameter", Reference(schema.Properties["items"].Items!));
        Assert.Equal(["items", "type", "value"], schema.Required!.Order());
    }

    [Fact]
    public async Task ListParameterBringsParameterAndParameterType()
    {
        IDictionary<string, IOpenApiSchema> schemas = await Transform(
            Document("IListParameter")
        );

        Assert.Equal(
            ["IListParameter", "IParameter", "IParameterType"],
            schemas.Keys.Order()
        );
        Assert.Equal(["type", "value"], schemas["IParameter"].Required!.Order());
        Assert.Equal(["name", "nullable"], schemas["IParameterType"].Required!.Order());
    }

    [Fact]
    public async Task ParameterBringsParameterTypeOnly()
    {
        IDictionary<string, IOpenApiSchema> schemas = await Transform(
            Document("IParameter")
        );

        Assert.Equal(["IParameter", "IParameterType"], schemas.Keys.Order());
        Assert.Equal(["name", "nullable"], schemas["IParameterType"].Required!.Order());
    }

    [Fact]
    public async Task ParameterTypeBringsNothing()
    {
        IDictionary<string, IOpenApiSchema> schemas = await Transform(
            Document("IParameterType")
        );

        Assert.Equal(["IParameterType"], schemas.Keys);
    }

    [Fact]
    public async Task LeavesUnrelatedSchemas()
    {
        OpenApiDocument document = Document("IParameter", "IString", "Order");
        IOpenApiSchema @string = document.Components!.Schemas!["IString"];
        IOpenApiSchema order = document.Components.Schemas["Order"];

        IDictionary<string, IOpenApiSchema> schemas = await Transform(document);

        Assert.Same(@string, schemas["IString"]);
        Assert.Same(order, schemas["Order"]);
    }

    [Fact]
    public async Task AddsNothingToADocumentWithoutParameters()
    {
        IDictionary<string, IOpenApiSchema> schemas = await Transform(Document("Order"));

        Assert.Equal(["Order"], schemas.Keys);
    }

    [Fact]
    public async Task BuildsNewSchemasForEachDocument()
    {
        IDictionary<string, IOpenApiSchema> first = await Transform(
            Document("IListParameter")
        );
        IDictionary<string, IOpenApiSchema> second = await Transform(
            Document("IListParameter")
        );

        Assert.All(
            first.Keys,
            component => Assert.NotSame(first[component], second[component])
        );
    }

    [Fact]
    public async Task DescribesEveryComponentAndProperty()
    {
        IDictionary<string, IOpenApiSchema> schemas = await Transform(
            Document("IListParameter")
        );

        Assert.All(
            schemas.Values,
            schema =>
            {
                Assert.False(string.IsNullOrWhiteSpace(schema.Description));
                Assert.All(
                    schema.Properties!.Values.OfType<OpenApiSchema>(),
                    property =>
                        Assert.False(string.IsNullOrWhiteSpace(property.Description))
                );
            }
        );
    }

    [Fact]
    public async Task GivesEveryComponentAnExample()
    {
        IDictionary<string, IOpenApiSchema> schemas = await Transform(
            Document("IListParameter")
        );

        Assert.All(
            schemas.Values,
            schema => Assert.IsType<JsonObject>(Assert.Single(schema.Examples!))
        );
    }

    [Fact]
    public async Task IgnoresADocumentWithoutComponents()
    {
        OpenApiDocument document = new OpenApiDocument();

        await new PureQLParametersDocumentTransformer().TransformAsync(
            document,
            null!,
            CancellationToken.None
        );

        Assert.Null(document.Components);
    }

    [Fact]
    public async Task IgnoresComponentsWithoutSchemas()
    {
        OpenApiDocument document = new OpenApiDocument
        {
            Components = new OpenApiComponents(),
        };

        await new PureQLParametersDocumentTransformer().TransformAsync(
            document,
            null!,
            CancellationToken.None
        );

        Assert.Null(document.Components.Schemas);
    }
}
