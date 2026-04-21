# NJsonSchema v12 Changelog

Running record of changes on the `v12` branch and migration guidance for users upgrading from v11.

See [`plan_v12.md`](./plan_v12.md) for the v12 scope, branch model, and release plan.

---

## Unreleased

### Breaking changes

- **Set up v12 integration branch and CI triggers** (PR [#1924](https://github.com/RicoSuter/NJsonSchema/pull/1924)) — no user-facing impact; infrastructure only.

Planned (not yet merged — track via linked PRs):

- **STJ core migration** — PR [#1914](https://github.com/RicoSuter/NJsonSchema/pull/1914). Multiple breaking API changes; see the PR body for the authoritative list.
- **JsonTypeInfo-based property discovery** for net8.0+ — PR [#1917](https://github.com/RicoSuter/NJsonSchema/pull/1917).
- **`SchemaType` enum expansion** — differentiate OpenAPI 3.0 from 3.1 (currently both map to `SchemaType.OpenApi3`).
- **Reference resolution fixes** — close the `$ref` sibling-keyword gaps documented in [`references.md`](./references.md) by generalizing the current `!IsEnumeration` wrapper-reference guard so other sibling keywords (`description`, `default`, `readOnly`, `title`, and `const` once it lands) are preserved on resolution.

### New features

*(to be filled as PRs merge)*

### Fixes

*(to be filled as PRs merge)*

---

## Migration guide (v11 → v12)

Intended as a running "how do I upgrade" companion. Each section is added as breaking changes land on the `v12` branch.

### System.Text.Json replaces Newtonsoft.Json in the core

*(placeholder — to be filled when PR #1914 merges)*

- What changes for consumers
- Before/after code snippets
- How to keep Newtonsoft.Json behavior (reference `NJsonSchema.NewtonsoftJson`)
- Notes on known behavioral differences

### `ValidationError.Token` type change

*(placeholder — to be filled when PR #1914 merges)*

### `OpenApiDiscriminator.Mapping` now has a setter

*(placeholder — to be filled when PR #1914 merges)*

### `SchemaType` enum expansion

*(placeholder — to be filled when the enum change lands)*

### Reference resolution: siblings of `$ref` are now preserved

*(placeholder — to be filled when the reference-resolution fixes land)*

---

## Contributing

When merging a v12 PR that includes a user-visible change:

1. Add an entry under `Unreleased → Breaking changes` / `New features` / `Fixes`.
2. If it breaks v11 consumers, also add a section under **Migration guide** with a before/after example.
3. Keep entries concise; link to the merged PR for full detail.
