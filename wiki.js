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
