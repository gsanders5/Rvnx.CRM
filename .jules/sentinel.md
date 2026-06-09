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

## 2026-04-16 - Trusted Content-Type Stored XSS
**Vulnerability:** `AttachmentsController` trusted the user-provided `Content-Type` header during file upload. An attacker could upload a `.txt` file with `Content-Type: text/html`, which would be served back to users as HTML, executing malicious scripts (Stored XSS).
**Learning:** Never trust client-provided metadata like `Content-Type`. Browsers often respect this header over file extensions, leading to XSS if not validated.
**Prevention:** Determine the MIME type server-side based on the file content or extension using a strict whitelist. Ignore the client's `Content-Type` header entirely.

## 2024-05-22 - Unvalidated Redirect in GET Action
**Vulnerability:** Found an Unvalidated Redirect (Open Redirect) and potential Reflected XSS in `RelationshipsController.Delete` (GET action). The `returnUrl` parameter was passed directly to the view model without validation, allowing attackers to inject malicious URLs or JavaScript.
**Learning:** Developers often remember to validate `returnUrl` in POST actions (like `DeleteConfirmed`) but overlook GET actions that display the URL in the view (e.g., in a "Cancel" link).
**Prevention:** Always validate `returnUrl` with `Url.IsLocalUrl()` in both GET and POST actions if it is used to redirect or rendered in a link.

## 2026-03-08 - CSP Is Incompatible With Third-Party Social Embeds
**Vulnerability:** Added a Content-Security-Policy header to harden the app. After extensive iteration it was scrapped entirely.
**Learning:** CSP nonce + `strict-dynamic` works well for first-party scripts, but is fundamentally incompatible with social media embeds. Twitch, TikTok, and Twitter load their real JavaScript from dynamic CDN subdomains (e.g. `lf16-tiktok-web.tiktokcdn-us.com`) that encode datacenter/cluster identifiers and change based on load balancing, geography, and deployment. You cannot enumerate these in a static allowlist. Additionally, ASP.NET Core's dev tooling (`_vs/browserLink`, `_framework/aspnetcore-browser-refresh.js`) injects scripts at runtime that cannot be given nonces, breaking hot reload under a strict policy. For a personal app behind authentication, CSP provides minimal security value against the primary threats (which are server-side) while adding significant, permanent maintenance overhead.
**Prevention:** Do not add CSP to this application. The three headers that do matter and have no maintenance cost are retained: `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, and `X-XSS-Protection: 1; mode=block`. If CSP is ever revisited, it must be scoped to routes that do not render social embeds, and the dev/prod split must be handled from day one.

## 2026-02-28 - Insecure Direct Object Reference (IDOR) in RelationshipsController
**Vulnerability:** The `RelationshipsController` actions (`Create` GET/POST, `CreatePartial`, `Edit` GET/POST, `Delete` GET/POST) did not adequately verify that the targeted `entityId` existed and belonged to the current user. An attacker could craft requests with arbitrary Entity IDs to enumerate entities or create unwanted associations, bypassing multi-tenant scoping since ID-based assignment (especially when utilizing generic relationships) does not trigger read-based query filters immediately during the `Create` view rendering.
**Learning:** Even though global query filters protect read operations (`_repository.ListAsync`), generic endpoints taking an `entityId` parameter and building related entity associations (like `Relationship`) must explicitly perform a point query validation (`_entityService.ExistsAsync(...)`) to confirm both the entity's existence and the current user's authorization to access it *before* returning a view model or persisting changes.

**Prevention:** In generic relationship or child-entity controllers, always inject and call `_entityService.ExistsAsync(entityType, entityId)` at the start of both GET (form rendering) and POST (form submission) actions. Return `NotFound()` if validation fails to prevent object enumeration and unauthorized cross-tenant associations.

## 2024-03-12 - Prevent Open Redirects via Referer Header
**Vulnerability:** The `AttachmentsController.SafeRedirect` method used `Redirect(referer)` when returning users to their previous page after operations like upload.
**Learning:** Even with an initial check that the `Uri.Host` matches the server host, returning a full `Redirect` with a potentially manipulatable absolute URL string is generally unsafe and can lead to test assertion mismatches or potential bypasses in specific edge cases of URI parsing.
**Prevention:** Instead of passing the full URL string to `Redirect`, parse it with `Uri.TryCreate`, verify the host, and then explicitly use `LocalRedirect(uri.PathAndQuery)` to guarantee the redirect cannot leave the local domain.
## 2024-03-16 - Add secure cookie options to Authentication setup
**Vulnerability:** Weak default cookie settings (HttpOnly and Secure) not explicitly enforced.
**Learning:** Default cookie settings might not enforce `SecurePolicy` and `SameSiteMode.Strict`, opening up risks to XSS attacks and cross-site requests if cookies are accessible through JavaScript or unencrypted HTTP.
**Prevention:** Always explicitly configure `options.Cookie.HttpOnly = true`, `options.Cookie.SecurePolicy = CookieSecurePolicy.Always`, and `options.Cookie.SameSite = SameSiteMode.Strict` when adding cookie authentication to `builder.Services.AddAuthentication()`.

## 2024-05-24 - Bulk updates bypassing `SaveChangesAsync` with `ExecuteUpdateAsync`
**Vulnerability:** SQL Injection via `ExecuteSqlRawAsync` string concatenation.
**Learning:** In EF Core 9.0+, `ExecuteUpdateAsync` provides a safer and parameterized way to perform bulk updates while preventing SQL injection. However, since it bypasses `SaveChangesAsync` and `CRMDbContext.UpdateAuditFields()`, explicit documentation and fallback mechanisms are required.
**Prevention:** Use `ExecuteUpdateAsync` for bulk operations and provide an `InMemory` tracking fallback for tests. Ensure proper documentation of audit bypass.
## 2026-04-16 - Enforce HttpClient Timeout
**Vulnerability:** The `HttpClient` registered for `IVCardService` in `ServiceCollectionExtensions` had no explicit timeout configured.
**Learning:** By default, `HttpClient` has a very long timeout (often 100 seconds). If an external server hangs during operations like downloading a profile photo, the application thread can block, potentially leading to a Denial of Service (DoS) condition under heavy load.
**Prevention:** Always configure an explicit, reasonable timeout (e.g., `TimeSpan.FromSeconds(10)`) when registering `HttpClient` services in the DI container.
## 2025-02-23 - Open Redirect Defense in Depth
**Vulnerability:** AccountController relied solely on `Url.IsLocalUrl()` to prevent Open Redirects during login.
**Learning:** While `Url.IsLocalUrl()` is standard and generally safe, it only checks if a URL is relative (not absolute). It does not verify if the relative path actually exists or is a safe place to redirect a user within the application, leaving potential room for obscure bypasses or unwanted internal routing if other vulnerabilities exist.
**Prevention:** Implement defense-in-depth by augmenting `Url.IsLocalUrl()` with an explicit `IsUrlInSafelist()` check that verifies the relative path against a known-good list of allowed application routes (e.g., `/`, `/Home`, `/Contacts`). Default to the application root if validation fails.

## 2025-06-05 - 🛡️ Sentinel: [CRITICAL] Fix DoS vulnerability in Immich controller proxy
**Vulnerability:** The `ImmichController.SetAsProfilePhoto` endpoint proxied image downloads from the Immich API into an unbounded memory stream without limits if `Content-Length` was missing (e.g. chunked transfers). An attacker could cause memory exhaustion (DoS).
**Learning:** `CopyToAsync` on streams without pre-validated size limits is dangerous. Using a chunked reading loop with an embedded size check mitigates this vulnerability.
**Prevention:** Whenever buffering external streams into memory, always read in chunks and validate total bytes read against the application's maximum allowed size limit.
