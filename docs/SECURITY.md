# Security Guidelines

- **Content Security Policy (CSP):** Restrictions enforced via <meta http-equiv="Content-Security-Policy">.
- **Client Storage:** Sensitive keys are never hardcoded. All client settings reside in isolated localStorage memory cache.
- **Input Sanitization:** All form inputs in Company Settings are validated before storage.