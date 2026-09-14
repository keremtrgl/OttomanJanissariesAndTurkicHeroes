mermaid.initialize({ startOnLoad: false, theme: 'base' });

function setLanguage(lang) {
  document.querySelectorAll('.lang-tr').forEach(el => el.hidden = lang !== 'tr');
  document.querySelectorAll('.lang-en').forEach(el => el.hidden = lang !== 'en');
  try { localStorage.setItem('seljuk-wiki-lang', lang); } catch (e) {}
  mermaid.run({ querySelector: lang === 'tr' ? '.mermaid.lang-tr' : '.mermaid.lang-en' });
  document.documentElement.lang = lang;
  const btn = document.getElementById('lang-toggle');
  if (btn) btn.textContent = lang === 'tr' ? 'EN' : 'TR';
  // TR and EN nav labels differ in length, so #toc can wrap to a different
  // number of rows per language at some viewport widths — re-measure so a
  // TOC-link click right after a toggle still lands below the nav.
  updateTocOffset();
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

// The sticky #toc wraps to a different number of rows (and so a different
// height) depending on viewport width, so a fixed CSS offset can't keep
// anchor/TOC-link scrolling from landing a heading under the nav at every
// width. Measure the nav's real height into --toc-h, which styles.css
// reads via `html { scroll-padding-top: var(--toc-h) }`.
function updateTocOffset() {
  const toc = document.getElementById('toc');
  if (toc) document.documentElement.style.setProperty('--toc-h', toc.offsetHeight + 'px');
}

// Re-apply the current #section-N hash once the offset is correct, since
// the browser's own scroll-to-fragment on initial load can happen before
// fonts finish loading (which reflows the nav) or before this script runs.
// Uses a manual scroll calculation rather than target.scrollIntoView(),
// since scrollIntoView() does not consistently honor scroll-padding-top.
function reapplyHashScroll() {
  if (!location.hash) return;
  const target = document.querySelector(location.hash);
  const toc = document.getElementById('toc');
  if (!target) return;
  const offset = toc ? toc.offsetHeight : 0;
  const y = target.getBoundingClientRect().top + window.scrollY - offset;
  window.scrollTo(0, Math.max(y, 0));
}

window.addEventListener('load', () => {
  updateTocOffset();
  reapplyHashScroll();
});
window.addEventListener('resize', updateTocOffset);
if (document.fonts && document.fonts.ready) {
  document.fonts.ready.then(() => {
    updateTocOffset();
    reapplyHashScroll();
  }).catch(() => {});
}
