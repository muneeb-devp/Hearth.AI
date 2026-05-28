# Contributing to Hearth

Thank you for wanting to improve Hearth. This document covers how to set up the project, the conventions used, and what makes a good contribution.

## Ground rules

- Be kind. We follow the [Contributor Covenant](https://www.contributor-covenant.org/).
- Small, focused PRs merge faster than large refactors.
- Tests are required for new behavior.
- Existing tests must pass before a PR will be reviewed.

## Setting up

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or .NET 8 — both are supported)
- A GGUF model file for manual testing (optional — unit/integration tests use mocks)

### Clone and build

```bash
git clone https://github.com/muneeb-devp/Hearth.AI.git
cd Hearth.AI
dotnet restore
dotnet build
```

### Run the tests

```bash
dotnet test
```

All tests run without a real model. Integration tests for `Hearth.AI.AspNetCore` use `Microsoft.AspNetCore.TestHost` with mock implementations.

### Try it locally

```bash
# Console sample
dotnet run --project samples/Hearth.Samples.Console -- ./path/to/model.gguf

# Blazor sample
dotnet run --project samples/Hearth.Samples.Blazor -- ./path/to/model.gguf
```

## Project structure

```
src/
  Hearth/               Core library — inference, model loading, HuggingFace download
  Hearth.AspNetCore/    OpenAI-compatible endpoint middleware
  Hearth.Cuda/          NVIDIA CUDA 12 backend shim
  Hearth.Metal/         Apple Metal backend shim
  Hearth.Vulkan/        Vulkan backend shim
samples/
  Hearth.Samples.Console/  CLI chat sample
  Hearth.Samples.Blazor/   Streaming Blazor Server chat sample
tests/
  Hearth.Tests/             Unit tests for core
  Hearth.AspNetCore.Tests/  Integration tests for endpoint middleware
templates/
  hearth-webapi/            dotnet new template source
  Hearth.AI.Templates.csproj
docs/                       DocFX source for the GitHub Pages site
```

## Conventions

- **Nullable reference types** are enabled everywhere. Don't add `!` suppressions without a comment explaining why.
- **No synchronous wrappers** over async code in new code paths (`.GetAwaiter().GetResult()` exists in a few legacy spots — don't add more).
- **No external state in tests.** Tests must not read from disk or network. Use constructor injection / DI to swap real implementations for mocks.
- **Comments only for non-obvious WHY.** Method names and types document what the code does. Add a comment when there's a hidden constraint or surprising invariant.
- **Target both net8.0 and net9.0** for `Hearth` and `Hearth.AspNetCore`. Other projects can target net9.0 only.

## Good first issues

Issues labelled [`good-first-issue`](https://github.com/muneeb-devp/Hearth.AI/issues?q=is%3Aissue+is%3Aopen+label%3Agood-first-issue) are a good place to start. They are scoped to be completable without a deep understanding of the full codebase.

## Submitting a pull request

1. Fork the repo and create a branch from `main`.
2. Make your changes. Add tests that cover the new behavior.
3. Run `dotnet test` — all tests must pass.
4. Open a PR against `main`. Describe:
   - What the change does
   - Why it is the right approach
   - How to test it manually (if applicable)

PRs that add or change public API surface should include XML doc comment updates and, if the change warrants it, a documentation article update in `docs/articles/`.

## Reporting security issues

Please do **not** open a public GitHub issue for security vulnerabilities. Email [security@hearth.ai](mailto:security@hearth.ai) instead. We'll respond within 72 hours.
