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
//   data-confirm-btn-icon      (optional) — Bootstrap icon class (default: "bi-trash")
//
// After-action modes (pick one; without any of these the form submits normally):
//   data-confirm-table         — CSS selector for a DataTable; removes the <tr> via AJAX
//   data-confirm-reload        — if present, POSTs via AJAX then reloads the page
//   data-confirm-remove        — CSS selector resolved from the trigger (closest); removes
//                                the matched element from the DOM after a successful AJAX POST
// ---------------------------------------------------------------------------
(function () {
    let _triggerEl = null;

    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-confirm-action]');
        if (!trigger) return;

        e.preventDefault();

        _triggerEl = trigger;

        const action   = trigger.dataset.confirmAction;
        const title    = trigger.dataset.confirmTitle    || 'Confirm';
        const body     = trigger.dataset.confirmBody     || 'Are you sure?';
        const btnLabel = trigger.dataset.confirmBtnLabel || 'Delete';
        const btnIcon  = trigger.dataset.confirmBtnIcon  || 'bi-trash';

        const modalEl  = document.getElementById('confirmModal');
        const form     = document.getElementById('confirmModalForm');
        const submitBtn = document.getElementById('confirmModalSubmit');

        if (!modalEl || !form || !submitBtn) return;

        document.getElementById('confirmModalLabel').textContent = title;
        document.getElementById('confirmModalBody').textContent  = body;

        // Reset state in case the previous open of this modal completed an AJAX delete
        // (success path doesn't restore disabled/innerHTML; only the failure path does).
        submitBtn.disabled = false;
        submitBtn.className = 'crm-btn-danger';
        submitBtn.innerHTML = `<i class="bi ${btnIcon}" aria-hidden="true"></i> ${btnLabel}`;

        form.action = action;

        mdb.Modal.getOrCreateInstance(modalEl).show();
    });

    document.addEventListener('submit', function (e) {
        if (e.target.id !== 'confirmModalForm') return;
        if (!_triggerEl) return;

        const tableSelector  = _triggerEl.dataset.confirmTable;
        const shouldReload   = _triggerEl.hasAttribute('data-confirm-reload');
        const removeSelector = _triggerEl.dataset.confirmRemove;

        // If none of the AJAX modes are set, let the form submit normally
        if (!tableSelector && !shouldReload && !removeSelector) return;

        e.preventDefault();

        const form      = e.target;
        const submitBtn = document.getElementById('confirmModalSubmit');
        const modalEl   = document.getElementById('confirmModal');

        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Deleting\u2026';

        fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (response) {
            if (!response.ok) throw new Error('Server returned ' + response.status);

            if (tableSelector) {
                var tableEl = document.querySelector(tableSelector);
                if (tableEl && $.fn.dataTable && $.fn.dataTable.isDataTable(tableEl)) {
                    var dt  = $(tableEl).DataTable();
                    var row = $(_triggerEl).closest('tr');
                    dt.row(row).remove().draw(false);
                }
                mdb.Modal.getOrCreateInstance(modalEl).hide();
            } else if (removeSelector) {
                var target = _triggerEl.closest(removeSelector);
                if (target) {
                    target.style.transition = 'opacity 200ms ease';
                    target.style.opacity = '0';
                    setTimeout(function () { target.remove(); }, 200);
                }
                mdb.Modal.getOrCreateInstance(modalEl).hide();
            } else if (shouldReload) {
                window.location.reload();
            }
        })
        .catch(function (err) {
            console.error('Delete failed:', err);
            submitBtn.disabled = false;
            submitBtn.className = 'crm-btn-danger';
            submitBtn.innerHTML = '<i class="bi bi-trash" aria-hidden="true"></i> Delete';
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

// ---------------------------------------------------------------------------
// Unsaved-changes indicator + beforeunload guard
// Any <form data-edit-form> wires up automatically:
//   - shows .crm-unsaved-indicator (within the form) on first input/change
//   - prompts the browser on nav-away while any tracked form is dirty
//   - clears the dirty flag on submit so Save doesn't trigger the prompt
// One window-level beforeunload listener tracks the dirty set across all forms.
// ---------------------------------------------------------------------------
document.addEventListener('DOMContentLoaded', function () {
    const dirtyForms = new Set();

    document.querySelectorAll('form[data-edit-form]').forEach(function (form) {
        const indicator = form.querySelector('.crm-unsaved-indicator');
        function markDirty() {
            if (dirtyForms.has(form)) return;
            dirtyForms.add(form);
            if (indicator) indicator.hidden = false;
        }
        form.addEventListener('input', markDirty);
        form.addEventListener('change', markDirty);
        form.addEventListener('submit', function () { dirtyForms.delete(form); });
    });

    window.addEventListener('beforeunload', function (e) {
        if (dirtyForms.size === 0) return;
        e.preventDefault();
        e.returnValue = '';
        return '';
    });
});

// ---------------------------------------------------------------------------
// Select2 auto-init
// Any <select data-select2> is initialized on load, replacing the per-view
// $('#id').select2({ ... }) script blocks. Optional attributes:
//   data-select2-placeholder="..."  — placeholder text
//   data-select2-tags               — allow free-text entries
//   data-select2-allow-clear        — show the clear (x) control
// ---------------------------------------------------------------------------
document.addEventListener('DOMContentLoaded', function () {
    $('select[data-select2]').each(function () {
        const $el = $(this);
        const options = { width: '100%' };
        if ($el.is('[data-select2-tags]')) options.tags = true;
        if ($el.is('[data-select2-allow-clear]')) options.allowClear = true;
        const placeholder = $el.attr('data-select2-placeholder');
        if (placeholder) options.placeholder = placeholder;
        $el.select2(options);
    });
});

// ---------------------------------------------------------------------------
// Event date field with optional "year unknown"
// A [data-event-date] group keeps a canonical yyyy-MM-dd in its hidden
// [data-event-date-value] input (year 0001 = unknown). The native date picker
// shows when the year is known; month + day selects replace it when it is not.
// Toggling either way carries the month/day across, whether or not a date was
// entered first.
// ---------------------------------------------------------------------------
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-event-date]').forEach(function (group) {
        const hidden = group.querySelector('[data-event-date-value]');
        const full   = group.querySelector('[data-event-date-full]');
        const mdWrap = group.querySelector('[data-event-date-md]');
        const month  = group.querySelector('[data-event-date-month]');
        const day    = group.querySelector('[data-event-date-day]');
        const toggle = group.querySelector('[data-event-date-yearunknown]');
        if (!hidden || !full || !mdWrap || !month || !day || !toggle) return;

        function commit() {
            if (toggle.checked) {
                hidden.value = (month.value && day.value)
                    ? '0001-' + month.value + '-' + day.value
                    : '';
            } else {
                hidden.value = full.value || '';
            }
        }

        function showYearUnknown(on) {
            full.hidden = on;
            mdWrap.hidden = !on;
        }

        // Hide day options past the selected month's end. Year-unknown dates are
        // stored under year 0001 (not a leap year), so February tops out at 28.
        const daysInMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        function clampDays() {
            const max = month.value ? daysInMonth[Number(month.value) - 1] : 31;
            Array.from(day.options).forEach(function (opt) {
                if (opt.value) opt.hidden = Number(opt.value) > max;
            });
            if (day.value && Number(day.value) > max) day.value = '';
        }

        toggle.addEventListener('change', function () {
            const parts = full.value.split('-');
            if (toggle.checked) {
                // Carry the picker's month/day into the selects.
                month.value = parts.length === 3 ? parts[1] : '';
                day.value   = parts.length === 3 ? parts[2] : '';
                clampDays();
            } else if (month.value && day.value) {
                // Carry the selects back, keeping the picker's year (or this year).
                full.value = (parts[0] || new Date().getFullYear()) +
                             '-' + month.value + '-' + day.value;
            }
            showYearUnknown(toggle.checked);
            commit();
        });

        full.addEventListener('change', commit);
        month.addEventListener('change', function () { clampDays(); commit(); });
        day.addEventListener('change', commit);

        clampDays();
        showYearUnknown(toggle.checked);
        commit();
    });
});
