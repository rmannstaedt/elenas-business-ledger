# Persistence and Artifact Standards

Use this file for changes involving JSON, JSON Lines, logs, reports, import/export files, generated artifacts, durable state, caches, manifests, cleanup, retention, migration, or format changes.

These standards apply broadly because most C#/.NET projects create configuration, logs, diagnostic output, reports, or durable state. Database-specific rules live in `database-standards.md` and apply only when the project has a database component.

## General principles

- Prefer boring, inspectable formats.
- Prefer durable formats that can be diagnosed with ordinary tools.
- Prefer append-friendly output for long-running operations.
- Make artifacts useful for audit, troubleshooting, support, recovery, and repeatability.
- Be conservative about format changes once real users, jobs, data, or integrations depend on them.
- Durable artifacts should reflect what actually happened, not what the code hoped would happen.
- Do not overwrite or discard diagnostic information needed to understand failures.
- Cleanup must be conservative, scoped, and reportable.

## Recommended formats

Use the project profile as the source of truth. In the absence of a project-specific choice:

- Configuration: JSON, environment variables, user secrets, or platform-appropriate settings.
- Durable machine-readable state: JSON.
- Appendable per-event or per-item records: JSON Lines.
- Logs: structured logging sink chosen by project profile; JSON Lines when file logs are intended for tooling.
- Human-readable reports: Markdown or HTML.
- Human-readable plus machine-readable reports: Markdown/HTML plus companion JSON.
- Small deterministic fixtures: JSON.
- Large or streaming manifests: JSON Lines.
- Temporary files: explicit reserved extension/name owned by the application, not generic `.tmp` semantics alone.

Avoid binary formats unless required for compatibility, performance, or size and the trade-off is documented.

## Artifact ownership

Every durable artifact should have a clear owner and lifecycle.

Define who creates it, who reads it, who may modify it, whether it is durable state/diagnostic output/cache/report/export/temporary work, whether it may be safely deleted, whether it is part of recovery or audit, whether it contains sensitive information, and whether it has a schema/version contract.

Do not treat recovery, audit, migration, failed-work, or diagnostic artifacts as disposable build output.

## Traceability

Artifacts should preserve enough information to connect relevant pieces of work: operation id, request id, correlation id, user/account/tenant id when applicable, job id, item/action id, source/destination identifiers, relative path or resource key, planned action, execution state, attempt number, verification result, failure reason, and final report location.

Do not duplicate sensitive identifiers unnecessarily. Treat full file paths, usernames, machine names, and personal data as potentially sensitive.

## JSON and JSON Lines contracts

Serialization settings are part of the format contract. Review changes to property naming, enum representation, case sensitivity, null handling, required fields, number handling, duplicate properties, date/time representation, path representation, version fields, and unknown field handling.

For durable or external JSON:

- include a schema or format version when migration is plausible;
- parse strictly where ambiguity can affect safety;
- fail closed for unknown safety-affecting enum values;
- preserve unknown fields only when the format explicitly supports forward-compatible round-tripping;
- distinguish missing, null, empty, unknown, and default values when the difference matters;
- reject duplicate properties when duplicates could create ambiguity;
- use invariant culture for machine-readable formatting;
- use UTC or `DateTimeOffset` for durable timestamps.

JSON Lines should be used for appendable records where each line is independently parseable. Do not write multi-line JSON objects into JSONL files.

## Atomicity and durability

When writing durable files:

- write to a temporary/staging file in the target directory or same volume when atomic replacement matters;
- flush/close before publishing readiness when durability matters;
- use atomic replace/move APIs where available and appropriate;
- ensure readers cannot mistake incomplete files for complete files;
- do not rely solely on generic `.part` suffixes when real files may use similar names;
- record enough state to recover or diagnose interruption.

Ready markers, completion files, renamed files, or database flags must not make incomplete work appear complete.

## Logs and reports

Logs are diagnostic evidence, not a replacement for truthful result models.

- Use structured logs for important identifiers and dimensions.
- Include operation, phase, attempt, duration, outcome, and relevant resource identifiers.
- Do not log secrets, credentials, access tokens, private keys, recovery codes, or file contents.
- Treat file paths and file names as potentially sensitive.
- Bound high-volume logs through sampling, summaries, separate artifacts, or log levels.
- Logs should not contradict reports or durable state.

Reports should answer what happened, what did not happen, and what needs attention. A useful report often includes operation identity, start/end timestamps, version/build information, input summary, action/result counts, skipped/cancelled/failed items, warnings, verification results, artifact locations, and next steps.

Partial success must be reported as partial success. Do not hide failed, skipped, unstable, unverified, or cancelled work inside a success summary.

## Format evolution

If a durable format changes:

- consider a schema/version field;
- document the change;
- update readers before writers when rolling deployment matters;
- consider migration only when real durable history must be preserved;
- preserve backward compatibility unless the breaking change is explicit and accepted;
- do not silently discard artifacts that the current product still needs for recovery, audit, or support.

Format changes should include tests for old, current, malformed, unknown, and future-looking inputs where relevant.

## Import, export, cleanup, and retention

Imported files are external input and must be validated for size, structure, version, encoding, required fields, enum values, and path/resource references.

Do not partially apply imports unless the contract clearly supports partial application and reports it truthfully.

Export deterministic output where practical so users can diff and test it.

Cleanup must be conservative:

- define retention policy before deleting durable artifacts;
- never delete logs, reports, recovery state, failed-work evidence, migrations, database files, or user exports as generic cleanup unless the project profile explicitly allows it;
- prefer moving questionable artifacts to a failed/quarantine/archive location over deleting them;
- cleanup should be scoped, logged, and reportable;
- do not clean outside configured roots or allowlists.

## Testing checklist

For persistence/artifact changes, test or inspect round-trip serialization, missing/null/duplicate/unknown/malformed/future values, version compatibility, interrupted writes, readers seeing incomplete files, permission-denied and locked-file behavior where relevant, cleanup scoping, privacy/redaction, and report/log consistency with durable state.
