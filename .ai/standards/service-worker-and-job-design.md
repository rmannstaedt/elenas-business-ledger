# Service, Worker, and Job Design Standards

Use this file when changes involve Windows services, hosted services, worker processes, scheduled jobs, background queues, durable jobs, progress aggregation, cancellation, retry, resume, startup recovery, shutdown, IPC, staging, verification, finalization, or service-owned state.

This file applies only when the project has services, workers, jobs, or background processing. Do not introduce that complexity unless the project needs it.

## Core principles

- Long-running work needs an owner.
- Durable work needs truthful state.
- Background exceptions must be observed.
- Cancellation, retry, timeout, shutdown, and failure are distinct outcomes.
- Recovery should be proportional to the risk and cost of the work.
- UI, CLI, and API surfaces may request and observe work; they should not secretly own service execution.
- Queue acceptance, process start, copy completion, or worker dispatch is not proof of end-to-end success unless the contract explicitly says so.

## Runtime roles

Name roles generically and consistently in the project profile.

Common roles:

- Control surface: UI, CLI, API, or scheduler that accepts user or automation intent.
- Coordinator: application/service component that validates, authorizes, sequences, and tracks work.
- Worker: component that performs a bounded piece of work.
- Job store: durable state used to recover, resume, audit, or report work.
- Artifact store: logs, reports, manifests, payload metadata, failed-work records, or diagnostic output.
- External participant: remote service, process, endpoint, file share, database, queue, or OS service.

A role should not gain authority accidentally. Be explicit about who may command, mutate, observe, recover, and finalize work.

## Job model

Use a durable job model when work is long-running, expensive, user-visible, destructive, safety-relevant, resumable, scheduled, or needs audit/support.

A job model should define job identity, owner component, input snapshot, approval/authorization state if applicable, phases/stages, item/action state when work is per-item, progress counters, cancellation state, retry state, terminal outcome, error details, artifact/report locations, and recovery behavior after restart.

Common terminal states include `Completed`, `CompletedWithWarnings` or `PartialSuccess`, `Failed`, `Cancelled`, and lifecycle-specific states such as `Abandoned` or `Expired`.

Do not report `Completed` when required verification, finalization, publication, or cleanup did not happen.

## State transitions

- Use typed states, not raw strings.
- Treat unknown safety-affecting state values as invalid and fail closed.
- Persist state before executing work when recovery needs to know that work was attempted.
- Persist final state and failure evidence before discarding working data.
- Completed actions should not repeat unnecessarily after restart.
- Retried actions should preserve attempt history or summarized retry evidence.
- Interrupted individual operations may be redone from the beginning when that is simpler and safe, but the contract must say so.

## Cancellation, timeout, shutdown, retry, and idempotency

- Cancellation is a requested stop, not a generic failure.
- Timeout is a time-bound failure or cancellation depending on the contract.
- Shutdown is host lifecycle and must be handled separately from user cancellation.
- Pass cancellation tokens through long-running call chains.
- Cancellation should leave durable state truthful and recoverable.
- Do not abandon in-flight operations without recording whether their outcome is unknown, cancelled, failed, or safe to retry.
- Define which failures are transient.
- Define retry limits, backoff, and final failure behavior.
- Do not retry non-idempotent work unless replay is safe.
- Use idempotency keys, leases, item state, optimistic concurrency, or deduplication where repeated execution is possible.
- Do not let retry hide persistent failures.
- Record retry exhaustion as a distinct outcome where it matters.

## Scheduling and automation

Scheduled jobs and automation should pass through the same safety gates as interactive jobs unless the project profile explicitly defines a different automation policy.

- Destructive work must remain explicit and scoped.
- Suspicious mass changes or unusual batch sizes should be blocked, escalated, or require policy approval when the consequence is high.
- Missed schedules, overlapping runs, disabled schedules, and scheduler shutdown should be represented truthfully.
- Avoid overlapping jobs unless concurrency is intentionally supported.
- Automation should not silently bypass user review requirements that exist for safety.

## Progress and observability

Progress should be useful and truthful.

- Define progress units: items, bytes, records, phases, or steps.
- Distinguish discovered total, estimated total, and known total.
- Do not show precise percentages when the denominator is unstable.
- Preserve progress through restarts when durable progress matters.
- Aggregate progress in the component that owns coordination.
- Logs, UI state, and durable job state should not contradict each other.
- High-volume per-item progress should be bounded, summarized, or stored in a suitable artifact.

## Queues, claims, leases, staging, and finalization

When work is claimed from a queue, file drop, database table, or IPC mechanism:

- claiming must be atomic enough to prevent duplicate workers from processing the same work unsafely;
- leases should have expiration/recovery behavior;
- poison items should be moved, marked, or recorded after retry exhaustion;
- malformed or unauthorized work should fail closed;
- working items should not remain forever in limbo after process death;
- startup recovery should reconcile incomplete claims before accepting new work when needed.

When jobs stage payloads, files, reports, database updates, or publishable results:

- staging location should be explicit and appropriate to the final destination;
- readers must not mistake staged/incomplete data for final data;
- finalization must happen only after required verification succeeds;
- finalization should be atomic or compensating where practical;
- failed staged data should be preserved or cleaned according to project policy;
- cleanup must not remove evidence needed for diagnosis or recovery.

## Windows services and IPC

For Windows services and hosted workers:

- keep service host code focused on composition and host lifecycle;
- keep workflow policy in Application or an explicit worker/coordinator component;
- use dependency injection lifetimes that match worker state;
- avoid long-running mutable state in singletons unless it is explicitly synchronized and owned;
- observe and log background exceptions;
- support graceful shutdown;
- define service account, permissions, working directories, log locations, and data locations;
- avoid requiring administrative privileges unless the project truly needs them;
- document install/update/uninstall behavior when the service is deployed.

For named pipes, sockets, file drops, HTTP endpoints, or other command channels:

- authenticate and authorize command sources where the channel crosses a trust boundary;
- validate all requests at the boundary;
- do not expose arbitrary command execution;
- make allowed commands explicit and narrow;
- include correlation ids for request/result matching;
- report unauthorized, malformed, unsupported, and failed requests distinctly;
- avoid treating request receipt as work success.

## Testing and review checklist

Test or inspect normal completion, failure before work starts, per-item failure, partial success, cancellation, timeout, retry and retry exhaustion, restart during active work, duplicate claim prevention, malformed work item handling, unauthorized request handling, progress aggregation, finalization blocked when verification fails, and cleanup preserving necessary evidence.

Before accepting a service/worker/job change, ask:

- Who owns the job and durable state?
- Can the job recover after process or machine restart where required?
- Can work be duplicated unsafely?
- Can incomplete staged work be processed or published?
- Is finalization blocked when verification fails?
- Are cancellation and interruption states explicit?
- Are retries bounded and safe?
- Are malformed or unauthorized requests rejected safely?
- Are completed, failed, cancelled, and partial outcomes durable and inspectable?
- Are logs and progress truthful?
