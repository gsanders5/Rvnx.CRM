# Palette's Journal

## 2025-05-15 - Reusable Copy-to-Clipboard Button
**Learning:** Implemented a reusable pattern for copy-to-clipboard buttons using `.btn-copy` class and `data-clipboard-text` attribute.
**Action:** Use this pattern for any future copy functionality: `<button class="btn btn-outline-secondary btn-copy" data-clipboard-text="..." title="..." aria-label="..."><i class="bi bi-clipboard"></i></button>`. The global JS handles the click, copy, and feedback (checkmark icon + tooltip).

## 2026-02-20 - Standardized Empty State Pattern
**Learning:** List views are cleaner when the empty table structure is hidden.
**Action:** When `Model.Any()` is false, hide the table and display a centered empty state: `<div class="text-center py-5 my-5">` with a large muted icon, descriptive text, and a primary action button.
