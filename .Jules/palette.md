# Palette's Journal

## 2025-05-15 - Reusable Copy-to-Clipboard Button
**Learning:** Implemented a reusable pattern for copy-to-clipboard buttons using `.btn-copy` class and `data-clipboard-text` attribute.
**Action:** Use this pattern for any future copy functionality: `<button class="btn btn-outline-secondary btn-copy" data-clipboard-text="..." title="..." aria-label="..."><i class="bi bi-clipboard"></i></button>`. The global JS handles the click, copy, and feedback (checkmark icon + tooltip).
