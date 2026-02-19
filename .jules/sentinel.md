## 2024-05-22 - Generic Controller IDOR
**Vulnerability:** `AttachmentsController.Upload` allowed uploading files to any entity ID without verifying ownership/access, leading to IDOR.
**Learning:** Generic controllers that accept raw IDs must explicitly verify access. `AddAsync` does not check global query filters; it blindly adds the entity. Access control via filters only happens on *queries*.
**Prevention:** Always verify entity existence and access rights before performing actions on it (especially Create/Update) in generic endpoints. Use `_repository.ExistsAsync<T>` which respects global query filters.
