// MDB auto-initializes components via data attributes — no manual init needed.

// ---------------------------------------------------------------------------
// Generic confirm modal
// Any element with [data-confirm-action] triggers #confirmModal before POSTing.
//
// Supported data attributes on the trigger element:
//   data-confirm-action        (required) — form POST URL
//   data-confirm-title         (optional) — modal heading  (default: "Confirm")
//   data-confirm-body          (optional) — modal body text (default: "Are you sure?")
//   data-confirm-btn-label     (optional) — submit button label (default: "Delete")
//   data-confirm-btn-class     (optional) — submit button CSS class (default: "btn-danger")
//   data-confirm-btn-icon      (optional) — Bootstrap icon class (default: "bi-trash")
//   data-confirm-table         (optional) — CSS selector for a DataTable; when set, the
//                                           POST is sent via fetch and the containing <tr>
//                                           is removed from the table without a page reload.
// ---------------------------------------------------------------------------
(function () {
    let _triggerEl = null;   // button that opened the current modal

    // Open modal on trigger click
    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-confirm-action]');
        if (!trigger) return;

        e.preventDefault();

        _triggerEl = trigger;

        const action   = trigger.dataset.confirmAction;
        const title    = trigger.dataset.confirmTitle    || 'Confirm';
        const body     = trigger.dataset.confirmBody     || 'Are you sure?';
        const btnLabel = trigger.dataset.confirmBtnLabel || 'Delete';
        const btnClass = trigger.dataset.confirmBtnClass || 'btn-danger';
        const btnIcon  = trigger.dataset.confirmBtnIcon  || 'bi-trash';

        const modalEl  = document.getElementById('confirmModal');
        const form     = document.getElementById('confirmModalForm');
        const submitBtn = document.getElementById('confirmModalSubmit');

        if (!modalEl || !form || !submitBtn) return;

        // Populate modal content
        document.getElementById('confirmModalLabel').textContent = title;
        document.getElementById('confirmModalBody').textContent  = body;

        // Update submit button appearance
        submitBtn.className = `btn ${btnClass}`;
        submitBtn.innerHTML = `<i class="bi ${btnIcon} me-1"></i> ${btnLabel}`;

        // Point the form at the right endpoint
        form.action = action;

        // Show modal
        mdb.Modal.getOrCreateInstance(modalEl).show();
    });

    // Handle AJAX submission when data-confirm-table is present on the trigger
    document.addEventListener('submit', function (e) {
        if (e.target.id !== 'confirmModalForm') return;
        if (!_triggerEl) return;

        const tableSelector = _triggerEl.dataset.confirmTable;
        if (!tableSelector) return;   // no table — fall through to normal form submit

        e.preventDefault();

        const form     = e.target;
        const submitBtn = document.getElementById('confirmModalSubmit');
        const modalEl  = document.getElementById('confirmModal');

        // Disable button and show spinner while waiting
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Deleting…';

        const formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (response) {
            if (!response.ok) throw new Error('Server returned ' + response.status);

            // Remove the row from DataTable without resetting to page 1
            const tableEl = document.querySelector(tableSelector);
            if (tableEl && typeof $.fn !== 'undefined' && $.fn.dataTable && $.fn.dataTable.isDataTable(tableEl)) {
                const dt  = $(tableEl).DataTable();
                const row = $(_triggerEl).closest('tr');
                dt.row(row).remove().draw(false);
            }

            mdb.Modal.getOrCreateInstance(modalEl).hide();
        })
        .catch(function (err) {
            console.error('Delete failed:', err);
            // Re-enable button so the user can retry or submit normally
            submitBtn.disabled = false;
            submitBtn.innerHTML = submitBtn.innerHTML.replace(/<span.*?<\/span>\s*/, '');
        })
        .finally(function () {
            _triggerEl = null;
        });
    });
}());


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

        icon.className = 'bi bi-check-lg';
        btn.setAttribute('title', 'Copied!');

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

document.addEventListener('change', function(e) {
    if (!e.target.matches('input[type="file"][data-preview-target]')) return;

    const input = e.target;
    const targetSelector = input.getAttribute('data-preview-target');
    const targetImg = document.querySelector(targetSelector);

    if (!targetImg) return;

    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function(e) {
            targetImg.src = e.target.result;
            targetImg.classList.remove('d-none');
        }
        reader.readAsDataURL(input.files[0]);
    } else {
        targetImg.classList.add('d-none');
        targetImg.src = '';
    }
});

$(function() {
    $('form').on('submit', function() {
        var $form = $(this);

        // Don't show spinner if form has target="_blank" (e.g. download)
        if ($form.attr('target') === '_blank') return;

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
