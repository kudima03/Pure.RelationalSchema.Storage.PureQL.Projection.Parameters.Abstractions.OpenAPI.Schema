using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Parameter;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.ListParameter;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Parameter;
using Decimal = Pure.Primitives.Number.Decimal;
using Long = Pure.Primitives.Number.Long;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema.Tests;

/// <summary>
/// Checks the JSON written by
/// <c>Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization</c>
/// against the schemas of the transformer: every property is described, every required
/// property is present, and every value has the described JSON type.
/// </summary>
public sealed record SerializationConformanceTests
{
    private static async Task<IDictionary<string, IOpenApiSchema>> Components()
    {
        OpenApiDocument document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    ["IListParameter"] = new OpenApiSchema(),
                },
            },
        };

        await new PureQLParametersDocumentTransformer().TransformAsync(
            document,
            null!,
            CancellationToken.None
        );

        return document.Components.Schemas;
    }

    private static JsonNode Serialize(IParameter parameter)
    {
        JsonSerializerOptions options = new JsonSerializerOptions();

        foreach (JsonConverter converter in new PureQLParametersConverters())
        {
            options.Converters.Add(converter);
        }

        return JsonNode.Parse(JsonSerializer.Serialize(parameter, options))!;
    }

    private static void Conforms(
        JsonNode? node,
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema> components
    )
    {
        IOpenApiSchema resolved = schema is OpenApiSchemaReference reference
            ? components[reference.Reference.Id!]
            : schema;

        switch (resolved.Type)
        {
            case JsonSchemaType.Object:
                JsonObject json = Assert.IsType<JsonObject>(node);
                HashSet<string> names = [.. json.Select(property => property.Key)];
                Assert.Subset(resolved.Properties!.Keys.ToHashSet(), names);
                Assert.Subset(names, resolved.Required!.ToHashSet());

                foreach ((string name, JsonNode? value) in json)
                {
                    Conforms(value, resolved.Properties[name], components);
                }

                break;
            case JsonSchemaType.Array:
                foreach (JsonNode? item in Assert.IsType<JsonArray>(node))
                {
                    Conforms(item, resolved.Items!, components);
                }

                break;
            case JsonSchemaType.String:
                Assert.Equal(JsonValueKind.String, node!.GetValueKind());

                if (resolved.Enum is { Count: > 0 } allowed)
                {
                    Assert.Contains(
                        node.GetValue<string>(),
                        allowed.Select(name => name.GetValue<string>())
                    );
                }

                break;
            case JsonSchemaType.Boolean:
                Assert.Contains(
                    node!.GetValueKind(),
                    new[] { JsonValueKind.True, JsonValueKind.False }
                );
                break;
            case JsonSchemaType.Null:
                break;
            case JsonSchemaType.Integer:
                break;
            case JsonSchemaType.Number:
                break;
            case null:
                break;
            default:
                Assert.Fail($"Unexpected schema type {resolved.Type}.");
                break;
        }
    }

    // Keyed by name, since xUnit cannot enumerate parameters as theory data rows.
    private static readonly IReadOnlyDictionary<string, IParameter> Parameters =
        new Dictionary<string, IParameter>
        {
            ["string"] = new StringParameter(new String("Ada")),
            ["decimal"] = new DecimalParameter(new Decimal(9.99m)),
            ["typed null"] = new NullableDateParameter(),
            ["stringList"] = new StringListParameter([
                new String("paid"),
                new String("shipped"),
            ]),
            ["integerList"] = new IntegerListParameter([new Long(1), new Long(-42)]),
            ["empty list"] = new StringListParameter([]),
        };

    [Theory]
    [InlineData("IParameter", "string")]
    [InlineData("IParameter", "decimal")]
    [InlineData("IParameter", "typed null")]
    [InlineData("IParameter", "stringList")]
    [InlineData("IListParameter", "integerList")]
    [InlineData("IListParameter", "empty list")]
    public async Task SerializedParameterConformsToItsSchema(
        string component,
        string parameter
    )
    {
        IDictionary<string, IOpenApiSchema> components = await Components();

        Conforms(Serialize(Parameters[parameter]), components[component], components);
    }

    [Fact]
    public async Task ListParameterWithoutItemsDoesNotConform()
    {
        IDictionary<string, IOpenApiSchema> components = await Components();

        _ = Assert.ThrowsAny<Exception>(() =>
            Conforms(
                Serialize(Parameters["string"]),
                components["IListParameter"],
                components
            )
        );
    }
}
