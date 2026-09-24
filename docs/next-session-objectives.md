# Next Session Objectives (as of 2026-09-24)

Derived from `Docs/roadmap.md` + `backlog.md` in the vault, cross-checked against
the actual repo state (git log, `dotnet build`, `dotnet test`, direct file reads)
rather than the backlog checkboxes alone.

## Reality check: Phase 0 is not actually finished

The backlog marks `ICurrentHouseHold Setup` done (2026-09-17), but two things the
roadmap requires under that umbrella are not actually live:

| Backlog says | Roadmap actually requires | Reality |
|---|---|---|
| ✅ ICurrentHouseHold Setup | "the EF Core global query filter mechanism... so every entity added afterward automatically inherits tenancy" | Filter is **commented out** in `src/infrastructure/EntityConfig/HouseholdMembershipTypeConfiguration.cs` — nothing is tenancy-scoped right now. |
| (no separate line item) | "the local `User` shadow-record sync on first login" | Not implemented — no `GetOrCreate` anywhere in the codebase. |
| — | — | **Newly found:** `HouseholdResolutionMiddleware` crashes the host on startup (scoped service resolved from root provider) — 4/9 tests currently fail. |

Building Phase 1 (Household, HouseholdMembership CRUD) before these are real means
retrofitting tenancy onto entities that already exist — more expensive later than
now, per the roadmap's own guiding principle.

## Session 1 — actually close out Phase 0

- [ ] Fix `HouseholdResolutionMiddleware`'s DI crash — move `IUserRepository` from
      constructor injection to `InvokeAsync` method injection (scoped service can't
      be constructor-injected into conventional middleware, which is built once
      against the root provider).
- [ ] Fix CI's `tsc`/`LayoutProps` failure — add `npx next typegen` before
      `npx tsc --noEmit` in `.github/workflows/ci.yml` (Next generates the ambient
      `LayoutProps`/`PageProps` types into `.next/types`, which doesn't exist yet on
      a clean CI checkout).
- [ ] Build the shadow-record sync: `IUserService.GetOrCreateAsync(idpId, email,
      displayName)` → `IUserRepository`, called from `HouseholdResolutionMiddleware`
      (or a dedicated earlier middleware) instead of the current look-up-and-no-op.
      This also resolves the `Guid.Empty`-as-household-id bug as a side effect,
      since "brand-new user, no household yet" becomes a real handled branch.
- [ ] Add a unique index + migration on `User.IdentityProviderId` (needed for the
      get-or-create to be race-safe under concurrent first-requests).
- [ ] Re-enable the tenancy query filter properly: inject `ICurrentHouseholdContext`
      into `AppDbContext`'s own constructor and reference that as a field in
      `HasQueryFilter`, rather than injecting it into the `IEntityTypeConfiguration`
      class (that's what caused the circular-dependency bug that got commented out).

## Session 2 — start Phase 1 (household lifecycle)

Scoped to the roadmap's own checkpoint: "two users can create a household and both
be members of it." Per `Docs/data-contract.json`'s `Household`/`HouseholdMembership`
shape:

- [ ] `Household` entity + EF migration (`name`, `timezone`, `inviteCode`,
      `createdAt`, `isDeleted`).
- [ ] `POST /households` — create household + `HouseholdMembership` (Owner) in one
      transaction.
- [ ] Invite code generation on creation.
- [ ] `POST /households/join` — join-by-code → `HouseholdMembership` (Member).
- [ ] Member removal (owner-only, task auto-unassignment) — stretch goal if Session
      2 runs long; not required for the roadmap's stated checkpoint.

## Deferred (not blocking either session above)

- EF Core package version conflict in `Directory.Packages.props`
  (`Microsoft.EntityFrameworkCore.Relational` 10.0.4 vs 10.0.11, `MSB3277`
  warnings) — a warning, not a blocker.
