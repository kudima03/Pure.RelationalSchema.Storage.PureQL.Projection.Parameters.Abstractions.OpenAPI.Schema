using System.Text.Json.Nodes;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema.Tests;

public sealed record AspNetCoreDocumentTests
{
    private static JsonObject Schemas(JsonObject document)
    {
        return document["components"]!["schemas"]!.AsObject();
    }

    [Fact]
    public async Task GeneratedDocumentWritesStringsAsArraysOfChars()
    {
        JsonObject schemas = Schemas(await new OpenApiApplication(false).DocumentAsync());
        JsonNode value = schemas["IParameter"]!["properties"]!["value"]!;
        JsonNode nullable = schemas["IParameterType"]!["properties"]!["nullable"]!;

        Assert.Contains(
            "array",
            value["type"] is JsonArray types
                ? types.Select(type => type!.GetValue<string>())
                : [value["type"]!.GetValue<string>()]
        );
        Assert.Equal(
            "#/components/schemas/IChar",
            value["items"]!["$ref"]!.GetValue<string>()
        );
        Assert.Equal("#/components/schemas/IBool", nullable["$ref"]!.GetValue<string>());
    }

    [Fact]
    public async Task TransformedDocumentHasTheExpectedComponents()
    {
        JsonObject schemas = Schemas(await new OpenApiApplication(true).DocumentAsync());
        JsonObject expected = JsonNode
            .Parse(await File.ReadAllTextAsync("Expected/Components.json"))!
            .AsObject();

        Assert.All(
            expected,
            component =>
                Assert.True(
                    JsonNode.DeepEquals(component.Value, schemas[component.Key]),
                    $"{component.Key}: {schemas[component.Key]?.ToJsonString()}"
                )
        );
    }

    [Fact]
    public async Task TransformedDocumentKeepsTheEndpointReferences()
    {
        JsonObject document = await new OpenApiApplication(true).DocumentAsync();
        JsonNode operation = document["paths"]!["/parameters"]!["post"]!;

        Assert.Equal(
            "#/components/schemas/IParameter",
            operation["requestBody"]!["content"]!["application/json"]!["schema"]![
                "items"
            ]!["$ref"]!.GetValue<string>()
        );
        Assert.Equal(
            "#/components/schemas/IListParameter",
            operation["responses"]!["200"]!["content"]!["application/json"]!["schema"]![
                "$ref"
            ]!.GetValue<string>()
        );
    }
}
