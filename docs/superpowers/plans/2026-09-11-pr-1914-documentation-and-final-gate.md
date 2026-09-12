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

## Task 1: preserve the toolchain getter and correct migration guidance (A1, D1)

Files: `src/NJsonSchema/JsonSchema.cs`, an appropriate core API contract test file, `docs/changelog_v12.md`, the stabilization design and phase checklists; add concise migration examples to an appropriate existing test file only if they provide an enduring public-contract regression. Otherwise compile temporary example projects outside the tracked tree.

- [x] Restore `JsonSchema.ToolchainVersion` as a public static get-only property (A1), retaining cached initialization and the STJ version text. Baseline master `18ba2ccf` exposes a getter; the migration changed it to a field unnecessarily. Add a failing public API regression that verifies the getter/property contract, then restore it and run covering tests. If feasible, compile a baseline consumer using the getter and run it with the repaired assembly to verify the restored member binding. Do not list this repaired change as an intentional break.
- [x] Inventory public removals/signature changes, constructors, virtual members, annotations, runtime types, exception contracts, and package/TFM changes between the pre-migration base and the repaired head. Verify claims against source or built assemblies; report source compatibility separately from binary compatibility.
- [x] Correct the Newtonsoft adapter description: it provides reflection/generation support, not automatic restoration of direct Newtonsoft serialization of all STJ-annotated core types.
- [x] Replace the invalid settings example with actual v11/v12 supported settings/generator APIs. Verify the old snippet against the baseline and the new snippet against the repaired source. Do not instantiate abstract settings or use a nonexistent `SerializerSettings` member.
- [x] Correct `OpenApiDiscriminator.Mapping` examples to the actual dictionary value type and setter behavior. Compile the replacement example.
- [x] Correct JSON-value runtime-type and date claims using the repaired materializer; explain exact-number fallback and distinguish literal date strings from sample-schema date inference.
- [x] Explain exact enum/uniqueItems equality and whole-valued integer recognition. Remove double-normalization claims.
- [x] Fix `ValidationError.Token` guidance: internal `JsonPropertyToken` cannot appear in consumer pattern matching. Use supported public access and display behavior; do not claim nonexistent public properties.
- [x] Update leniency, source-location, stream ownership, and generated-contract notes to match repaired behavior. Audit `readOnly`/enum-description and other semantic snapshot differences against the baseline rather than listing all reordering as a breaking change.
- [x] Compile runnable API examples in a temporary consumer project using the real built/project-referenced assemblies. Record target framework, commands, and results in the task report. Run code-generation consumer tests when examples touch generated output.
- [x] Mark each finding resolved only after its actual implementation/test gate; keep NSwag integration visibly pending. Update plan checkboxes truthfully, retain any unresolved findings and new compatibility choices.
- [x] Commit `Document the stabilized System.Text.Json migration contracts`.

## Final controller gate

- [x] Have an independent reviewer inspect the complete migration diff against `v12`, with the original 26-finding checklist and repaired tests. Include API contracts, converter/async behavior, numeric edge cases, generated declarations, and performance changes. Address actionable findings and run covering tests.
- [x] Run full local build, tests, and pack with the repository's supported SDK/build tooling:

```bash
./build.sh Compile Test Pack --configuration Release
git -c core.whitespace=cr-at-eol diff --check
```

- [x] Review package contents/dependencies and supported TFMs. When a full build command needs environment-specific setup, record the actual equivalent commands and limitation rather than silently skipping a gate.
- [x] Refresh the PR description using `.github/pull_request_template.md`, keeping Breaking changes and Verification. Organize breaking changes by API/binary, schema/JSON wire contract, runtime/validation, generated code, and downstream migration. Include measured performance with limitations.
- [x] Preserve open migration decisions and distinguish fixed regressions from intentional changes. Explicitly leave the full NSwag companion check unchecked with the user's sequencing decision.
- [x] Verify remote head ownership and branch before pushing `HEAD:feature/migrate-core-to-stj`; if the remote advanced, fetch and integrate its changes without overwriting them.
- [x] Verify Windows and Ubuntu CI on the exact pushed head. Investigate failures and repair those caused by this work. Do not call old-head CI sufficient.
- [x] Report final head, resolved findings, relevant test results, remaining risks, and the next NSwag companion work. Do not merge PR #1914 or release v12.

## Final functional verification

At `74e71db7324143a4a193cf9d020c42915f2fb356`, all five implementation phases and task reviews are complete. The independent whole migration review against `v12` identified F1–F6; their fixes are independently re-reviewed, and the subsequently introduced N1 struct-default omission is repaired and approved. No actionable review findings remain from those reviews.

The unchanged Release command above passed in a clean normal clone at that exact source: **2,353 passed, 17 existing skips, zero failures** across eight local runs. Core net8:924/7 skipped, net9:927/7 skipped; CodeGeneration net8:52, net9:53; CSharp:257; TypeScript:134; Newtonsoft adapter:2; Yaml:4/3 skipped. No AutoVerify snapshots changed. NUKE cannot discover the Codex worktree's `.git` file, so the normal clone supplies the required repository metadata without source/build changes.

All seven production packages have the expected DLL/XML assets, unchanged target frameworks, consistent version `11.6.0-dev-20260912-0128`, and verified dependency groups. Core has no direct Newtonsoft dependency; the adapter retains it. The unchanged benchmark project emits a separate package. Existing NUKE bootstrap dependency advisories and benchmark readme warning remain; no warning-free tooling claim or package release is made.

The final packed-assembly API audit matched each DLL to the full build by hash and source informational version. Public/protected metadata is unchanged from `13dcbd0a`; against the pre-migration baseline, only the documented core serializer migration deltas remain. The unchanged baseline-compiled ToolchainVersion getter consumer runs with the final packed core. This targeted metadata/binding audit is not an exhaustive binary-compatibility guarantee.

The actual NSwag STJ source was inspected at `edd1a7c6d7883240d212788a9f0f64ed0a2da554`. Its full integration gate remains deliberately deferred to [NSwag #5355](https://github.com/RicoSuter/NSwag/pull/5355). Keep the callback/reference, discriminator mapping, parameter override, converter, and generated-client checks from the approved design; use explicit local NJsonSchema project wiring and record both repository heads when that companion work starts. No merge or release is part of this task.

Both Windows and Ubuntu CI passed on the exact functional head `74e71db7`: [run 34665012045](https://github.com/RicoSuter/NJsonSchema/actions/runs/34665012045). Windows includes actual net472 execution. This final documentation checkpoint changes no production/test/build files; any subsequent head status is reported in the [PR verification section](https://github.com/RicoSuter/NJsonSchema/pull/1914). All NJsonSchema plan gates are complete; the downstream NSwag gate above remains open by design.
