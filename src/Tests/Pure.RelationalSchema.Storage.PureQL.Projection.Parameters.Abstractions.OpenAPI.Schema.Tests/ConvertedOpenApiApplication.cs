using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization;

namespace Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema.Tests;

/// <summary>
/// An in-memory ASP.NET Core application set up as the README tells: the parameter
/// converters registered, and the transformers. Its one endpoint, <c>POST /request</c>,
/// is the given handler.
/// </summary>
internal sealed record ConvertedOpenApiApplication
{
    private readonly Delegate _handler;

    private readonly bool _schemaTransformed;

    public ConvertedOpenApiApplication(Delegate handler)
        : this(handler, true) { }

    public ConvertedOpenApiApplication(Delegate handler, bool schemaTransformed)
    {
        _handler = handler;
        _schemaTransformed = schemaTransformed;
    }

    public async Task<JsonObject> DocumentAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();
        _ = builder.Services.ConfigureHttpJsonOptions(options =>
        {
            foreach (JsonConverter converter in new PureQLParametersConverters())
            {
                options.SerializerOptions.Converters.Add(converter);
            }
        });
        _ = builder.Services.AddOpenApi(options =>
        {
            if (_schemaTransformed)
            {
                _ = options.AddSchemaTransformer<PureQLParametersSchemaTransformer>();
            }

            _ = options.AddDocumentTransformer<PureQLParametersDocumentTransformer>();
        });

        await using WebApplication application = builder.Build();
        _ = application.MapOpenApi();
        _ = application.MapPost("/request", _handler);

        await application.StartAsync();

        using HttpClient client = application.GetTestClient();
        string json = await client.GetStringAsync("/openapi/v1.json");

        await application.StopAsync();

        return JsonNode.Parse(json)!.AsObject();
    }
}
