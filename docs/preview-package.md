# Preview package status

`Microsoft.Substrait` 0.1 previews are evaluation packages. Continuous
integration produces `.nupkg` and `.snupkg` workflow artifacts, but does not
publish them to a package feed.

## API baseline

The Roslyn Public API analyzer records the current surface in
`src/Substrait/PublicAPI.Unshipped.txt`. `PublicAPI.Shipped.txt` remains empty for
the first preview, so no API is declared stable yet. Moving APIs into the shipped
baseline requires a separate review after package-consumer feedback. Until then,
preview releases may contain breaking API changes.

## Compatibility scope

- The package targets `net8.0` and `netstandard2.0`.
- Generated protobuf bindings and embedded extension definitions correspond to
  the Substrait v0.73.0 source pinned by the repository submodule.
- Both targets depend on `Antlr4.Runtime.Standard`, `Google.Protobuf`, and
  `YamlDotNet`. The `netstandard2.0` target also uses `IndexRange`,
  `Microsoft.Bcl.HashCode`, and `System.Memory` compatibility packages.
- query-language, product-specific, product-specific, product-specific, downstream/downstream integration, and shared downstream
  `TestData` assets remain outside the public package.

Package validation covers metadata, dependencies, license expression, README,
symbols, Source Link, an SPDX 2.2 SBOM, and standalone package consumers on
Windows and Linux. Windows also executes the `netstandard2.0` package asset from
a .NET Framework 4.6.2 consumer. When organization policy blocks public package
downloads on developer machines, the hosted CI jobs are the authoritative
clean-restore result.

## Remaining gate

Behavioral comparison with the downstream implementation using an approved shared
compatibility corpus is still required before package adoption and downstream adoption.
The existing downstream fixture collections cannot be copied into this repository
without the provenance review described in `tests/README.md`.