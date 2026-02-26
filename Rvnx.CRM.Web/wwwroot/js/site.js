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

$(function() {
    // Global form submission loading state
    $('form').on('submit', function() {
        var $form = $(this);

        // Don't show spinner if form has target="_blank" (e.g. download)
        if ($form.attr('target') === '_blank') return;

        // Check if validation is available and passes
        if (typeof $form.valid === 'function' && !$form.valid()) {
            return;
        }

        var $btn = $form.find('button[type="submit"]:not([disabled])');
        if ($btn.length === 0) return;

        var $icon = $btn.find('i');
        if ($icon.length) {
            // Store original class to restore if needed (though page usually reloads)
            $icon.data('original-class', $icon.attr('class'));
            $icon.removeClass().addClass('spinner-border spinner-border-sm');
        } else {
            $btn.prepend('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>');
        }

        // Disable button to prevent double submit
        $btn.prop('disabled', true);

        // Timeout to re-enable button after 10 seconds in case of network issue or 204 No Content
        setTimeout(function() {
            $btn.prop('disabled', false);
            if ($icon.length) {
                $icon.attr('class', $icon.data('original-class'));
            } else {
                $btn.find('.spinner-border').remove();
            }
        }, 10000);
    });
});
