using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.ListParameter;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Parameter;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.ParameterType;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema.Tests;

public sealed record PureQLParametersSchemaTransformerTests
{
    private static JsonNode Body(JsonObject document)
    {
        return document["paths"]!["/request"]!["post"]!["requestBody"]!["content"]![
            "application/json"
        ]!["schema"]!;
    }

    private static JsonObject Schemas(JsonObject document)
    {
        return document["components"]!["schemas"]!.AsObject();
    }

    private static string Reference(JsonNode schema)
    {
        return schema["$ref"]!.GetValue<string>();
    }

    [Fact]
    public async Task GeneratedDocumentWithConvertersLeavesArrayItemsUntyped()
    {
        JsonObject document = await new ConvertedOpenApiApplication(
            ([FromBody] IParameter[] parameters) => Results.Ok(),
            false
        ).DocumentAsync();

        Assert.Null(Body(document)["items"]);
    }

    [Fact]
    public async Task ArrayItemsReferenceTheParameterComponent()
    {
        JsonObject document = await new ConvertedOpenApiApplication(
            ([FromBody] IParameter[] parameters) => Results.Ok()
        ).DocumentAsync();

        Assert.Equal(
            "#/components/schemas/IParameter",
            Reference(Body(document)["items"]!)
        );
    }

    [Fact]
    public async Task ArrayItemsReferenceTheListParameterComponent()
    {
        JsonObject document = await new ConvertedOpenApiApplication(
            ([FromBody] IListParameter[] parameters) => Results.Ok()
        ).DocumentAsync();

        Assert.Equal(
            "#/components/schemas/IListParameter",
            Reference(Body(document)["items"]!)
        );
    }

    [Fact]
    public async Task DictionaryValuesReferenceTheParameterTypeComponent()
    {
        JsonObject document = await new ConvertedOpenApiApplication(
            ([FromBody] Dictionary<string, IParameterType> types) => Results.Ok()
        ).DocumentAsync();

        Assert.Equal(
            "#/components/schemas/IParameterType",
            Reference(Body(document)["additionalProperties"]!)
        );
    }

    [Fact]
    public async Task DictionaryPropertyValuesReferenceTheReplacedParameterComponent()
    {
        JsonObject document = await new ConvertedOpenApiApplication(
            ([FromBody] ParametersRequest request) => Results.Ok()
        ).DocumentAsync();
        JsonObject schemas = Schemas(document);
        JsonObject expected = JsonNode
            .Parse(await File.ReadAllTextAsync("Expected/Components.json"))!
            .AsObject();

        Assert.Equal(
            "#/components/schemas/IParameter",
            Reference(
                schemas["ParametersRequest"]!["properties"]!["parameters"]![
                    "additionalProperties"
                ]!
            )
        );
        Assert.True(JsonNode.DeepEquals(expected["IParameter"], schemas["IParameter"]));
    }

    [Fact]
    public async Task OtherCollectionsAreLeftAsTheyAre()
    {
        JsonObject document = await new ConvertedOpenApiApplication(
            ([FromBody] string[] names) => Results.Ok()
        ).DocumentAsync();

        Assert.Equal("string", Body(document)["items"]!["type"]!.GetValue<string>());
        Assert.Null(document["components"]?["schemas"]?["IParameter"]);
    }
}
