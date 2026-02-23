## 2024-05-22 - Generic Controller IDOR
**Vulnerability:** `AttachmentsController.Upload` allowed uploading files to any entity ID without verifying ownership/access, leading to IDOR.
**Learning:** Generic controllers that accept raw IDs must explicitly verify access. `AddAsync` does not check global query filters; it blindly adds the entity. Access control via filters only happens on *queries*.
**Prevention:** Always verify entity existence and access rights before performing actions on it (especially Create/Update) in generic endpoints. Use `_repository.ExistsAsync<T>` which respects global query filters.

## 2025-01-30 - File Extension Spoofing
**Vulnerability:** `AttachmentsController` allowed uploading non-image files (e.g. PDF, DOCX) without verifying their file signature (magic bytes), relying only on file extension. This could allow attackers to upload malicious files (e.g. executables) disguised as allowed document types.
**Learning:** Checking file extension is insufficient for security. Validation logic was inconsistent: strict for images but lax for documents. Attackers can easily rename files to bypass extension checks.
**Prevention:** Implement `IsValidFileSignature` to check magic bytes for ALL allowed file types, not just images. Ensure validation logic is centralized and applied uniformly to all uploads.

## 2026-02-21 - Open Redirect in OIDC Login
**Vulnerability:** `AccountController.Login` directly passed the `returnUrl` query parameter to `AuthenticationProperties.RedirectUri`. This allowed attackers to craft a malicious login link redirecting authenticated users to an external phishing site.
**Learning:** The OIDC `ChallengeResult` behavior with `RedirectUri` redirects the user *after* successful authentication (via the middleware's callback). This redirection is not automatically validated as local.
**Prevention:** Always validate return URLs using `Url.IsLocalUrl()` before passing them to authentication properties or any redirection logic. Default to the application root if invalid.

## 2026-03-10 - IDOR in Related Entity Creation
**Vulnerability:** `NotesController.Create` accepted an `entityId` to link a note to a contact but failed to verify if the current user had access to that contact. This allowed attackers to attach notes to contacts belonging to other users.
**Learning:** Even non-generic controllers that manage related entities (like Notes, Reminders) are vulnerable to IDOR if they blindly trust the parent `entityId` during creation. `AddAsync` does not trigger query filters.
**Prevention:** Explicitly verify existence and access to the parent entity using `_repository.ExistsAsync<ParentT>(entityId)` before creating the child entity.

## 2026-05-15 - Trusted Content-Type Stored XSS
**Vulnerability:** `AttachmentsController` trusted the user-provided `Content-Type` header during file upload. An attacker could upload a `.txt` file with `Content-Type: text/html`, which would be served back to users as HTML, executing malicious scripts (Stored XSS).
**Learning:** Never trust client-provided metadata like `Content-Type`. Browsers often respect this header over file extensions, leading to XSS if not validated.
**Prevention:** Determine the MIME type server-side based on the file content or extension using a strict whitelist. Ignore the client's `Content-Type` header entirely.
