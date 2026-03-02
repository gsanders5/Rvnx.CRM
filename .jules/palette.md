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

## 2025-05-24 - Skip to Content Pattern
**Learning:** Keyboard users often have to tab through long navigation menus before reaching the main content, which is a significant accessibility barrier.
**Action:** Implement a "Skip to content" link immediately after the `<body>` tag using standard Bootstrap classes:
```html
<a href="#main-content" class="visually-hidden-focusable position-absolute top-0 start-0 p-3 z-3 bg-primary text-white text-decoration-none rounded-bottom-end">Skip to main content</a>
```
Ensure the target container has `id="main-content"` and `tabindex="-1"` to correctly receive focus.

## 2026-02-28 - ARIA labels for Icon-Only Graph Controls
**Learning:** Found several icon-only buttons in the Home Dashboard network graph missing `aria-label` attributes.
**Action:** Ensure all controls intended for graph manipulation (like zoom and fullscreen buttons) always include `aria-label` attributes to make their function clear to screen reader users, especially when they only contain an icon (like `bi-arrows-fullscreen`).

## 2024-03-01 - [ARIA Labels for Bootstrap Icons]
**Learning:** Bootstrap Icons in action buttons (`btn-outline-primary`, `btn-outline-danger`) often lack `aria-label` attributes for screen readers, especially in generic CRUD views like `Labels` and `SignificantDates`. The `title` attribute is visually helpful for hover tooltips but `aria-label` provides a more robust screen reader experience.
**Action:** When adding or auditing icon-only action buttons in standard CRUD tables, consistently verify that `aria-label` is present and interpolates the relevant entity name (e.g., `@label.Name` or `@item.Title`) for context.
