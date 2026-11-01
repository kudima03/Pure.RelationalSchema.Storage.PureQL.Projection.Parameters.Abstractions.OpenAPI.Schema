using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema;

/// <summary>
/// Replaces the component schemas ASP.NET Core generates for the query parameter
/// abstractions of <c>Pure.RelationalSchema.Storage.PureQL.Projection</c>,
/// <c>IParameterType</c>, <c>IParameter</c> and <c>IListParameter</c>, with schemas
/// matching their JSON as written by
/// <c>Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization</c>:
/// <c>{"name", "nullable"}</c>, <c>{"type", "value"}</c> and
/// <c>{"type", "value", "items"}</c>.
/// </summary>
/// <remarks>
/// Only the components present in the document are replaced, together with the
/// components they reference: an <c>IListParameter</c> brings <c>IParameter</c>, and an
/// <c>IParameter</c> brings <c>IParameterType</c>. The schemas are built anew on each
/// call, without reflection, so the transformer is trimming- and native AOT-compatible.
/// </remarks>
public sealed class PureQLParametersDocumentTransformer : IOpenApiDocumentTransformer
{
    private const string ParameterTypeComponent = "IParameterType";

    private const string ParameterComponent = "IParameter";

    private const string ListParameterComponent = "IListParameter";

    private static readonly string[] TypeNames =
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
    ];

    /// <summary>
    /// Replaces the schemas of <c>IParameterType</c>, <c>IParameter</c> and
    /// <c>IListParameter</c> in <c>document.Components.Schemas</c>, if the document has
    /// them, and adds the components they reference. Other schemas are left as they are.
    /// </summary>
    /// <param name="document">The OpenAPI document to transform.</param>
    /// <param name="context">The transformer context; not used.</param>
    /// <param name="cancellationToken">Not used: the transformation is synchronous.
    /// </param>
    /// <returns>A completed task.</returns>
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        if (document.Components?.Schemas is not { } schemas)
        {
            return Task.CompletedTask;
        }

        bool listParameter = schemas.ContainsKey(ListParameterComponent);
        bool parameter = listParameter || schemas.ContainsKey(ParameterComponent);
        bool parameterType = parameter || schemas.ContainsKey(ParameterTypeComponent);

        if (listParameter)
        {
            schemas[ListParameterComponent] = ListParameterSchema();
        }

        if (parameter)
        {
            schemas[ParameterComponent] = ParameterSchema();
        }

        if (parameterType)
        {
            schemas[ParameterTypeComponent] = ParameterTypeSchema();
        }

        return Task.CompletedTask;
    }

    private static OpenApiSchema ParameterTypeSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description =
                "The PureQL type of a bound query parameter: the counterpart of the "
                + "type of a PureQL literal.",
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["name"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Description =
                        "The PureQL type name; for a list parameter, the list type "
                        + "name, such as stringList.",
                    Enum = [.. TypeNames.Select(name => (JsonNode)name)],
                },
                ["nullable"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Boolean,
                    Description =
                        "True for the type of a typed null; false for the type of a "
                        + "value and for a list type.",
                },
            },
            Required = new HashSet<string> { "name", "nullable" },
            Examples = [JsonNode.Parse("""{"name":"decimal","nullable":false}""")!],
        };
    }

    private static OpenApiSchema ParameterSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description =
                "The value bound to a query parameter: the counterpart of a PureQL "
                + "literal. A list parameter also has its items.",
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["type"] = new OpenApiSchemaReference(ParameterTypeComponent),
                ["value"] = ValueSchema(),
                ["items"] = ItemsSchema(),
            },
            Required = new HashSet<string> { "type", "value" },
            Examples =
            [
                JsonNode.Parse(
                    """{"type":{"name":"decimal","nullable":false},"value":"9.99"}"""
                )!,
            ],
        };
    }

    private static OpenApiSchema ListParameterSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description =
                "The list bound to a list parameter, read by in: the counterpart of a "
                + "PureQL list literal, with its items as parameters of the item type.",
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["type"] = new OpenApiSchemaReference(ParameterTypeComponent),
                ["value"] = ValueSchema(),
                ["items"] = ItemsSchema(),
            },
            Required = new HashSet<string> { "type", "value", "items" },
            Examples =
            [
                JsonNode.Parse(
                    """
                    {
                      "type": { "name": "stringList", "nullable": false },
                      "value": "[\"paid\",\"shipped\"]",
                      "items": [
                        {
                          "type": { "name": "string", "nullable": false },
                          "value": "paid"
                        },
                        {
                          "type": { "name": "string", "nullable": false },
                          "value": "shipped"
                        }
                      ]
                    }
                    """
                )!,
            ],
        };
    }

    private static OpenApiSchema ValueSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Description =
                "The bound value as invariant cell text, such as -42, 1.5, True, "
                + "2026-10-29, 08:30:00, 2026-10-29T08:30:00 or a lower case uuid; "
                + "the empty string for a typed null. For a list parameter, a JSON "
                + "array of its items' values, such as [\"paid\",\"shipped\"].",
        };
    }

    private static OpenApiSchema ItemsSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Description =
                "The items of a list parameter, in order: parameters of the item type "
                + "that hold a value. A parameter that is not a list has no items.",
            Items = new OpenApiSchemaReference(ParameterComponent),
        };
    }
}
