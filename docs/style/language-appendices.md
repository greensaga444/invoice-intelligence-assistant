# Language Appendices (Layer 2)

Use these defaults unless a project section overrides them.

## JavaScript and TypeScript

- Formatter: Prettier
- Linter: ESLint
- Type checks: TypeScript `strict` mode
- Notes:
  - Use explicit types at module boundaries.
  - Avoid `any` unless documented with rationale.

## Python

- Formatter: Ruff format or Black
- Linter: Ruff
- Type checks: MyPy or Pyright
- Notes:
  - Prefer explicit return types for public functions.
  - Keep side effects out of import time.

## Go

- Formatter: gofmt
- Linter: golangci-lint
- Static checks: go vet
- Notes:
  - Follow idiomatic Go style over custom patterns.
  - Keep interfaces minimal and consumer-driven.

## Java

- Formatter: google-java-format or Spotless
- Linter: Checkstyle or PMD
- Static checks: Error Prone
- Notes:
  - Favor immutability for shared data structures.
  - Keep package boundaries explicit.

## C#

- Formatter: dotnet format
- Linter: Roslyn analyzers
- Static checks: nullable reference types + analyzers
- Notes:
  - Enable nullable reference types by default.
  - Prefer async APIs for I/O paths.

## Rust

- Formatter: rustfmt
- Linter: Clippy
- Static checks: compiler warnings treated as errors
- Notes:
  - Model invalid states out of type design.
  - Prefer Result propagation over panic in libraries.

## Kotlin

- Formatter: ktlint
- Linter: detekt
- Static checks: compiler warnings enforced in CI
- Notes:
  - Use null-safety idioms consistently.
  - Keep coroutine scope ownership explicit.

## Swift

- Formatter: SwiftFormat
- Linter: SwiftLint
- Static checks: compiler warnings enforced in CI
- Notes:
  - Favor value types where practical.
  - Keep threading and actor boundaries explicit.

## C and C++

- Formatter: clang-format
- Linter: clang-tidy
- Static checks: compiler warnings at strict levels
- Notes:
  - Use RAII patterns for resource safety.
  - Keep ownership and lifetime rules explicit.
