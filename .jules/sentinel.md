## 2024-05-22 - Generic Controller IDOR
**Vulnerability:** `AttachmentsController.Upload` allowed uploading files to any entity ID without verifying ownership/access, leading to IDOR.
**Learning:** Generic controllers that accept raw IDs must explicitly verify access. `AddAsync` does not check global query filters; it blindly adds the entity. Access control via filters only happens on *queries*.
**Prevention:** Always verify entity existence and access rights before performing actions on it (especially Create/Update) in generic endpoints. Use `_repository.ExistsAsync<T>` which respects global query filters.

## 2025-01-30 - File Extension Spoofing
**Vulnerability:** `AttachmentsController` allowed uploading non-image files (e.g. PDF, DOCX) without verifying their file signature (magic bytes), relying only on file extension. This could allow attackers to upload malicious files (e.g. executables) disguised as allowed document types.
**Learning:** Checking file extension is insufficient for security. Validation logic was inconsistent: strict for images but lax for documents. Attackers can easily rename files to bypass extension checks.
**Prevention:** Implement `IsValidFileSignature` to check magic bytes for ALL allowed file types, not just images. Ensure validation logic is centralized and applied uniformly to all uploads.
