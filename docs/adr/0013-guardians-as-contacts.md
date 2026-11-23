# 0013. Guardians as contacts, no parent logins in Phase 1

- **Status:** Accepted
- **Date:** 2026-11-23

## Context

Most players are minors. Parents need team announcements and the schedule, but nearly all of that can reach them by email. Every account is one more thing to secure and support. Every field about a child is data the club is responsible for.

## Decision

- **Players never get logins.**
- **Parents don't log in in Phase 1.** A `Guardian` is a contact on a player record: name, email and relationship. Guardians receive announcement emails, with a one-click unsubscribe. Parent logins are reconsidered in Phase 3, when RSVPs give parents something to do in the app.
- **Minimal data about minors.** For a player, the app stores only name, grad year, jersey number and positions. For a guardian, it stores name, email and relationship. No birthdates, addresses, phone numbers or medical information.
- Only coaches, team managers and club admins sign in.

## Consequences

- Fewer accounts, a smaller attack surface, and less personal data to protect.
- Invitations in Phase 1 are for staff and admins only.
- When parent logins arrive, a guardian contact can be linked to an account by email without changing the player data.

## Alternatives considered

- **Parent accounts now:** parents could view the roster and schedule in the app, but the gain over email is small until RSVPs, and it adds invitations, support and more personal data.
