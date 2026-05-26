## 2026-04-08 - Adding aria-hidden to decorative icons
**Learning:** Bootstrap Icons (and other purely decorative icon tags) inside wrappers that already convey their meaning (like a span with a title attribute) are read redundantly by screen readers unless hidden.
**Action:** When adding decorative visual elements like `<i class="bi ...">` inside elements that already contain text or tooltips explaining the intent, always add `aria-hidden="true"` to prevent screen readers from announcing redundant content.
## 2026-04-14 - Accessible Header Navigation
**Learning:** Anchor tags containing only an image with an empty `alt` attribute are invisible to screen readers unless given an `aria-label`. Similarly, interactive controls (like toggle buttons) that contain visual icons inside them and an `aria-label` should have `aria-hidden="true"` applied to their internal icons to prevent screen readers from reading them redundantly.
**Action:** When adding or maintaining header navigation containing branding images or toggle buttons, ensure anchor tags with decorative images have descriptive `aria-label`s and inner icon tags within labeled buttons have `aria-hidden="true"`.
## 2026-04-15 - Unlabelled Links from Decorative Images
**Learning:** Anchor tags (`<a>`) that wrap decorative images with `alt=""` and contain no actual text content are completely invisible to screen readers, resulting in unlabelled links.
**Action:** When adding anchor tags that only contain an image, verify that if the image has `alt=""`, the parent `<a>` tag has an explicit `aria-label` describing the destination.
## 2026-04-16 - Prevent Redundant Reading of Icons within Labelled Containers
**Learning:** Bootstrap Icons (and other purely decorative icon tags) inside wrappers that already convey their meaning (like buttons or tabs with text) are read redundantly by screen readers unless hidden.
**Action:** When adding decorative visual elements like `<i class="bi ...">` inside elements that already contain text or tooltips explaining the intent, always add `aria-hidden="true"` to prevent screen readers from announcing redundant content. Ensure the attribute is only added once.
## 2024-05-26 - Add aria-hidden to dynamically injected icons
**Learning:** Decorative icons injected dynamically via JavaScript (e.g. `innerHTML`) also need `aria-hidden="true"` to prevent screen readers from reading them redundantly.
**Action:** Ensure dynamically injected icons (like Bootstrap Icons `<i class="bi ...">`) have `aria-hidden="true"` when used as purely decorative elements alongside textual labels or inside elements with `aria-label`.
