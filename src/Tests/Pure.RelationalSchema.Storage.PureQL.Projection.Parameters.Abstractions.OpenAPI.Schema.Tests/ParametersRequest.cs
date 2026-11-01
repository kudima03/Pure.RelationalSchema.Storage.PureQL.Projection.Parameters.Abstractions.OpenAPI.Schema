using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Parameter;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema.Tests;

/// <summary>A request body that holds parameters only as the values of a dictionary.</summary>
/// <param name="Parameters">The parameters, by name.</param>
public sealed record ParametersRequest(
    IReadOnlyDictionary<string, IParameter> Parameters
);
