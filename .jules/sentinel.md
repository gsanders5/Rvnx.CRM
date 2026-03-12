## 2024-03-12 - Prevent Open Redirects via Referer Header
**Vulnerability:** The `AttachmentsController.SafeRedirect` method used `Redirect(referer)` when returning users to their previous page after operations like upload.
**Learning:** Even with an initial check that the `Uri.Host` matches the server host, returning a full `Redirect` with a potentially manipulatable absolute URL string is generally unsafe and can lead to test assertion mismatches or potential bypasses in specific edge cases of URI parsing.
**Prevention:** Instead of passing the full URL string to `Redirect`, parse it with `Uri.TryCreate`, verify the host, and then explicitly use `LocalRedirect(uri.PathAndQuery)` to guarantee the redirect cannot leave the local domain.
