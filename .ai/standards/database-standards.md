# Database Standards

Use this file only when the project includes a database component or when a change touches SQL, SQLite, SQL Server, EF Core, migrations, data access, persistence models, transactions, query performance, seed data, or database-backed tests.

Database rules are optional for projects without a database. Do not introduce a database merely because this file exists.

## General principles

- Treat the database as durable state with a compatibility contract.
- Keep business policy out of persistence plumbing.
- Make transaction boundaries explicit.
- Make migrations intentional, reviewable, and reversible or recoverable when practical.
- Prefer correctness and data integrity over clever query abstraction.
- Do not hide partial failure as success.
- Use stable identities and explicit relationships for durable business state; do not rely on mutable descriptive fields for financial, audit, or cross-record references.
- Do not let persistence models leak into UI or domain code unless the project profile explicitly chooses an active-record style.

## Ownership

Default Clean/Layered ownership:

- Domain/Core owns business concepts and invariants.
- Application owns use-case workflow and transaction policy at the level of business intent.
- Infrastructure owns concrete database access, EF Core DbContexts, SQL, migrations, connection creation, and low-level persistence details.
- Hosts compose database services and run migrations only according to project policy.

Do not put EF Core, SQL, connection strings, DbContexts, database attributes, or migration logic into Domain/Core unless the project architecture explicitly permits it.

## EF Core guidance

When using EF Core:

- Keep `DbContext` lifetime scoped to a unit of work, request, command, or operation as appropriate.
- Avoid long-lived DbContexts in desktop apps and background services.
- Do not share a DbContext across threads.
- Prefer explicit loading, projections, or includes over accidental lazy loading. Disable lazy loading by default unless the project profile intentionally enables it.
- Do not return `IQueryable<T>` across application boundaries unless the project has an explicit query abstraction policy.
- Prefer projections for read models when only a subset of columns is needed.
- Avoid tracking for read-only queries when tracking is unnecessary.
- Keep migrations in the owning infrastructure project.
- Review generated migrations; do not blindly accept destructive operations.
- Configure required fields, max lengths, indexes, uniqueness, cascade behavior, precision, and conversions intentionally.
- Do not rely on database defaults for business meaning unless the domain/application contract says so.
- Convert EF entities to application/domain models before safety-sensitive behavior when entities are persistence-shaped.

## Identity, lineage, and durable references

Database schemas are part of the domain-data integrity contract. When rows represent durable business data, review whether the schema can prove which exact entity each row refers to.

Use explicit stable references for:

- invoice lines, payments, credits, receipts, tax/VAT calculations, accounting exports, and issued documents;
- local rows mapped to external-system rows;
- imported/exported/reconciled records;
- completed, locked, finalized, printed, paid, or deleted state;
- historical plan/catalog/customer/student/account relationships.

Database-backed durable references should normally be represented by primary keys, foreign keys, unique alternate keys, or explicit immutable IDs. A descriptive compound match such as name plus date/time plus amount is not enough unless it is a documented, validated, constrained, immutable natural key.

When historical output must remain stable, store both:

- the stable reference needed for ownership, joins, locking, corrections, and audit lineage;
- the snapshot fields needed to display what was issued or agreed to at the time.

Review database code for:

- status queries that join by exact source identity rather than date/name/text similarity;
- required reference columns that are nullable or default to `0` in finalized records;
- missing indexes for identity-based lookup paths;
- missing uniqueness constraints where the domain depends on uniqueness;
- migrations that add identity columns but leave existing durable rows ambiguous;
- cleanup/reset operations that remove parent records while leaving child rows with misleading stale references;
- EF projections or DTO mappings that drop IDs needed by downstream workflows.

If a database change allows financial, billing, tax, payment, audit, or finalized state to attach to the wrong row, treat it as a Critical review finding.

## SQL guidance

- Use parameterized queries. Do not concatenate untrusted input into SQL.
- Keep SQL readable and close to the repository/service that owns it.
- Treat SQL text as source code: review it, test it, and keep it versioned.
- Avoid hidden cross-cutting filters or triggers unless they are documented and tested.
- Make sorting deterministic when result order matters.
- Avoid `SELECT *` in production queries where schema evolution, performance, or compatibility matters.
- Use appropriate indexes for common filters, joins, uniqueness, and ordering.
- Be explicit about collation and case sensitivity where it affects behavior.

## SQLite guidance

When using SQLite:

- Decide whether the database is user data, cache, test fixture, or internal durable state.
- Store the database under an appropriate application/user data location, not next to build output unless it is a test artifact.
- Be explicit about journal mode, busy timeout, connection pooling, and concurrency expectations when concurrent access is plausible.
- Treat file-level backup, copy, and deletion as database operations with durability implications.
- Do not edit production SQLite files manually as part of normal application behavior.

## Migrations and schema evolution

- Migrations are durable format changes and should be reviewed like code.
- Destructive migrations require explicit approval or documented compatibility policy.
- Do not drop columns, tables, indexes, or constraints casually.
- Preserve data or provide a documented migration path when real user data may exist.
- Keep migration order deterministic.
- Do not run automatic migrations at startup unless the project profile explicitly allows it.
- For desktop apps, decide whether migration happens automatically, on first launch, during install/update, or under user/admin control.
- Record schema version where the technology does not already provide it.
- Test migrations from the oldest supported version when real deployments exist.
- For identity/lineage changes, test duplicate descriptive values and stale/missing reference cases, not only happy-path round trips.

## Transactions and consistency

- Define transaction boundaries around business units of work, not around arbitrary repository calls.
- Use transactions when multiple writes must succeed or fail together.
- Do not hold transactions open across user interaction, slow network calls, long file I/O, or unbounded work.
- Avoid mixing database state and filesystem state without an explicit compensation, staging, or reconciliation strategy.
- Distinguish committed, pending, failed, cancelled, and unknown states where the workflow requires it.
- Retries must be safe. Do not retry non-idempotent writes unless the operation has idempotency keys, optimistic concurrency checks, or another replay-safety mechanism.

## Concurrency and performance

- Use optimistic concurrency tokens where lost updates matter.
- Handle concurrency conflicts as first-class outcomes, not generic exceptions.
- Do not silently overwrite another user's or process's changes.
- Make lock/wait behavior explicit for batch jobs and background services.
- Avoid unbounded parallel database access.
- Prefer clear queries first, then optimize based on realistic data and measurement.
- Avoid N+1 query patterns in production paths.
- Use pagination or streaming for large result sets.
- Avoid loading unbounded result sets into memory.
- Use indexes intentionally and verify that they support the actual query shape.
- Add performance tests or query plans when performance risk is meaningful.

## Security and privacy

- Connection strings and credentials are secrets. Do not commit them.
- Store secrets using the project-approved secret store.
- Enforce authorization in application/service layers; do not rely on UI filtering.
- Apply row/account/tenant scoping at the query/application boundary where relevant.
- Do not log raw SQL parameters when they may contain secrets or personal data.
- Encrypt at rest only when the project risk model requires it and the technology supports it correctly.

## Test guidance

- Prefer repository/service tests for data access behavior that cannot be verified with pure unit tests.
- Use realistic database engines for behavior that differs by provider.
- SQLite in-memory tests are not a substitute for SQL Server behavior when provider differences matter.
- Use isolated test databases or unique database names/paths per test run.
- Clean test data safely; never point tests at production or developer user data.
- Test migrations, constraints, transaction rollback, concurrency conflicts, and malformed persisted values where relevant.

## Review checklist

Before accepting a database change, ask:

- Is the database concern active in the project profile?
- Is persistence logic in the owning layer?
- Are durable business relationships represented by stable identities or enforced natural keys?
- Are references and snapshots both stored where history and exact lineage both matter?
- Does the migration preserve real data or explicitly document a breaking/destructive change?
- Are transaction boundaries explicit and appropriate?
- Are queries parameterized and scoped?
- Are large result sets bounded or streamed?
- Are concurrency conflicts handled truthfully?
- Can state such as invoiced, paid, completed, printed, issued, reconciled, or deleted attach to the wrong row through descriptive matching?
- Are database errors reported without exposing secrets?
- Were relevant migration/data-access tests run or clearly reported as not run?
