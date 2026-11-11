# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory.

```bash
dotnet restore
dotnet tool restore
dotnet build --no-restore -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes             # check code style (CI enforces this)
dotnet csharpier check .                      # check formatting (CI enforces this)
dotnet format && dotnet csharpier format .    # auto-fix code style
dotnet stryker --mutation-level Complete --break-at 98
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`.

## Architecture

This is a **two-class NuGet library**. `PureQLParametersDocumentTransformer`, a sealed class implementing `IOpenApiDocumentTransformer` (from `Microsoft.AspNetCore.OpenApi`), replaces the component schemas ASP.NET Core generates for the interfaces of `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions` with schemas matching the JSON written by `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.Serialization`:

- `IParameterType`: `{"name","nullable"}`, both required; `name` is an `enum` of the 16 PureQL type names (`integer` … `uuid`, `integerList` … `uuidList`)
- `IParameter`: `{"type","value","items"}`, `type` and `value` required; `items` is there because the `IParameter` converter writes a list parameter with its items
- `IListParameter`: the same properties, with `items` required too

If the JSON format of the serialization package or the type names of the abstractions change, update the schemas, the `Expected/Components.json` snapshot and the README together.

**Schema transformer:** `PureQLParametersSchemaTransformer` (`IOpenApiSchemaTransformer`) exists because, with the serialization converters registered, ASP.NET Core writes a collection or dictionary of parameters without `items` / `additionalProperties`, and then may not create the component at all. For a schema whose `JsonTypeInfo.ElementType` is one of the three interfaces (matched by full name), it sets `items` (collections) or `additionalProperties` (`JsonTypeInfoKind.Dictionary`) to `context.GetOrCreateSchemaAsync(elementType)`. That schema carries ASP.NET Core's component marker, so ASP.NET Core turns it into a `$ref` and creates the component under the default reference id (the type name), which the document transformer then replaces. Do not set an `OpenApiSchemaReference` there: ASP.NET Core's reference resolution inlines the target of a reference set by a schema transformer.

**Matching:** components are matched by the names ASP.NET Core gives them (`IParameterType`, `IParameter`, `IListParameter`). A component is replaced only if the document has it, and the components it references are added: `IListParameter` brings `IParameter`, `IParameter` brings `IParameterType`. Other schemas (the generated `IString`, `IBool`, `IChar`, …) are never touched. The package does not reference the abstractions or the serialization package.

**Schemas:** built in code by private static methods, anew on each call, so documents never share mutable `OpenApiSchema` instances. Every component and property has a `Description`, and every component one entry in `Examples` (the obsolete `Example` fails the build under `-warnaserror`).

**AOT:** `IsAotCompatible = true`: trim and AOT analyzer warnings fail the build. Keep the class free of reflection; `JsonNode.Parse` and implicit `JsonNode` conversions from strings are fine.

**Source generator:** `Microsoft.AspNetCore.OpenApi` adds its XML comment generator to every project that references it. The library never calls `AddOpenApi`, so the `RemoveOpenApiSourceGenerator` target drops the generator from its build; otherwise ~500 lines of dead generated code would ship in the package and fail the coverage threshold. The test project keeps it, since `OpenApiApplication` calls `AddOpenApi`.

**Multi-targeting:** net10.0 and net11.0, each with the matching `Microsoft.AspNetCore.OpenApi` major (10.x and 11.0.0, with Microsoft.OpenApi 2.x and 3.x).

**Documentation:** `GenerateDocumentationFile` is on, so public members need XML doc comments (CS1591).

**Package validation:** `EnablePackageValidation = true`, with `PackageValidationBaselineVersion` set to the last published version (bump it after each release); breaking API changes against it fail the build.

**Publishing:** triggered by pushing a semver tag matching `*.*.*`. The tag name becomes the `PackageVersion`. Packages are published to both GitHub Packages and NuGet.org.

**Tests:** xUnit project at `src/Tests/Pure.RelationalSchema.Storage.PureQL.Projection.Parameters.Abstractions.OpenAPI.Schema.Tests/`, targeting net11.0:

- `PureQLParametersDocumentTransformerTests`: the transformer on hand-built documents
- `AspNetCoreDocumentTests`: an in-memory ASP.NET Core app (`OpenApiApplication`, on `TestServer`, without the converters) whose generated document is compared with `Expected/Components.json`
- `PureQLParametersSchemaTransformerTests`: an in-memory app set up as the README tells (`ConvertedOpenApiApplication`: converters registered, both transformers, one `POST /request` endpoint per test): collection items and dictionary values reference the components, other collections are untouched
- `SerializationConformanceTests`: JSON written by the published `PureQLParametersConverters` for the published `Pure.RelationalSchema.Storage.PureQL.Projection.Parameters` implementations, checked against the schemas

The test project is on `Microsoft.NET.Sdk`, so it sets `InterceptorsNamespaces` for the `AddOpenApi` source generator; without it the build fails with CS9137. CI enforces ≥98% line coverage (warning below 99%) and a 98% mutation score with dotnet-stryker.

## Code Style

Enforced via `.editorconfig`, `dotnet format --verify-no-changes` and `dotnet csharpier check .` in CI:

- No `var`: always use explicit types
- No expression-bodied methods or constructors. Properties, indexers, accessors and lambdas may be expression-bodied
- File-scoped namespace declarations
- `using` directives outside the namespace, System directives sorted first
- Private fields: `_camelCase`
- Max line length: 90 characters. The namespace declarations and the full package names in XML docs are longer and cannot be wrapped
- Braces on new lines (`csharp_new_line_before_open_brace = all`)
- In tests, do not compare JSON as raw string literals: `dotnet format` tags them with `/*lang=json*/` and re-indents them. Compare `JsonNode`s or single values instead

## Commit Messages

Conventional Commits (`feat: …`, `fix: …`, `test: …`, `docs: …`, `chore: …`, `ci: …`, `build: …`), without a scope or body. Do not mention Claude or AI assistance in commit messages or pull requests.
