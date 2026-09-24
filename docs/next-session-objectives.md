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

## Session 1 — actually close out Phase 0 ✅ done (2026-09-24)

- [x] Fix `HouseholdResolutionMiddleware`'s DI crash — moved `IUserRepository`/
      `IUserService` off the constructor onto `InvokeAsync` method injection.
      Verified: `dotnet test` 9/9 passing, host starts cleanly.
- [x] Fix CI's `tsc`/`LayoutProps` failure — `npx next typegen` added before
      `npx tsc --noEmit` in `.github/workflows/ci.yml`.
- [x] Build the shadow-record sync: `IUserService.GetOrCreateAsync(idpId,
      displayName, email)` wired into `HouseholdResolutionMiddleware`, reading
      claims from `context.User.FindFirst(...)`. Reviewed twice; both bugs found
      along the way (dead `context.Items` read, exception-wrapping regression)
      are fixed.
- [x] Unique index + migration on `User.IdentityProviderId`
      (`20260924173208_AddUnqieIndexOnUserIdpID`).
- [x] Tenancy query filter re-enabled on `HouseholdMembership` in
      `AppDbContext.OnModelCreating`, reading `ICurrentHouseholdContext` via
      constructor injection. Interface relocated to `infrastructure.Interfaces`
      to fix the reference-direction conflict; duplicate-class build break from
      that move is fixed. `dotnet build` clean, 9/9 tests passing.

Two trivial cosmetic leftovers, not blocking: a stale comment in
`HouseholdMembershipTypeConfiguration.cs`, and an unnecessary `using` alias in
`Program.cs` left over from resolving the duplicate-class conflict.

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
