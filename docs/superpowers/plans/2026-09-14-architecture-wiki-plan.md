# Architecture Wiki (graph.md → published site) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish `graphify-out/graph.md` and `graphify-out/index.html` as a bilingual (TR/EN), properly-rendered static site on GitHub Pages, on a dedicated `gh-pages` branch that never touches `main`.

**Architecture:** A hand-built two-page static site (`index.html` = translated graph.md content with working Mermaid diagrams and a language toggle; `architecture.html` = the existing 155-node interactive radial visualization, adapted). No build tooling — plain HTML/CSS/JS, `mermaid.js` loaded from a CDN, fonts from Google Fonts. Built on an orphan `gh-pages` branch so it never mixes with `main`'s `docs/superpowers/` internal notes.

**Tech Stack:** Static HTML/CSS/vanilla JS, `mermaid.js` (CDN, pinned version), Google Fonts (Cinzel/Plus Jakarta Sans/IBM Plex Mono — matching this session's established parchment/gold visual identity).

## Global Constraints

- Site language default: Turkish state visible on load (matches graph.md's original audience); an explicit TR/EN toggle switches state, no page reload.
- Every Mermaid diagram must render without console errors in both language states.
- `main` branch is not modified by this plan except for the final `git push origin main` step (syncing already-written local commits, not new content).
- `gh-pages` branch has unrelated history to `main` (orphan branch) and contains only site files — no `docs/superpowers/`, no `Source/`, no `ModuleData/`.
- Code identifiers, file paths, class/method names, and version numbers are identical in both language states — only surrounding prose translates.

---

### Task 1: gh-pages branch scaffold and shared design system

**Files:**
- Create (on new orphan branch `gh-pages`, working from a temporary local checkout directory `../seljuk-wiki-build/` so `main`'s working tree is untouched): `styles.css`, `wiki.js`, `index.html` (empty shell)

**Interfaces:**
- Produces: CSS custom-property tokens (`--bg`, `--paper`, `--paper-2`, `--ink`, `--ink-muted`, `--rule`, `--gold`, `--gold-strong`, `--teal`, `--lapis`, `--brick` — light values on bare `:root`, dark overrides under `@media (prefers-color-scheme: dark)` guarded `:root:not([data-theme="light"])`, and `:root[data-theme="dark"]`, matching the pattern already used in this session's published artifacts) in `styles.css`.
- Produces: `.lang-tr` / `.lang-en` CSS classes (one shown, one `[hidden]` at a time) and a JS function `setLanguage(lang)` in `wiki.js` that: toggles `[hidden]` on every `.lang-tr`/`.lang-en` element, re-runs `mermaid.run()` on the now-visible diagrams (each language has its own `<pre class="mermaid lang-tr">`/`<pre class="mermaid lang-en">` block so diagram labels translate too), and persists the choice in `localStorage` under key `seljuk-wiki-lang` (read on load, defaulting to `"tr"` if unset or on a `localStorage` exception — wrap the read/write in try/catch since this runs as a plain static page with no other error boundary).
- Produces: a sticky `<nav id="toc">` element in `index.html` with 10 placeholder anchor links (`#section-1` .. `#section-10`) and a language-toggle button (`id="lang-toggle"`), plus a `<main>` container with 10 empty `<section id="section-N">` stubs for later tasks to fill.
- Produces: a pinned-version `mermaid.js` `<script>` tag loaded before `wiki.js`, with Subresource Integrity set (see Step 4), and `mermaid.initialize({ startOnLoad: false, theme: 'base' })` called once in `wiki.js` before the first `setLanguage()` call.

- [ ] **Step 1: Create the build checkout**

```bash
cd "C:/Users/Kerem/OneDrive/Ekler/Masaüstü/bannermod"
git worktree add ../seljuk-wiki-build --orphan gh-pages
cd ../seljuk-wiki-build
```

Expected: a new directory `seljuk-wiki-build` with no tracked files (orphan branch, no commits yet).

- [ ] **Step 2: Write `styles.css`**

Write the full token system (light `:root` block with all 11 custom properties listed above, the two dark-mode override blocks, `body { background: var(--bg); color: var(--ink); font-family: 'Plus Jakarta Sans', ui-sans-serif, system-ui, sans-serif; margin: 0; }`), plus layout rules for `#toc` (`position: sticky; top: 0;` with a `.page` max-width container at `960px`), `.lang-tr[hidden], .lang-en[hidden] { display: none; }`, and heading styles using `'Cinzel', serif` for `h1`/`h2` and `'IBM Plex Mono', ui-monospace, monospace` for the eyebrow/nav labels — same font roles as this session's roadmap/audit artifacts.

- [ ] **Step 3: Write `wiki.js`**

```javascript
mermaid.initialize({ startOnLoad: false, theme: 'base' });

function setLanguage(lang) {
  document.querySelectorAll('.lang-tr').forEach(el => el.hidden = lang !== 'tr');
  document.querySelectorAll('.lang-en').forEach(el => el.hidden = lang !== 'en');
  try { localStorage.setItem('seljuk-wiki-lang', lang); } catch (e) {}
  mermaid.run({ querySelector: lang === 'tr' ? '.mermaid.lang-tr' : '.mermaid.lang-en' });
  const btn = document.getElementById('lang-toggle');
  if (btn) btn.textContent = lang === 'tr' ? 'EN' : 'TR';
}

function getInitialLanguage() {
  try {
    const stored = localStorage.getItem('seljuk-wiki-lang');
    if (stored === 'tr' || stored === 'en') return stored;
  } catch (e) {}
  return 'tr';
}

document.addEventListener('DOMContentLoaded', () => {
  const initial = getInitialLanguage();
  setLanguage(initial);
  const btn = document.getElementById('lang-toggle');
  if (btn) {
    btn.addEventListener('click', () => {
      const current = document.querySelector('.lang-en[hidden]') ? 'en' : 'tr';
      setLanguage(current);
    });
  }
});
```

- [ ] **Step 4: Write `index.html` shell**

Include `<!doctype html>`, `<head>` with `<meta charset="utf-8">`, `<title>Seljuk Empire: Sword of Islam — Architecture Wiki</title>`, the Google Fonts `<link>` tags (Cinzel:wght@600;700;900, Plus+Jakarta+Sans:wght@400;500;600;700, IBM+Plex+Mono:wght@500;600), `<link rel="stylesheet" href="styles.css">`, `<script src="wiki.js" defer></script>`.

For the mermaid script tag: look up the current SRI hash for a recent pinned mermaid version from jsdelivr's own hash endpoint before writing the tag —

```bash
curl -s "https://data.jsdelivr.com/v1/packages/npm/mermaid" | grep -o '"version":"[0-9.]*"' | head -1
```

then fetch that exact version's integrity hash from `https://www.jsdelivr.com/package/npm/mermaid` (the site publishes the `<script>` tag with hash pre-filled for each version) and use it verbatim:

```html
<script src="https://cdn.jsdelivr.net/npm/mermaid@<PINNED_VERSION>/dist/mermaid.min.js" integrity="sha384-<HASH_FROM_JSDELIVR>" crossorigin="anonymous"></script>
```

Do not fabricate a hash — if it can't be looked up, load the script without `integrity` rather than with a wrong one, and note that as a follow-up. In `<body>`: a header with the eyebrow/title/dek (bilingual, both language spans present, one hidden per current state), the `<nav id="toc">` with 10 links, a `<button id="lang-toggle">EN</button>`, and 10 empty `<section id="section-N"><h2 class="lang-tr">...</h2><h2 class="lang-en" hidden>...</h2></section>` stubs titled from graph.md's own section headers (translate just the 10 titles now — full body content is Tasks 2-6).

- [ ] **Step 5: Verify the shell renders**

Open `seljuk-wiki-build/index.html` directly in a browser (`file://` path). Confirm: page loads with no console errors, TR content shows by default, clicking the toggle switches to EN content and back, `localStorage` persists the choice across a manual reload.

- [ ] **Step 6: Commit**

```bash
git add styles.css wiki.js index.html
git commit -m "$(cat <<'EOF'
Scaffold bilingual architecture wiki shell on gh-pages

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Sections 1-3 content (module map, economy engine, FPS optimizer)

**Files:**
- Modify: `../seljuk-wiki-build/index.html` (fills `#section-1`, `#section-2`, `#section-3`)

**Interfaces:**
- Consumes: the `.lang-tr`/`.lang-en` toggle convention and `<section id="section-N">` stubs from Task 1.
- Produces: nothing consumed by later tasks — each section task is independent.

- [ ] **Step 1: Read the source content**

Read `graphify-out/graph.md` (in the `main`-branch working tree, e.g. `../graphifty-guncel-kontrol-09155a/graphify-out/graph.md`) sections 1 ("🏗️ 1. Genel Modül & Dosya Bağımlılık Haritası"), 2 ("🪙 2. Selçuklu Kervan Devlet Sigortası"), and 3 ("⚡ 3. Savaş Alanı FPS & Ragdoll Optimizasyon Motoru") in full, including their Mermaid diagram source.

- [ ] **Step 2: Write section 1 (TR + EN)**

Inside `#section-1`, add two Mermaid blocks (`<pre class="mermaid lang-tr">...</pre>` with the diagram exactly as in graph.md, and `<pre class="mermaid lang-en">...</pre>` with the same graph structure but English node labels) and two prose blocks (`<div class="lang-tr">` with graph.md's paragraph text verbatim, `<div class="lang-en" hidden>` with an English translation preserving every technical term — class names, file names, version numbers — unchanged).

- [ ] **Step 3: Write sections 2 and 3 (TR + EN)**

Same pattern as Step 2, for the economy-engine and FPS-optimizer sections, including graph.md's explanatory note in section 3 about `MissionMode.Battle` covering both field battles and sieges.

- [ ] **Step 4: Verify**

Open `index.html` in a browser, navigate to sections 1-3 via the TOC, toggle language, confirm both diagrams render correctly in both states with no console errors and no untranslated leftover text bleeding into the wrong language block.

- [ ] **Step 5: Commit**

```bash
git add index.html
git commit -m "$(cat <<'EOF'
Add wiki sections 1-3: module map, economy engine, FPS optimizer

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Sections 4-5 content (tactical AI doctrine, character creation stages)

**Files:**
- Modify: `../seljuk-wiki-build/index.html` (fills `#section-4`, `#section-5`)

**Interfaces:**
- Consumes: same as Task 2.

- [ ] **Step 1: Read the source content**

Read graph.md section 4 ("🏹 4. Çok Doktrinli Reaktif Taktik Yapay Zeka Motoru", including subsection 4b's behavior-comparison table) and section 5 ("👤 5. Selçuklu Karakter Yaratma Özgeçmiş Aşamaları") in full.

- [ ] **Step 2: Write section 4 (TR + EN)**

Include the Mermaid diagram (both language label sets), the 4b Markdown table translated to an HTML `<table>` (same 4 rows: Şok Süvarisi/Atlı Okçu/Piyade/Yaya Okçu vs their EN equivalents Shock Cavalry/Horse Archer/Infantry/Foot Archer, both old/new behavior columns translated), and the closing paragraph about the three final-review bug classes found across v1.7.9-v1.8.1.

- [ ] **Step 3: Write section 5 (TR + EN)**

Include the Mermaid diagram for the 5 character-creation stages with their option lists, translated.

- [ ] **Step 4: Verify**

Same verification pattern as Task 2 Step 4, scoped to sections 4-5, plus confirm the HTML table renders correctly (not just the diagrams) in both language states.

- [ ] **Step 5: Commit**

```bash
git add index.html
git commit -m "$(cat <<'EOF'
Add wiki sections 4-5: tactical AI doctrine, character creation stages

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Sections 5b-7 content (rival character creation, clan hierarchy, settlement ownership)

**Files:**
- Modify: `../seljuk-wiki-build/index.html` (fills `#section-5b` — insert as part of `#section-5` or its own stub added here — `#section-6`, `#section-7`)

**Interfaces:**
- Consumes: same as Task 2. Note: section 5b was not in Task 1's 10 stubs (graph.md numbers it as a sub-section of 5, not top-level) — add an `<section id="section-5b">` stub and TOC link as part of this task's Step 2, immediately after `#section-5`.

- [ ] **Step 1: Read the source content**

Read graph.md section 5b ("🌏 5b. Rakip Krallıkların Karakter Yaratma Özgeçmişleri"), section 6 ("👑 6. Krallık, Beylikler ve Tarihi Liderler Hiyerarşisi"), and section 7 ("🗺️ 7. Şehirler, Kaleler ve Bağlı Köylerin Mülkiyet Dağılımı") in full, including section 7's Markdown table.

- [ ] **Step 2: Add the section 5b stub and write its content (TR + EN)**

Insert `<section id="section-5b">` right after the `#section-5` closing tag, add its TOC link after section 5's. Fill with the Mermaid diagram (6 rival cultures + the Latin-Empire-has-no-separate-content note) and prose, both languages.

- [ ] **Step 3: Write sections 6 and 7 (TR + EN)**

Section 6: the clan-hierarchy Mermaid diagram, translated. Section 7: the example settlement-ownership table (Yerleşke Türü/Yerleşke Adı/Sahibi Olan Klan/Bağlı Köyler columns → Settlement Type/Name/Owning Clan/Attached Villages) as an HTML `<table>`, plus the closing note pointing to `ModuleData/settlements.xml` and `SeljukSettlementBehavior.cs`.

- [ ] **Step 4: Verify**

Same pattern as prior verify steps, scoped to sections 5b-7, including the two HTML tables.

- [ ] **Step 5: Commit**

```bash
git add index.html
git commit -m "$(cat <<'EOF'
Add wiki sections 5b-7: rival character creation, clan hierarchy, settlements

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Sections 8-9c content (rival kingdoms, companions)

**Files:**
- Modify: `../seljuk-wiki-build/index.html` (fills `#section-8`, `#section-9` with its 9a/9b/9c subsections)

**Interfaces:**
- Consumes: same as Task 2. Note: 9a/9b/9c are sub-anchors within `#section-9`, not separate TOC entries — use `<h3 id="section-9a">` etc. inside the section.

- [ ] **Step 1: Read the source content**

Read graph.md section 8 ("🌍 8. Rakip Krallıklar Hiyerarşisi") including its two note blocks ("Erken oturumlarda yapılan toprak transferleri" and "Bu oturumda tamamlanan içerik"), and section 9 in full (intro + 9a companions-table Mermaid diagram + its "Motor doğrulaması" and "v1.7.4/v1.7.6 düzeltmesi" notes + 9b's 11-wanderer Mermaid diagram + its own fix note + 9c's `verify_mod.py` check list, checks 14-19, with their ERROR/WARN details and the two "yanlış alarm düzeltmeleri" corrections and the withdrawn weapon-parity check note).

- [ ] **Step 2: Write section 8 (TR + EN)**

Diagram + both note blocks, translated, preserving all specific settlement/clan names as proper nouns (transliterated consistently, not translated as common nouns).

- [ ] **Step 3: Write section 9 with 9a/9b/9c (TR + EN)**

This is graph.md's longest section — write it in full: intro paragraph, 9a's companion-tree diagram and its two explanatory notes (engine verification + the two GameText-gap fix histories), 9b's wanderer-archetype diagram and its fix note, 9c's full check-14-through-19 list (each check's ERROR/WARN behavior stated correctly per this session's own audit finding that check 18's *docstring* was backwards — the *wiki text* must describe the actual verified behavior: collision ERROR requires game install, unused-icon WARN does not), the two false-alarm corrections, and the withdrawn-check note.

- [ ] **Step 4: Verify**

Same pattern, scoped to sections 8-9, including the 9c check list's internal consistency (no ERROR/WARN reversal repeated from the source docstring bug).

- [ ] **Step 5: Commit**

```bash
git add index.html
git commit -m "$(cat <<'EOF'
Add wiki sections 8-9: rival kingdoms hierarchy, companions

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Section 10 content (version history)

**Files:**
- Modify: `../seljuk-wiki-build/index.html` (fills `#section-10`)

**Interfaces:**
- Consumes: same as Task 2.

- [ ] **Step 1: Read the source content**

Read graph.md section 10 in full (the version-chain Mermaid diagram from v1.6.1 through v1.8.4, and every root-cause bullet from v1.6.5 through v1.8.4).

- [ ] **Step 2: Write section 10 (TR + EN)**

Diagram translated (version numbers and code identifiers unchanged, descriptive labels translated). Every root-cause bullet translated in full — these are the most technically dense paragraphs in the whole document (decompiled method names, exact field/property behavior); preserve every `code`-formatted identifier exactly, translate only the surrounding explanation.

- [ ] **Step 3: Verify**

Same pattern as prior tasks, scoped to section 10. This is also the point to do a full top-to-bottom pass: click every TOC link in both languages, confirm no section is missing, no `[hidden]` element is visible in the wrong state, no diagram fails to render.

- [ ] **Step 4: Commit**

```bash
git add index.html
git commit -m "$(cat <<'EOF'
Add wiki section 10: version history

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: architecture.html (interactive node graph page)

**Files:**
- Create: `../seljuk-wiki-build/architecture.html`
- Modify: `../seljuk-wiki-build/index.html` (add a prominent link to `architecture.html`)

**Interfaces:**
- Consumes: `graphify-out/index.html`'s existing embedded `rawNodes`/`rawEdges` JSON and rendering logic (read from the `main`-branch working tree, not modified there).

- [ ] **Step 1: Copy the existing visualization**

Copy `graphify-out/index.html` (from the `main` worktree) to `../seljuk-wiki-build/architecture.html` verbatim as a starting point — it's already a complete, self-contained interactive page (151+ nodes, radial layout, `focusNode()` JS API per this session's earlier work).

- [ ] **Step 2: Restyle the page chrome to match the wiki**

Add a small header bar at the top of `architecture.html` (outside the existing visualization's own DOM) linking back to `index.html`, styled with the same `styles.css` tokens (link `styles.css` in its `<head>`) — don't touch the visualization's own internal SVG/canvas rendering logic, only wrap it.

- [ ] **Step 3: Add the link from the wiki homepage**

In `index.html`'s header (near the eyebrow/title, both language states), add a visible button/link: `<a href="architecture.html">` with TR text "Etkileşimli mimari grafiğini keşfet" / EN text "Explore the live dependency graph".

- [ ] **Step 4: Verify**

Open `architecture.html` directly, confirm the interactive graph still works exactly as it did in `graphify-out/index.html` (pan/zoom/click-to-focus), confirm the new header bar doesn't overlap or break the existing layout, confirm the link from `index.html` navigates correctly and the back-link works.

- [ ] **Step 5: Commit**

```bash
git add architecture.html index.html
git commit -m "$(cat <<'EOF'
Add architecture.html: interactive node graph, linked from wiki home

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 8: Push and enable Pages

**Files:** none (git/GitHub operations only)

**Interfaces:** none.

- [ ] **Step 1: Final full verification pass**

Open `index.html`, click every one of the 11 TOC entries (10 sections + 5b) in both TR and EN, confirm every diagram renders, confirm `architecture.html` loads and links back correctly. Fix anything broken before proceeding — this is the last checkpoint before anything goes public.

- [ ] **Step 2: Push `gh-pages`**

```bash
cd "C:/Users/Kerem/OneDrive/Ekler/Masaüstü/bannermod/../seljuk-wiki-build"
git push origin gh-pages
```

Expected: new branch appears on the remote.

- [ ] **Step 3: Push `main`**

```bash
cd "C:/Users/Kerem/OneDrive/Ekler/Masaüstü/bannermod"
git push origin main
```

Expected: origin/main catches up to local main (the repo is already public, so this is not a first-time publish of anything, per the design spec).

- [ ] **Step 4: Enable GitHub Pages**

```bash
gh api -X POST "repos/{owner}/{repo}/pages" -f "source[branch]=gh-pages" -f "source[path]=/" 2>&1 || echo "gh CLI unavailable or insufficient permissions — tell the user to enable Pages manually: repo Settings > Pages > Source: gh-pages branch, / (root)"
```

If the `gh api` call fails (no auth, wrong scope, or `gh` not installed), report the exact manual toggle to the user instead of retrying blindly.

- [ ] **Step 5: Confirm the live URL**

Fetch `https://<owner>.github.io/<repo>/` (construct from the actual `origin` remote) after Pages has had a minute to build, confirm the page loads, the language toggle works, and `architecture.html` is reachable at `https://<owner>.github.io/<repo>/architecture.html`.

- [ ] **Step 6: Remove the temporary build worktree**

```bash
git worktree remove ../seljuk-wiki-build
```

Expected: the temporary checkout directory is cleaned up; the `gh-pages` branch itself remains on the remote and in the local repo's refs.
