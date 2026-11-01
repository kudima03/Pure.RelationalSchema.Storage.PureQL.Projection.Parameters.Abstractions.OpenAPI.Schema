# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-preview.0.2.0] - 2026-11-01

### Added

- `PureQLParametersSchemaTransformer`: an `IOpenApiSchemaTransformer` that makes the items of a collection and the values of a dictionary of `IParameterType`, `IParameter` or `IListParameter` reference their components. With the serialization converters registered, ASP.NET Core wrote `IParameter[]` as `{"type":"array"}` and `IReadOnlyDictionary<string, IParameter>` as `{"type":"object"}`, without the reference, and a document whose parameters were only such values had no `IParameter` component for `PureQLParametersDocumentTransformer` to replace. Register it next to the document transformer.

## [0.1.0-preview.0.1.0] - 2026-11-01

### Added

- `PureQLParametersDocumentTransformer`: an `IOpenApiDocumentTransformer` that replaces the schemas ASP.NET Core generates for `IParameterType`, `IParameter` and `IListParameter` with schemas matching their JSON, as written by `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization`: `{"name","nullable"}` with the 16 PureQL type names as an `enum`, `{"type","value"}` and `{"type","value","items"}`, with descriptions and examples. AOT-compatible.

[Unreleased]: https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema/compare/0.1.0-preview.0.2.0...HEAD
[0.1.0-preview.0.2.0]: https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema/compare/0.1.0-preview.0.1.0...0.1.0-preview.0.2.0
[0.1.0-preview.0.1.0]: https://github.com/kudima03/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema/releases/tag/0.1.0-preview.0.1.0
