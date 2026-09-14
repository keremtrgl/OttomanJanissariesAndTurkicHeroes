# graph.md → public architecture wiki

Date: 2026-09-14

## Context

`graphify-out/graph.md` (currently ~700 lines) and `graphify-out/index.html` (a 155-node
interactive radial visualization) are this mod's internal architecture documentation, kept current
after every version bump. Both are entirely in Turkish and written for a technical audience. The
mod's GitHub repo is public. The user asked for graph.md to become "a general mod wiki" — this spec
covers turning it into a properly rendered, published site rather than a Markdown file only
readable well inside the repo viewer.

Two technical facts drove the design decisions below:

1. GitHub Pages' default Jekyll renderer does **not** render Mermaid code fences — only GitHub's
   own repo file viewer does. Publishing graph.md as-is via Pages would show raw ` ```mermaid ` code
   blocks, not diagrams.
2. GitHub Pages, when serving from a branch, only supports `/` (repo root) or `/docs` as the site
   root. This repo's `main` branch already has a `docs/` folder — `docs/superpowers/specs/` and
   `docs/superpowers/plans/` — holding this session's own internal design/plan documents. Serving
   Pages from `main`'s `/docs` would accidentally publish those alongside the wiki.

## Decisions (confirmed with user)

- **Rendering**: a hand-built HTML page loading `mermaid.js` from a CDN, not raw Jekyll. Diagrams
  render correctly; the page can reuse the parchment/gold "manuscript" visual identity already
  established this session in the roadmap and audit artifacts, instead of GitHub's default theme.
- **Hosting**: a fresh `gh-pages` branch (orphan, unrelated history to `main`), containing only the
  site's static files. `main` is untouched — no `docs/` collision, no risk of publishing internal
  planning notes.
- **Language**: bilingual, Turkish and English, with a client-side toggle (same interaction pattern
  as the light/dark toggle already used in this session's artifacts — a stored UI preference, not a
  page reload). Code identifiers, file paths, and diagram node labels are language-invariant and
  appear identically in both states, matching how the Steam Workshop store page already handles
  EN+TR. Translation to English is done directly (not via an external translator or additional
  research) — this is well within normal writing capability, unlike the IT/PL/PT localization work
  tracked separately.
- **Push**: `main` gets pushed to `origin/main` to catch it up (the repo is already public, so this
  is not a first-time publish of anything — origin is simply ~30 commits behind local `main` from
  this session's earlier work). `gh-pages` is pushed as a new branch. GitHub Pages is enabled to
  serve from `gh-pages` via `gh` CLI if the session has an authenticated `gh`, otherwise the user is
  told the exact one settings toggle to flip.

## Site structure

Two pages, linked to each other, sharing one visual identity:

1. **`index.html`** — the wiki homepage. One long-scrolling page with a sticky section nav mirroring
   graph.md's existing 10 sections (module map, economy engine, FPS optimizer, multi-doctrine
   tactical AI, character creation, clan/beylik hierarchy, settlement ownership, rival kingdoms,
   tavern companions, version history). Content is graph.md's existing prose and diagrams, carried
   over faithfully — this is a rendering and translation change, not a rewrite of the technical
   content or its depth.
2. **`architecture.html`** — hosts the *existing* interactive radial node graph currently at
   `graphify-out/index.html` (155 nodes, 143+ edges), copied over largely as-is. Linked prominently
   from the wiki homepage as "explore the live dependency graph," not inlined into the long-scroll
   page — it's a different interaction model (pan/zoom/click-to-focus) than prose-with-diagrams.

## Content work

- Full English translation of graph.md's ~700 lines (10 sections + the version-history root-cause
  notes in section 10), produced directly, section by section, preserving all technical specifics
  (decompile-verified engine facts, exact method/property names, version numbers) unchanged —
  only the surrounding prose is translated.
- The existing Turkish text is kept verbatim as the TR state of the toggle; no changes to its
  technical content in this pass.
- Mermaid diagram *source* (node/edge definitions) is shared between both language states — only
  node labels that contain natural-language descriptions get a translated counterpart; diagrams
  are re-rendered per toggle state via two mermaid definitions (TR/EN) swapped by the toggle, not
  by re-diagramming from scratch.

## Out of scope

- Restructuring the underlying technical content (same 10 sections, same depth, same audience
  level) — this pass changes how it's published and adds an English translation, not what it says.
- Any change to `graphify-out/graph.md` or `graphify-out/index.html` themselves — those stay as the
  Turkish, contributor-facing source of truth inside the repo; the new site is a separate,
  published artifact built from them, on its own branch.
- Custom domain setup — Pages ships at its default `github.io` URL.

## Verification

- Local preview of the built `gh-pages` content in a browser before pushing, checking both
  language states render their Mermaid diagrams without console errors.
- After Pages is enabled, fetch the live published URL and confirm both toggle states render and
  the `architecture.html` link works.
