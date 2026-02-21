// MDB auto-initializes components via data attributes — no manual init needed.

document.addEventListener('click', function (e) {
    const btn = e.target.closest('.btn-copy');
    if (!btn) return;

    const text = btn.getAttribute('data-clipboard-text');
    if (!text) return;

    navigator.clipboard.writeText(text).then(() => {
        const icon = btn.querySelector('i');
        const originalIconClass = btn.dataset.originalIcon || icon.className;
        const originalTitle = btn.dataset.originalTitle || btn.getAttribute('title');

        if (!btn.dataset.originalIcon) {
            btn.dataset.originalIcon = originalIconClass;
            btn.dataset.originalTitle = originalTitle;
        }

        // Change icon to check
        icon.className = 'bi bi-check-lg';
        btn.setAttribute('title', 'Copied!');

        // Clear existing timeout
        if (btn.dataset.timeoutId) {
            clearTimeout(parseInt(btn.dataset.timeoutId));
        }

        const timeoutId = setTimeout(() => {
            icon.className = originalIconClass;
            btn.setAttribute('title', originalTitle);
            delete btn.dataset.timeoutId;
        }, 2000);

        btn.dataset.timeoutId = timeoutId;
    }).catch(err => {
        console.error('Failed to copy: ', err);
    });
});
