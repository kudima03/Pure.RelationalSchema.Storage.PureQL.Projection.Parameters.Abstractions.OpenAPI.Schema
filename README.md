# Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema

AOT-compatible OpenAPI document transformer for the query parameter abstractions of `Pure.RelationalSchema.Storage.PureQL.Projection`: `IParameterType`, `IParameter` and `IListParameter`.

[![.NET build & test](https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema)](https://www.nuget.org/packages/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

[`Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions) describes a value bound to a query parameter: its PureQL type and its value as invariant cell text, or, for a list parameter, its list type and its items. [`Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization`](https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization) writes them as JSON.

ASP.NET Core's OpenAPI generator cannot see that JSON: it reflects over the interfaces, so `IString` becomes an array of `IChar` objects and `IBool` an object with a `boolValue`. With the converters registered, it writes a collection or a dictionary of parameters without its items. This package provides two transformers:

- `PureQLParametersDocumentTransformer`, an `IOpenApiDocumentTransformer` that replaces the generated `IParameterType`, `IParameter` and `IListParameter` component schemas with schemas matching the JSON the converters write.
- `PureQLParametersSchemaTransformer`, an `IOpenApiSchemaTransformer` that makes the items of a collection and the values of a dictionary of parameters reference those components.

The schemas are built in code, without reflection, so the transformers work under trimming and native AOT.

## Installation

```
dotnet add package Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema
```

## Public API

| Name | Namespace | Description |
|------|-----------|-------------|
| `PureQLParametersDocumentTransformer` | `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema` | `IOpenApiDocumentTransformer` that replaces the generated schemas of `IParameterType`, `IParameter` and `IListParameter` |
| `PureQLParametersSchemaTransformer` | `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema` | `IOpenApiSchemaTransformer` that points the items of a collection and the values of a dictionary of those types at their components |

## Schemas

| Component | JSON shape | Required |
|-----------|------------|----------|
| `IParameterType` | `{ "name": string, "nullable": boolean }` | `name`, `nullable` |
| `IParameter` | `{ "type": IParameterType, "value": string, "items": IParameter[] }` | `type`, `value` |
| `IListParameter` | `{ "type": IParameterType, "value": string, "items": IParameter[] }` | `type`, `value`, `items` |

- **`name`** is an `enum` of the 16 PureQL type names: `integer`, `decimal`, `string`, `boolean`, `date`, `time`, `datetime`, `uuid`, and the list types `integerList` … `uuidList`.
- **`value`** is a string: the cell text, such as `"9.99"`, the empty string for a typed null, or, for a list parameter, a JSON array of its items' values, such as `"[\"paid\",\"shipped\"]"`.
- **`items`** is optional on `IParameter`, since a list parameter written as an `IParameter` keeps its items, and required on `IListParameter`.
- Every component and property has a description, and every component an example:

```json
{
  "type": { "name": "stringList", "nullable": false },
  "value": "[\"paid\",\"shipped\"]",
  "items": [
    { "type": { "name": "string", "nullable": false }, "value": "paid" },
    { "type": { "name": "string", "nullable": false }, "value": "shipped" }
  ]
}
```

The schemas describe the JSON as it is written. When reading, the converters also accept a list parameter without its `value`, which they build from the items.

### Collections and dictionaries

With the converters registered, ASP.NET Core still references the component of a parameter it meets as a property, a body or a response, but writes `IParameter[]` as `{"type":"array"}` and `IReadOnlyDictionary<string, IParameter>` as `{"type":"object"}`, without `items` or `additionalProperties`. When the document has no other reference to `IParameter`, it then has no `IParameter` component either, so the document transformer has nothing to replace.

`PureQLParametersSchemaTransformer` restores them: for a collection or a dictionary whose element type is `IParameterType`, `IParameter` or `IListParameter` (matched by its full name), it sets `items` or `additionalProperties` to the schema ASP.NET Core generates for the element type, which ASP.NET Core then writes as a reference to that type's component. Other schemas are left as they are.

### Which components are replaced

The transformer replaces a component only if the document has it, and then adds the components it references: `IListParameter` brings `IParameter`, and `IParameter` brings `IParameterType`. Other schemas, such as the generated `IString`, `IBool` and `IChar`, are left as they are; register [`Pure.Primitives.Abstractions.OpenAPI.Schema`](https://github.com/kudima03/Pure.Primitives.Abstractions.OpenAPI.Schema) as well if other endpoints use Pure primitives. The schemas are built anew for each document, so one document's transformers never change another's.

## Usage

Register both transformers, and the `System.Text.Json` converters the schemas describe. Without the schema transformer, the request body of the endpoint below is an array without items:

```csharp
using System.Text.Json.Serialization;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Parameter;
using Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer<PureQLParametersSchemaTransformer>();
    options.AddDocumentTransformer<PureQLParametersDocumentTransformer>();
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    foreach (JsonConverter converter in new PureQLParametersConverters())
    {
        options.SerializerOptions.Converters.Add(converter);
    }
});

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapPost("/parameters", (IParameter[] parameters) => Results.Ok(parameters));

app.Run();
```

### Outside the Web SDK

`AddOpenApi` comes with a source generator that intercepts the call. In a project on `Microsoft.NET.Sdk` rather than `Microsoft.NET.Sdk.Web`, such as a test project, enable its interceptors, or the build fails with CS9137:

```xml
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

## Design Principles

- **Matches the JSON** — the schemas describe what `PureQLParametersConverters` writes, and the tests check its output against them
- **Replace, never invent** — a component is replaced only when the document has it or a replaced component references it
- **AOT-compatible** — `IsAotCompatible=true`; schemas are built in code, without reflection
- **Two public types** — `PureQLParametersDocumentTransformer` and `PureQLParametersSchemaTransformer`

## Dependencies

- [`Microsoft.AspNetCore.OpenApi`](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi) — `IOpenApiDocumentTransformer` and `IOpenApiSchemaTransformer`
- [`Microsoft.OpenApi`](https://www.nuget.org/packages/Microsoft.OpenApi) — the OpenAPI document model

The package does not reference the abstractions: it matches the components by the names ASP.NET Core gives them.

## Target Frameworks

- .NET 10

## Related packages

- `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions` — the interfaces these schemas describe
- `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization` — the `System.Text.Json` converters that write the JSON described here
- `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters` — the implementations, one per PureQL literal and list literal
- `Pure.Primitives.Abstractions.OpenAPI.Schema`, `Pure.RelationalSchema.Abstractions.OpenAPI.Schema`, `Pure.RelationalSchema.Storage.Abstractions.OpenAPI.Schema` — the transformers of the other Pure abstractions
