# Tooling and Dependency Standards

Use this file when changes involve target frameworks, SDK selection, `global.json`, `.editorconfig`, analyzers, formatting, NuGet packages, build tooling, CI configuration, generated code inputs, source generators, or dependency policy.

The goal is consistency and useful signal, not tooling ceremony.

## Target Framework and SDK Policy

- Use the target framework and SDK declared by `.ai/project-profile.md`, `global.json`, project files, or repository documentation.
- For new projects without a declared choice, prefer the current approved .NET LTS or STS for the organization/project.
- Use Windows-specific TFMs such as `netX.0-windows` only for projects that require Windows APIs, WPF, WinForms, Windows services, COM, registry, named pipes with Windows-specific behavior, or other Windows-only features.
- Keep reusable libraries on non-Windows TFMs when practical.
- Do not retarget or downgrade merely to make a local build pass. If the required SDK is unavailable, report that clearly.

## EditorConfig

- Use `.editorconfig` for formatting and low-noise code-style consistency.
- Prefer rules that improve readability, correctness, or maintainability.
- Do not turn subjective style preferences into blocking issues unless the repository has explicitly chosen them.
- Keep nullable enabled for new C# projects unless the project profile explains why not.

## Analyzers and Quality Gates

- Treat compiler warnings, nullable warnings, and security warnings as meaningful unless proven noisy for the project.
- Prefer focused analyzers that catch real defects over broad noisy rule packs.
- Do not add tools such as StyleCop, SonarQube, Roslynator, or heavy analyzer bundles without a clear project reason and owner approval.
- Suppress warnings narrowly with a reason. Do not use broad suppressions to hide real issues.

## NuGet Dependencies

- Do not add, remove, or upgrade production dependencies without explaining why they are needed and what alternatives were considered.
- Prefer .NET built-in APIs and existing project dependencies when they are adequate.
- Check package license, maintenance posture, transitive dependencies, security advisories, and runtime footprint for production dependencies.
- Keep test-only dependencies out of production projects.
- Avoid dependencies that force a project into a UI, database, hosting, logging, DI, or Windows-specific model unnecessarily.

## Build and CI

- Prefer the narrowest useful build/test/format command for the change.
- CI should catch real regressions without creating excessive local friction.
- Do not make formatting/analyzer gates stricter than the team can realistically maintain.
- Generated code should have a clear source and regeneration command.
- Do not edit generated output directly unless the project explicitly treats it as checked-in source.

## Source Generators and Build-Time Tools

- Add source generators only when they reduce real boilerplate or enforce useful contracts.
- Generated behavior should be understandable from source, tests, or documentation.
- Build-time tools must not require hidden machine-specific state.
- Failures should be diagnosable from command output.

## Review Questions

- Does the tooling choice improve correctness, readability, maintainability, or useful verification?
- Is the signal high enough to justify the friction?
- Can developers run the relevant checks locally?
- Does the dependency or tool create platform, license, security, performance, or maintenance risk?
- Is the decision recorded in `.ai/project-profile.md` or project documentation if it affects future work?
