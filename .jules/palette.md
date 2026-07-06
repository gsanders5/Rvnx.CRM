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
## 2026-04-16 - Accessible Pinned Contact Links
**Learning:** Anchor tags containing decorative images (like avatars with `alt=""`) and no text content can be unlabelled for screen readers, even if a `title` attribute is present.
**Action:** Always ensure that links containing only decorative images have an explicit `aria-label` attribute describing their destination or action.
## 2026-04-18 - Accessible Profile Photos in Comparison Contexts
**Learning:** In ASP.NET Core Razor views, when displaying profile images in comparison or side-by-side contexts (e.g., merge screens), using hardcoded generic `alt` text like "Profile Photo" prevents screen reader users from distinguishing between the entities being compared.
**Action:** Always use dynamic, context-specific `alt` text (e.g., `alt="Profile photo of @Model.Contact.FullName"`) for profile images to allow screen reader users to distinguish between entities in comparison views.
