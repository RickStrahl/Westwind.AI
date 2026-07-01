# Westwind.AI Microsoft.AI Refactor Plan

## Goals

- Refactor chat/completion internals to use Microsoft.AI extensions instead of raw HTTP request plumbing.
- Preserve existing configuration architecture centered on `OpenAiConnection` and `OpenAiConnectionConfiguration`.
- Minimize outward-facing API changes and behavioral regressions.
- Keep changes compatible with a follow-up agentic integration pass (tools/MCP/function calling orchestration).
- Simplify target requirements to modern frameworks only.

## Target Framework Requirements

- `net10.0`
- `net8.0`

`net472` is out of scope for this refactor.

## Current State Summary

The current chat pipeline is built around a low-level HTTP abstraction and custom OpenAI JSON message models:

- `Westwind.AI/OpenAiHttpClient.cs`
- `Westwind.AI/Chat/GenericAiChatClient.cs`
- `Westwind.AI/Chat/AiTextOperations.cs`
- `Westwind.AI/AiBase.cs`
- `Westwind.AI/Configuration/OpenAiNativeChatRequestMessages.cs`

Provider and endpoint variability are already encapsulated well in:

- `Westwind.AI/Configuration/OpenAiConnection.cs`
- `Westwind.AI/Configuration/OpenAiConnectionConfiguration.cs`

This makes the migration feasible without broad public API redesign.

## Refactor Strategy

### 1. Keep Public API Stable

Keep existing entry points and signatures in place:

- `GenericAiChatClient.Complete(string prompt, string systemPrompt = null, bool includeHistory = false)`
- `GenericAiChatClient.Complete(IEnumerable<OpenAiChatMessage> prompts, bool includeHistory = false)`
- `AiTextOperations` methods (`Summarize`, `TranslateText`, `CheckGrammar`, etc.)

Internals are swapped to Microsoft.AI-backed transports/adapters.

### 2. Add Internal Transport Abstraction

Introduce an internal chat transport interface, for example:

- `IWestwindAiChatTransport`

Responsibilities:

- Send chat prompts/messages
- Return assistant text response
- Surface errors in existing format
- Preserve request context and history behavior

Implementations:

- `MicrosoftAiChatTransport` (primary)
- `OpenAiHttpFallbackTransport` (compatibility fallback for unsupported providers/features)

### 3. Bridge Existing Connection Model to Microsoft.AI

Add a factory/internal adapter that maps `IOpenAiConnection` to a configured Microsoft.AI `IChatClient`.

Key requirements:

- Respect `ProviderMode`, `Endpoint`, `ModelId`, `ApiKey`, `EndpointTemplate`
- Preserve multi-provider behavior (OpenAI, Azure OpenAI, GitHub Models, OpenAI-compatible endpoints where possible)
- Fall back to raw HTTP transport if provider/client builder path is unavailable

### 4. Message Model Mapping

Add bidirectional mapping between:

- `OpenAiChatMessage` (existing public/custom model)
- `ChatMessage` (Microsoft.Extensions.AI)

Mapping rules:

- Preserve `role` semantics (`system`, `user`, `assistant`, future `tool`)
- Preserve text + image content behavior where currently supported
- Preserve `includeHistory` semantics and dedupe behavior currently implemented

### 5. Preserve Diagnostics and Error Semantics

Existing diagnostics to preserve where possible:

- `CaptureRequestData`
- `LastRequestJson`
- `LastResponseJson`
- `ErrorMessage`
- `TimeTaken`

If exact JSON capture is not available via Microsoft.AI abstraction, implement best-effort structured trace capture and document any differences.

### 6. Agentic-Ready Internal Hooks

Without changing current public signatures, add internal extensibility for next phase:

- Tool-call message role support in mapper
- Optional execution context object for tool registry/MCP pipeline
- Internal response envelope that can carry text + tool invocation metadata

## Phased Execution Plan

## Phase 0: Prereqs and Dependency Setup

- Add required Microsoft.AI packages.
- Validate compatibility for `net10.0` and `net8.0`.

Deliverable:

- Build succeeds on target frameworks with no public API changes.

## Phase 1: Internal Abstraction Layer

- Introduce transport interface and factory.
- Add Microsoft.AI transport implementation skeleton.
- Keep existing `OpenAiHttpClient` path as fallback.

Deliverable:

- No behavior change yet; code compiles with new architecture seams.

## Phase 2: Message + Connection Adapters

- Implement connection-to-client factory logic.
- Implement message mappers and role/content conversions.
- Implement chat history handling parity.

Deliverable:

- Internal Microsoft.AI path can execute equivalent chat requests.

## Phase 3: Switch Chat Clients to New Transport

- Refactor `GenericAiChatClient` to use transport abstraction.
- Refactor `AiTextOperations` to use the same transport indirectly.
- Keep method signatures and return values unchanged.

Deliverable:

- Existing tests and usage patterns continue to work with minimal/no API breaks.

## Phase 4: Parity Testing and Hardening

- Add/adjust tests for:
  - single prompt + system prompt
  - prompt list API
  - chat history carry-forward
  - error propagation parity
  - provider matrix smoke coverage
- Run test suite on target frameworks.

Deliverable:

- Verified parity and documented known exceptions.

## Phase 5: Documentation and Soft Deprecation

- Update README to reflect Microsoft.AI-backed internals while preserving existing usage examples.
- Mark direct chat-specific low-level HTTP entry points as soft deprecated if appropriate.
- Keep migration notes clear and non-breaking for existing consumers.

Deliverable:

- Consumers can remain on current API while benefiting from new backend architecture.

## Scope Boundaries for This Refactor

In scope now:

- Chat/completions pipeline (`GenericAiChatClient`, `AiTextOperations`, chat history, message mapping)

Out of scope for first pass (recommended):

- Image generation migration (`OpenAiImageGeneration`)
- Text-to-speech migration (`OpenAiTextToSpeech`)

These can remain on raw HTTP until chat migration stabilizes.

## Risk Register and Mitigations

1. Provider compatibility differences
- Mitigation: keep fallback HTTP transport; gate providers by capability.

2. Diagnostic parity loss (request/response raw JSON)
- Mitigation: best-effort trace logging and explicit docs for behavior differences.

3. Behavior drift in history/message ordering
- Mitigation: parity tests against existing behavior before switching default path.

## Success Criteria

- Public APIs used by existing consumers compile without modification.
- Chat operations run through Microsoft.AI transport for supported providers.
- Existing configuration model remains unchanged and authoritative.
- Existing tests pass (or have explicitly documented, justified updates).
- Codebase is prepared for a follow-on agentic/tool-invocation integration pass.

## Proposed Commit Sequence

1. `chore: add Microsoft.AI dependencies and internal transport abstractions`
2. `feat: add connection and message adapters for Microsoft.AI chat clients`
3. `refactor: route GenericAiChatClient and AiTextOperations through transport`
4. `test: add chat parity/provider smoke tests`
5. `docs: update README and add migration notes`

## Decision Notes for Follow-up

- Decide when to deprecate (or retain) direct low-level chat methods on `OpenAiHttpClient`.
- Decide whether image/TTS should move to SDK abstractions in the next milestone or remain HTTP-first.
