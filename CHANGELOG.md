# Changelog

All notable changes are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

## [0.1.0] – 2026-05-28

### Added

- `AddHearth(Action<HearthOptions>)` — single-call DI registration for `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>`
- `HearthOptions` — full configuration for model path, context size, GPU layers, batch size, threads, HuggingFace token, download progress callback
- `HearthChatClient` — `IChatClient` implementation backed by `StatelessExecutor` (thread-safe; one KV-cache per call)
- `GetResponseAsync` and `GetStreamingResponseAsync` with per-call `Temperature` and `MaxOutputTokens`
- Chat template auto-detection (Llama 3, Gemma, Phi-3, ChatML) with per-family anti-prompt tokens
- Tool calling with automatic agentic loop (up to 5 rounds; respects `ChatToolMode.None`)
- `IEmbeddingGenerator<string, Embedding<float>>` backed by `LLamaEmbedder`
- `HuggingFaceClient` — resumable download with `Range` header, atomic rename, SHA-256 verify
- `QuantizationSelector` — preference-ranked GGUF file selection (`Q4_K_M > Q5_K_M > …`)
- `ModelResolver` — local path vs. HuggingFace repo-ID detection + cache at `~/.hearth/models`
- `Hearth.AI.AspNetCore` — `MapHearth()` exposes `POST /v1/chat/completions` (streaming + non-streaming SSE), `POST /v1/embeddings`, `GET /v1/models`
- `Hearth.AI.Cuda`, `Hearth.AI.Metal`, `Hearth.AI.Vulkan` — optional GPU backend shims
- `Hearth.AI.Templates` — `dotnet new hearth` project template (Web API with `AddHearth` + `MapHearth`, optional `--gpu-backend` flag)
- Console sample (`hearth-chat`) and Blazor Server streaming chat sample
- GitHub Actions CI (build + test on .NET 8 and 9, pack, publish to NuGet on push/tag)
- DocFX documentation site deployed to GitHub Pages
- GitHub Codespaces `devcontainer.json`, `samples/try-it.http` (VS Code REST Client), `Dockerfile` for local demo
- 96 unit and integration tests; all CI checks pass on .NET 8 and 9

## [Unreleased]

### Phase 1 — Core inference (current)

#### Added

- `AddHearth(Action<HearthOptions>)` DI extension for `IServiceCollection`
- `HearthOptions` — typed configuration for model path, context size, GPU layers, batch size, threads
- `HearthModel` — singleton wrapper around `LLamaWeights`; loads the GGUF once at first DI resolution
- `HearthChatClient` — `IChatClient` (Microsoft.Extensions.AI 9.5.0) backed by a `StatelessExecutor`
  - `GetResponseAsync` — collects the full response into a `ChatResponse`
  - `GetStreamingResponseAsync` — yields tokens as `IAsyncEnumerable<ChatResponseUpdate>`
  - `ChatOptions.Temperature` and `ChatOptions.MaxOutputTokens` are honoured per call
- `ChatTemplate.FormatChatML` — internal ChatML prompt formatter compatible with Qwen, Llama 3, Mistral, Phi-3, and most modern instruction-tuned GGUFs
- Anti-prompt tokens (`<|im_end|>`, `</s>`, `<|eot_id|>`, `<|end|>`) to stop generation at turn boundaries
- `Log` — structured logging via `[LoggerMessage]` source generator (zero-allocation at disabled log levels)
- Console sample (`hearth-chat`) — streaming multi-turn REPL with Ctrl+C cancellation
- Central package version management (`Directory.Packages.props`)
- `EnforceCodeStyleInBuild=true` + `TreatWarningsAsErrors=true` for consistent code style across the solution
- 11 unit tests covering `HearthOptions` defaults and `ChatTemplate` formatting edge cases

### Phase 2 — Model management (current)

#### Added

- `HuggingFaceClient` — fetches repository metadata from the Hub API and downloads files with
  resume support (`Range` header) and atomic rename (`.tmp` → final)
- `QuantizationSelector` — scores GGUF files by quantization preference; default ranking:
  `Q4_K_M > Q5_K_M > Q4_K_S > Q5_K_S … > Q2_K`; honours `ModelFile` if specified
- `ModelResolver` — detects whether `HearthOptions.Model` is a local path or a Hugging Face
  repo ID and coordinates downloading, SHA-256 verification, and cache lookup
- `ModelDownloadProgress` — public `readonly record struct` with `FileName`, `BytesDownloaded`,
  `TotalBytes`, `IsResumed`, and `Percentage`
- `HearthOptions.HuggingFaceToken` — bearer token for private/gated repositories
- `HearthOptions.OnDownloadProgress` — `Action<ModelDownloadProgress>` callback for progress UI
- SHA-256 post-download verification against Hugging Face LFS metadata; corrupt files are
  deleted and re-downloaded on the next startup
- Incremental cache: interrupted downloads leave a `.tmp` file that is resumed on next run
- `HearthModel.LoadAsync` — async entry point; `Load` bridges to it via `.GetAwaiter().GetResult()`
- 21 new unit tests (10 `QuantizationSelectorTests`, 14 `ModelResolverPathTests`)

### Phase 3 — Advanced inference (current)

#### Added

- `ChatTemplateFamily` — enum with `ChatML`, `Llama3`, `Gemma`, `Phi3` values
- `ChatTemplate.DetectFamily(modelPath)` — auto-detects template family from GGUF filename
- `ChatTemplate.FormatLlama3` — native Llama 3 format (`<|begin_of_text|>`, `<|start_header_id|>`, `<|eot_id|>`)
- `ChatTemplate.FormatGemma` — native Gemma format (`<start_of_turn>`, `<end_of_turn>`); system message prepended to first user turn
- `ChatTemplate.FormatPhi3` — native Phi-3/4 format (`<|user|>`, `<|assistant|>`, `<|end|>`)
- Per-family anti-prompt tokens — stops generation at correct turn boundaries for each model family
- `HearthChatClient` — automatically selects template family at construction; applies correct anti-prompts
- Tool/function calling in `GetResponseAsync` — injects tool JSON schema into system prompt; parses model output for `{"tool_call": ...}` JSON; auto-invokes matching `AIFunction` from `ChatOptions.Tools`; loops up to 5 rounds; respects `ChatToolMode.None` to disable
- `ToolCallParser` — internal JSON parser that extracts tool calls from plain text or markdown code-fenced model output
- `IEmbeddingGenerator<string, Embedding<float>>` — registered by `AddHearth()`; backed by `LLamaEmbedder` from LLamaSharp 0.21.0
- 38 new unit tests (13 `ChatTemplateFamilyTests`, 12 `ToolCallParserTests`) — 74 total passing

### Phase 4 — ASP.NET Core (current)

#### Added

- `Hearth.AspNetCore` package — new project targeting `net8.0;net9.0`; adds `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- `MapHearth(this IEndpointRouteBuilder)` — single call registers all three OpenAI-compatible routes
- `POST /v1/chat/completions` — maps to `IChatClient.GetResponseAsync`; echoes request `model` field; validates that `messages` is non-empty (400 otherwise)
- `POST /v1/chat/completions` streaming (`"stream": true`) — Server-Sent Events; emits role chunk, token chunks, finish-reason chunk, then `data: [DONE]`; flushes after every line
- `POST /v1/embeddings` — maps to `IEmbeddingGenerator<string, Embedding<float>>.GenerateAsync`; `input` accepts a string **or** an array of strings (400 on any other JSON kind)
- `GET /v1/models` — returns the registered model's ID via `IChatClient.GetService(typeof(ChatClientMetadata))`
- `HearthChatClient.GetService` updated to expose `ChatClientMetadata` so `Hearth.AspNetCore` can surface the model ID on `/v1/models`
- `Hearth.AspNetCore.Tests` — 22 integration and unit tests covering all three endpoints; uses in-process `TestServer` (no real model required); 96 total passing tests

### Phase 5 — Polish and release

#### Added

- `Hearth.Cuda` package — pulls in `LLamaSharp.Backend.Cuda12`; set `GpuLayers > 0` to offload to NVIDIA GPU
- `Hearth.Metal` package — pulls in `LLamaSharp.Backend.MacMetal`; set `GpuLayers = 999` on Apple Silicon for full GPU offload
- `Hearth.Vulkan` package — pulls in `LLamaSharp.Backend.Vulkan`; set `GpuLayers > 0` for AMD/Intel Vulkan GPUs
- Blazor Server streaming chat sample (`samples/Hearth.Samples.Blazor`) — real-time token rendering via `StateHasChanged()` on each `ChatResponseUpdate`; dark-themed chat UI; Enter to send, Shift+Enter for newline; cancellable generation
- GitHub Actions CI/CD (`.github/workflows/ci.yml`) — build + test on .NET 8 and 9; `pack` job on `main`; `publish` job on `v*` tags using `NUGET_API_KEY` secret; test results and `.nupkg` artifacts uploaded per run
- NuGet packages now embed `README.md` (`PackageReadmeFile`); version bumped to `0.1.0`; extended `PackageTags`; `PackageReleaseNotes` points to `CHANGELOG.md`
