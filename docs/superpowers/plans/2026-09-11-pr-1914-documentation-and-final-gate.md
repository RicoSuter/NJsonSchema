# PR #1914 Phase 5: Documentation and Final Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve D1, accurately document the repaired migration, and publish verified NJsonSchema changes to the existing PR.

**Architecture:** Compare the actual API and behavior against the pre-migration base; compile migration examples; review the complete repaired branch before updating the PR description from its current template.

**Tech Stack:** Existing build/test/pack tools, reflection or source API comparison, C# snippets, GitHub CLI.

**Spec:** [Stabilization design](../specs/2026-09-11-pr-1914-stabilization-design.md).

## Global Constraints

- Begin after phases 1–4. Documentation must describe code that exists and test results actually obtained.
- Preserve the user's chosen integration: focused commits in PR #1914 targeting `v12`; no merge or release. Push only to its verified existing head branch, without force.
- NSwag companion fixes and its full downstream test gate are deliberately deferred to the existing NSwag STJ PR. Record required checks and dependencies; do not implement those fixes here or claim overall downstream readiness.
- Existing March migration docs are historical. The approved stabilization roadmap governs remaining repairs.
- No new public type solely to make a documentation example possible. Example code must only use public supported APIs.

## Task 1: correct migration guidance and inventory (D1)

Files: `docs/changelog_v12.md`, the stabilization design and phase checklists; add concise migration examples to an appropriate existing test file only if they provide an enduring public-contract regression. Otherwise compile temporary example projects outside the tracked tree.

- [ ] Inventory public removals/signature changes, constructors, virtual members, annotations, runtime types, exception contracts, and package/TFM changes between the pre-migration base and the repaired head. Verify claims against source or built assemblies; report source compatibility separately from binary compatibility.
- [ ] Correct the Newtonsoft adapter description: it provides reflection/generation support, not automatic restoration of direct Newtonsoft serialization of all STJ-annotated core types.
- [ ] Replace the invalid settings example with actual v11/v12 supported settings/generator APIs. Verify the old snippet against the baseline and the new snippet against the repaired source. Do not instantiate abstract settings or use a nonexistent `SerializerSettings` member.
- [ ] Correct `OpenApiDiscriminator.Mapping` examples to the actual dictionary value type and setter behavior. Compile the replacement example.
- [ ] Correct JSON-value runtime-type and date claims using the repaired materializer; explain exact-number fallback and distinguish literal date strings from sample-schema date inference.
- [ ] Explain exact enum/uniqueItems equality and whole-valued integer recognition. Remove double-normalization claims.
- [ ] Fix `ValidationError.Token` guidance: internal `JsonPropertyToken` cannot appear in consumer pattern matching. Use supported public access and display behavior; do not claim nonexistent public properties.
- [ ] Update leniency, source-location, stream ownership, and generated-contract notes to match repaired behavior. Audit `readOnly`/enum-description and other semantic snapshot differences against the baseline rather than listing all reordering as a breaking change.
- [ ] Compile runnable API examples in a temporary consumer project using the real built/project-referenced assemblies. Record target framework, commands, and results in the task report. Run code-generation consumer tests when examples touch generated output.
- [ ] Mark each finding resolved only after its actual implementation/test gate; keep NSwag integration visibly pending. Update plan checkboxes truthfully, retain any unresolved findings and new compatibility choices.
- [ ] Commit `Document the stabilized System.Text.Json migration contracts`.

## Final controller gate

- [ ] Have an independent reviewer inspect the complete migration diff against `v12`, with the original 26-finding checklist and repaired tests. Include API contracts, converter/async behavior, numeric edge cases, generated declarations, and performance changes. Address actionable findings and run covering tests.
- [ ] Run full local build, tests, and pack with the repository's supported SDK/build tooling:

```bash
./build.sh Compile Test Pack --configuration Release
git -c core.whitespace=cr-at-eol diff --check
```

- [ ] Review package contents/dependencies and supported TFMs. When a full build command needs environment-specific setup, record the actual equivalent commands and limitation rather than silently skipping a gate.
- [ ] Refresh the PR description using `.github/pull_request_template.md`, keeping Breaking changes and Verification. Organize breaking changes by API/binary, schema/JSON wire contract, runtime/validation, generated code, and downstream migration. Include measured performance with limitations.
- [ ] Preserve open migration decisions and distinguish fixed regressions from intentional changes. Explicitly leave the full NSwag companion check unchecked with the user's sequencing decision.
- [ ] Verify remote head ownership and branch before pushing `HEAD:feature/migrate-core-to-stj`; if the remote advanced, fetch and integrate its changes without overwriting them.
- [ ] Verify Windows and Ubuntu CI on the exact pushed head. Investigate failures and repair those caused by this work. Do not call old-head CI sufficient.
- [ ] Report final head, resolved findings, relevant test results, remaining risks, and the next NSwag companion work. Do not merge PR #1914 or release v12.
