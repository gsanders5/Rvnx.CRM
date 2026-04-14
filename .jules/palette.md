## 2024-04-08 - Adding aria-hidden to decorative icons
**Learning:** Bootstrap Icons (and other purely decorative icon tags) inside wrappers that already convey their meaning (like a span with a title attribute) are read redundantly by screen readers unless hidden.
**Action:** When adding decorative visual elements like `<i class="bi ...">` inside elements that already contain text or tooltips explaining the intent, always add `aria-hidden="true"` to prevent screen readers from announcing redundant content.
## 2024-04-14 - Accessible Header Navigation
**Learning:** Anchor tags containing only an image with an empty `alt` attribute are invisible to screen readers unless given an `aria-label`. Similarly, interactive controls (like toggle buttons) that contain visual icons inside them and an `aria-label` should have `aria-hidden="true"` applied to their internal icons to prevent screen readers from reading them redundantly.
**Action:** When adding or maintaining header navigation containing branding images or toggle buttons, ensure anchor tags with decorative images have descriptive `aria-label`s and inner icon tags within labeled buttons have `aria-hidden="true"`.
