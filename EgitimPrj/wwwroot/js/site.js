(() => {
  'use strict';

  const safeHtml = (value) => {
    const s = String(value ?? '');
    return s
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  };

  const fetchJson = async (url, options = undefined) => {
    const res = await fetch(url, options);
    if (!res.ok) {
      let errorText = '';
      try { errorText = await res.text(); } catch { /* ignore */ }
      const suffix = errorText ? `: ${errorText}` : '';
      throw new Error(`HTTP ${res.status}${suffix}`);
    }
    return res.json();
  };

  const togglePassword = (inputId, iconId) => {
    const input = document.getElementById(inputId);
    const icon = document.getElementById(iconId);
    if (!input || !icon) return;

    if (input.type === 'password') {
      input.type = 'text';
      icon.className = 'bi bi-eye-slash';
    } else {
      input.type = 'password';
      icon.className = 'bi bi-eye';
    }
  };

  window.AppUtils = Object.freeze({
    safeHtml,
    fetchJson,
    togglePassword,
  });

  // Backward-compatible global for inline onclick handlers
  window.togglePassword = togglePassword;
})();
