# Palette's Journal

## 2025-05-15 - Reusable Copy-to-Clipboard Button
**Learning:** Implemented a reusable pattern for copy-to-clipboard buttons using `.btn-copy` class and `data-clipboard-text` attribute.
**Action:** Use this pattern for any future copy functionality: `<button class="btn btn-outline-secondary btn-copy" data-clipboard-text="..." title="..." aria-label="..."><i class="bi bi-clipboard"></i></button>`. The global JS handles the click, copy, and feedback (checkmark icon + tooltip).

## 2026-02-20 - Standardized Empty State Pattern
**Learning:** List views are cleaner when the empty table structure is hidden.
**Action:** When `Model.Any()` is false, hide the table and display a centered empty state: `<div class="text-center py-5 my-5">` with a large muted icon, descriptive text, and a primary action button.

## 2025-05-24 - DataTables Mobile Layout Fix
**Learning:** DataTables default buttons (like column visibility) can stretch awkwardly or have layout issues on mobile screens when using Bootstrap integration.
**Action:** Use the following CSS pattern to reset button behavior on mobile viewports:
```css
@media screen and (max-width: 767px) {
    .dt-buttons.btn-group {
        box-shadow: none !important;
        width: auto !important;
        display: inline-flex !important;
    }
    .dt-buttons .buttons-colvis {
        width: auto !important;
        flex: 0 0 auto !important;
    }
}
```
**Also:** Ensure responsive button groups use `d-grid gap-2 d-md-flex` for full-width stacking on mobile and inline layout on desktop.
