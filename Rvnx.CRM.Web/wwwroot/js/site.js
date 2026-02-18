// Initialize Material Style components
document.addEventListener('DOMContentLoaded', () => {
    // Add ripple surface to buttons
    const buttons = document.querySelectorAll('.btn:not(.btn-link):not(.btn-close)');
    buttons.forEach(btn => {
        if (!btn.querySelector('.ripple-surface')) {
            const ripple = document.createElement('span');
            ripple.classList.add('ripple-surface');
            btn.appendChild(ripple);
        }
    });

    // Initialize Ripple
    const rippleSurface = Array.prototype.slice.call(document.querySelectorAll('.ripple-surface'));
    rippleSurface.map(s => {
        return new mdc.ripple.MDCRipple(s);
    });

    // Initialize Text fields
    var textFieldList = [].slice.call(document.querySelectorAll('.form-control'));
    textFieldList.map(function (textField) {
        return new materialstyle.TextField(textField);
    });

    // Initialize Select fields
    var selectList = [].slice.call(document.querySelectorAll('.form-select'));
    selectList.map(function (select) {
        return new materialstyle.SelectField(select);
    });
});

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
