# 0001. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

This is a solo project that will grow over several phases (MVP, practice planning, MCP). Decisions made early, such as tenancy, hosting and framework, shape everything after them. Without a record, the reasons behind them get lost, and future-me or a reviewer can't tell a deliberate trade-off from an accident.

## Decision

We will record significant architecture decisions as short Markdown ADRs in `docs/adr/`, numbered sequentially, using [the template](0000-template.md) (Nygard style). An ADR is never edited after it is accepted. When a decision changes, a new ADR supersedes the old one.

## Consequences

- Decisions and their trade-offs are visible next to the code and reviewed in pull requests.
- There is a small overhead for each significant decision.

## Alternatives considered

- **Wiki / external docs:** these drift out of sync with the code and aren't visible in the repo.
- **No records:** the reasoning gets lost; the project is also meant to show planning skills.
