# 0006. Resolve tenant from route slug

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

Each request must know which club it belongs to before any data is touched. Common options are subdomain (`lakeside.example.com`), path (`/lakeside/...`), or inferring the club from the signed-in user. The free hosting tier ([ADR 0007](0007-low-cost-azure-hosting.md)) offers only `*.azurewebsites.net` with no wildcard custom domains.

## Decision

We will resolve the tenant from a **`{club}` route slug** at the start of the path: `/{club}/teams/{teamId}/...`. A middleware looks up the club by slug, populates the scoped `TenantContext`, and returns 404 for unknown slugs. Authorization then requires a `ClubMembership` for that club. Root-level routes (`/`, account pages) are tenant-free and send signed-in users to their club, or let them pick one if they belong to several.

## Consequences

- Works on free hosting with no DNS setup; URLs are shareable and clearly show the club.
- Every tenant route needs the `{club}` segment, handled by a route convention and link helpers.
- Moving to subdomains later only changes how the slug is read; `TenantContext` and everything downstream stay the same.

## Alternatives considered

- **Subdomain per club:** looks nicer, but needs a custom domain, wildcard DNS and TLS, which the free tier doesn't support.
- **Infer from the signed-in user only:** ambiguous for users in more than one club, and makes public club pages (e.g. the iCal feed) awkward.
