# Changelog

All notable changes to `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `PureQLParametersDocumentTransformer`: an `IOpenApiDocumentTransformer` that replaces the schemas ASP.NET Core generates for `IParameterType`, `IParameter` and `IListParameter` with schemas matching their JSON, as written by `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization`: `{"name","nullable"}` with the 16 PureQL type names as an `enum`, `{"type","value"}` and `{"type","value","items"}`, with descriptions and examples. AOT-compatible.
