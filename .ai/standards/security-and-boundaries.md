# Security and Boundary Standards

Use this file when changes involve authentication, authorization, secrets, credentials, tokens, keys, file paths, permissions, IPC, named pipes, network calls, uploads/downloads, external processes, installers/updaters, privacy, logging, encryption, trust boundaries, or dependency safety.

These standards apply proportionally to the project risk level. A local utility and a multi-user service have different threat models, but both need safe defaults and honest boundaries.

## Core principles

- Treat external input as untrusted until parsed, validated, and authorized when applicable.
- Keep trust boundaries visible in code, UI, logs, and documentation.
- Fail closed for safety- or security-affecting uncertainty.
- Use least privilege.
- Do not log or persist secrets accidentally.
- Do not rely on UI restrictions as authorization.
- Do not execute arbitrary remote or user-controlled commands.
- Do not report secure, verified, trusted, or authorized state unless the required proof actually exists.

## Threat model

For security-relevant changes, identify assets being protected, trusted and untrusted inputs, users/roles/processes involved, local machine boundaries, network or IPC boundaries, filesystem boundaries, database boundaries, external service boundaries, failure impact, and acceptable recovery behavior.

Document the threat model when the decision affects future maintenance.

## Secrets

Secrets include passwords, API keys, access tokens, refresh tokens, private keys, connection strings with credentials, recovery codes, signing keys, and sensitive certificates.

- Do not commit secrets.
- Do not put secrets in source code, sample config, logs, reports, telemetry, screenshots, or test output.
- Store secrets using the project-approved secret store.
- Mask secrets by default in UI and logs.
- Provide deliberate reveal/copy flows only when needed.
- Avoid passing secrets through broad configuration objects or generic dictionaries.
- Avoid storing secrets in durable artifacts unless the artifact is explicitly protected.
- Redact secrets in exception messages and diagnostics.

## Authentication and authorization

- Authenticate the caller/process/user before accepting sensitive commands across a trust boundary.
- Authorize the specific action, resource, and scope.
- Do not rely on UI hiding or disabled buttons as authorization.
- Distinguish unauthenticated, unauthorized, malformed, unsupported, and failed requests.
- Scope access by account, tenant, workspace, environment, machine, root path, or role where relevant.
- Use secure defaults when configuration is missing or ambiguous.

## Path and filesystem safety

Paths are external input unless created wholly by trusted code.

- Normalize and validate paths before safety-sensitive use.
- Resolve relative paths against an explicit base.
- Reject path traversal outside allowed roots.
- Check unexpected root changes, drive changes, UNC paths, symlinks, junctions, and reparse points where relevant.
- Treat casing and normalization differences intentionally.
- Do not assume a path remains safe after time passes; revalidate before finalization when the filesystem may change.
- Avoid broad recursive delete/copy/move operations unless roots and scope are explicit and tested.
- Do not place user data, logs, or databases under build output unless they are test artifacts.

## IPC and local process boundaries

For named pipes, sockets, file drops, registry signals, service control, or other IPC:

- Define who is allowed to send commands.
- Authenticate/authorize when crossing user, process, service account, or machine boundaries.
- Keep command sets narrow and explicit.
- Do not expose arbitrary command execution.
- Validate all payloads.
- Include correlation ids for traceability.
- Treat file-drop protocols as untrusted input until validated and claimed safely.
- Avoid leaving malformed or unauthorized requests in active processing locations.

## Network boundaries

- Use TLS for sensitive network communication.
- Validate certificates according to platform defaults unless the project has a documented trust model.
- Do not disable certificate validation to make development easier.
- Use explicit timeouts, cancellation, and retry policy.
- Do not retry non-idempotent operations unless replay is safe.
- Validate response structure and status before using data.
- Treat remote data as untrusted.
- Avoid logging full URLs when query strings may contain secrets.

## External processes

- Prefer structured argument passing over shell invocation.
- Do not concatenate command strings with untrusted input.
- Capture exit code, stdout/stderr as appropriate, timeout, cancellation, and diagnostic context.
- Treat output as untrusted input.
- Use allowlisted executable paths or project-approved tools when possible.
- Do not run external processes elevated unless necessary and approved.
- Do not treat process exit code 0 as full workflow success unless the contract proves the required outcome.

## Data privacy

- Collect and persist only what the project needs.
- Treat file paths, usernames, machine names, email addresses, document names, and free-text input as potentially sensitive.
- Redact or summarize sensitive values in shareable reports.
- Keep telemetry free of secrets, file contents, private messages, and unnecessary raw paths.
- Make exports, uploads, sharing, publishing, and external links explicit in UI.
- Honor retention and cleanup policies.

## Cryptography and verification

- Use platform cryptography APIs and well-known algorithms.
- Do not design custom cryptography.
- Use SHA-256 or stronger for safety-relevant content hashing unless compatibility requires otherwise.
- Do not use MD5 or SHA-1 for security decisions.
- A hash proves equality to the hash input, not authenticity.
- Use HMAC, digital signatures, authenticated encryption, or trusted transport when authenticity matters.
- Manage keys as secrets with rotation and storage policy.
- Distinguish integrity, authenticity, confidentiality, and authorization in contracts and UI text.

## Dependency and supply-chain safety

- Do not add production dependencies casually.
- Prefer established packages with clear maintenance, licensing, and security posture.
- Explain why a new dependency is needed and what alternatives were considered.
- Avoid packages for trivial functionality.
- Keep package versions consistent with project policy.
- Run vulnerability checks when the project profile requires them.
- Do not ignore package security warnings without documenting the reason and risk.

## Installers and updates

When the project has installers, updates, or deployment automation:

- Define install location, data location, log location, service account, privileges, and rollback behavior.
- Do not overwrite user data during update.
- Verify downloaded updates or packages when updates cross a trust boundary.
- Make elevation prompts and permission changes explicit.
- Preserve diagnostics for failed install/update/uninstall.
- Do not silently weaken file permissions to make installation easier.

## Review checklist

Before accepting a security/boundary change, ask:

- What trust boundary is crossed?
- Is the caller/user/process authenticated and authorized where needed?
- Are external inputs parsed and validated at the boundary?
- Are paths normalized, scoped, and revalidated before dangerous use?
- Are secrets kept out of source, logs, reports, telemetry, and UI by default?
- Does the change fail closed for unknown safety-affecting values?
- Can the change execute arbitrary commands or access arbitrary paths?
- Are destructive operations scoped and explicit?
- Are logs useful without leaking sensitive data?
- Is cryptographic wording accurate about what is actually proven?
