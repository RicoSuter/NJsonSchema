# NJsonSchema v12

Scope, branch model, release plan, and running changelog for the v12 major release.

## Scope

v12 bundles the breaking improvements that were deferred during the v11.x series:

- **STJ core migration** (PR [#1914](https://github.com/RicoSuter/NJsonSchema/pull/1914)) — replace Newtonsoft.Json with System.Text.Json in the NJsonSchema core. Newtonsoft.Json support stays in `NJsonSchema.NewtonsoftJson`. Multiple breaking API changes (documented in the PR body).
- **JsonTypeInfo-based property discovery** (PR [#1917](https://github.com/RicoSuter/NJsonSchema/pull/1917)) — on net8.0+, use STJ's `JsonTypeInfo` for reflection instead of the custom reflection code. Performance and AOT friendliness.
- **`SchemaType` enum expansion** — differentiate OpenAPI 3.0 and 3.1 (3.1 = JSON Schema 2020-12). Currently `OpenApi3` conflates both, which blocks 3.1-specific behavior (`$ref` siblings, native `const`, type-array nullability).
- **Reference resolution fixes** — close the `$ref` sibling-keyword gaps documented in [`docs/references.md`](./references.md). Generalize the existing `!IsEnumeration` guard on `HasAllOfSchemaReference` / `HasOneOfSchemaReference` / `HasAnyOfSchemaReference` so that `description`, `default`, `readOnly`, `title`, etc. (and `const` once it lands) are preserved rather than dropped when a schema is classified as a wrapper reference.
- **Opportunistic cleanups** deferred for being breaking — deprecated API removal, rename-risk members, consolidation of access paths.

## Branch model

`v12` is the integration branch. `master` continues to ship v11.x.

```
master (v11.x stable) ────o────o────o────o────o────o──── v11.x patches
                           \                    \
                            \ periodic merges    \
                             ↓                    ↓
v12 (integration) ─────o────o────O────O────O────O────── release v12.0.0 → merge to master
                        ↑
                   feature PRs target v12
```

Rules:

- `master` stays the v11.x line. Non-breaking bug fixes and small features land on `master` and release as v11.6.x.
- All v12 feature PRs target `v12`.
- `master` is merged into `v12` after each v11.x patch release (or weekly) to keep v12 fresh and minimise the final merge conflict surface.
- When v12 is feature-complete and stable, a final PR merges `v12` into `master` (`--no-ff`), it's tagged `v12.0.0`, and packages are published.

## Development workflow with NSwag v15

NSwag v15 (its downstream consumer) currently builds against local NJsonSchema via `UseLocalNJsonSchemaProjects=true` project references. Practical implications:

- Breaking changes in NJsonSchema v12 surface as NSwag v15 build failures immediately — mechanical enforcement of the "Cross-check with NSwag" rule in [`CLAUDE.md`](../CLAUDE.md).
- Developers clone both repos as siblings (`../NJsonSchema` on `v12`, `../NSwag` on `v15`).
- NSwag `v15` branch CI checks out both repos to replicate the sibling layout.

When v12 is feature-complete and entering external validation, NSwag v15 will flip to `UseLocalNJsonSchemaProjects=false` and consume NJsonSchema v12 preview NuGet packages.

## Release plan

Ordered by the NJsonSchema → NSwag dependency:

1. **NJsonSchema v12** stabilizes, merges to `master`, is tagged `v12.0.0`, published to NuGet.
2. **NSwag v15** (see NSwag's `docs/plan_v15.md`) bumps its dependency to `NJsonSchema 12.0.0`, stabilizes, is tagged `v15.0.0`.

v11.x patches continue to ship from `master` until superseded.

## Contributing to v12

- Open PRs against `v12`, not `master`.
- Confirm in the PR body that the change has been cross-checked against NSwag `v15` (see [`CLAUDE.md`](../CLAUDE.md) → Cross-check with NSwag). For backend-only changes an inline "N/A — no NSwag surface affected" is fine.
- Non-breaking bug fixes unrelated to v12 scope should target `master` (v11.x) — they will be brought into v12 via periodic master→v12 merges.

## Pre-release cleanup checklist

Before the final `v12` → `master` merge, revert the temporary shims that only make sense while v12 is a live branch:

- **`build/Build.CI.GitHubActions.cs`**: remove `"v12"` from `OnPullRequestBranches` and `OnPushBranches`. The `master` / `main` triggers alone are sufficient once v12 is merged.
- **`.github/workflows/build.yml`, `.github/workflows/pr.yml`**: regenerate from NUKE (`nuke --generate-configuration GitHubActions_build --host GitHubActions` and the `pr` equivalent) so the `v12` branch entry is removed from the generated YAML.
- **NuGet preview-package publishing** (if enabled on the `v12` branch during Phase 2): disable or remove the preview feed configuration.
- **`docs/plan_v12.md` and `docs/changelog_v12.md`**: move or archive after release. Either delete once v12 is GA, or relocate to `docs/releases/` as historical context.
- **Any `[Obsolete]` shims or temporary wrappers** added specifically to ease the v11→v12 migration: remove or mark for removal in v13.
- **Cross-check with NSwag `v15`** — see its `docs/plan_v15.md` cleanup checklist (e.g. removing its sibling-checkout CI step, re-enabling package references by default).

Add items to this checklist as new temporary shims are introduced during v12 development, so nothing leaks into the final release.

## Changelog and migration guide

See [`changelog_v12.md`](./changelog_v12.md) for the running list of landed changes and the v11 → v12 migration guide. Every PR merged to `v12` that has user-visible impact should update that file (see its `Contributing` section).
