using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema;

/// <summary>
/// Restores the schema of the items of a collection and of the values of a dictionary of
/// <c>IParameterType</c>, <c>IParameter</c> or <c>IListParameter</c>, so that they
/// reference the component that <see cref="PureQLParametersDocumentTransformer"/>
/// replaces.
/// </summary>
/// <remarks>
/// With the converters of
/// <c>Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization</c>
/// registered, ASP.NET Core still references the component of a parameter it meets as a
/// property, a body or a response, but writes a collection of them as
/// <c>{"type":"array"}</c> and a dictionary of them as <c>{"type":"object"}</c>, without
/// the reference. This transformer sets <c>items</c> or <c>additionalProperties</c> to
/// the schema ASP.NET Core generates for the element type, which it then turns into a
/// reference to that type's component. Other schemas are left as they are. The element
/// type is matched by its full name, so the package does not reference the abstractions.
/// </remarks>
public sealed class PureQLParametersSchemaTransformer : IOpenApiSchemaTransformer
{
    private const string AbstractionsNamespace =
        "Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions";

    private static readonly HashSet<string> ParameterTypes =
    [
        $"{AbstractionsNamespace}.ParameterType.IParameterType",
        $"{AbstractionsNamespace}.Parameter.IParameter",
        $"{AbstractionsNamespace}.ListParameter.IListParameter",
    ];

    /// <summary>
    /// Sets <c>items</c> of a collection, or <c>additionalProperties</c> of a dictionary,
    /// whose element type is one of the parameter abstractions to the schema of that
    /// type, which ASP.NET Core writes as a reference to its component.
    /// </summary>
    /// <param name="schema">The schema to transform.</param>
    /// <param name="context">The transformer context: the schema's type, and the
    /// schemas of other types.</param>
    /// <param name="cancellationToken">Cancels generating the element type's schema.
    /// </param>
    /// <returns>A task that completes when the schema is transformed.</returns>
    public async Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        JsonTypeInfo type = context.JsonTypeInfo;

        if (
            type.ElementType is not { FullName: { } name } elementType
            || !ParameterTypes.Contains(name)
        )
        {
            return;
        }

        OpenApiSchema element = await context.GetOrCreateSchemaAsync(
            elementType,
            cancellationToken: cancellationToken
        );

        if (type.Kind == JsonTypeInfoKind.Dictionary)
        {
            schema.AdditionalProperties = element;
        }
        else
        {
            schema.Items = element;
        }
    }
}
