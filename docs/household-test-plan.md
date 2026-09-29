# Household Test Plan — starting set

Scope: `Household` model, `HouseholdRepository`, `HouseholdService`, and
`HouseholdResolutionMiddleware`, as they exist today (before the `POST
/households` / join-by-code endpoints are built). Grouped by layer, roughly in
the order to write them.

## Prerequisites (do these before/alongside the tests below)

- [ ] Apply the `IgnoreQueryFilters()` fix to `HouseholdResolutionMiddleware`'s
      `HouseholdMemberships` lookup — without it, any middleware test that
      supplies real claims will throw `InvalidOperationException`, not fail
      the assertion you're actually testing.
- [ ] `TestAuthHandler` (`test/setup/TestAuthHandler.cs`) only sets `sub` and
      `azp` today. Middleware tests need it to optionally set `useremail`/
      `username` too, or the shadow-record-sync/household-lookup code path
      never gets reached — same trap that hid the bug above.
- [ ] Add a mocking library — `test/test.csproj` has none yet. Add Moq
      (`PackageVersion` in `Directory.Packages.props` + `PackageReference` in
      `test/test.csproj`). Every `HouseholdServiceTests` case in Section 1
      needs it.

**Mocked repository vs. real repository + InMemory DB — pick one per test,
never both:**

| | `Mock<IHouseholdRepository>` | `HouseholdRepository` + `InMemoryAppDbContextFactory` |
|---|---|---|
| What "seeding data" means | `repo.Setup(...).ReturnsAsync(seeded)` — no DB involved at all | `db.Households.Add(seeded); await db.SaveChangesAsync();` — a real row has to exist |
| What it proves | `HouseholdService`'s own logic in isolation | Real EF Core/query-filter behavior |
| Where it's used | Section 1 | Sections 2, 3, and the `HouseholdServiceIntegrationTests` example |

Mixing the two — e.g. mocking the repository *and* expecting EF's
auto-generated `Guid` on save — is the exact mistake from this session's
`GetByIdAsync_Success_Propagates` review; worth keeping this table in mind
while writing Section 1.

## 1. `HouseholdService` — unit tests, mock `IHouseholdRepository`

Fastest to write, no DB needed. Mirrors whatever pattern `UserService` will
eventually get (none exist yet for `UserService` either — worth noting as a
gap, not blocking this plan).

- [ ] `GetByIdAsync` — repository returns a `Household` → returns the mapped
      `HouseholdDto` (and confirm the DTO has no `Id` field, per the
      DTO-shouldn't-carry-Id decision).
- [ ] `GetByIdAsync` — repository throws `NotFoundException` (missing id) →
      propagates, not swallowed.
- [ ] `AddAsync` — calls `CreateAsync` with a correctly-mapped `Household`,
      then calls `SaveDbChangesAsync` (catches a regression like the
      "forgot to call SaveDbChangesAsync" bug from `UserService` earlier
      this session).
- [ ] `UpdateAsync` — fetches the existing entity by id, applies
      `Name`/`Timezone`/`InviteCode` from the DTO, calls
      `SaveDbChangesAsync`. Assert `IsDeleted` is *not* touched by update
      (matches `UserService.UpdateAsync`'s existing behavior).
- [ ] `DeleteAsync` — calls `DeleteAsync` + `SaveDbChangesAsync` exactly
      once each (regression test for the double-lookup bug just fixed —
      assert the mock's `GetAsync` is never called from this method).
- [ ] `ToModel` — a `HouseholdDto` with `IsDeleted: true` round-trips to a
      `Household` with `IsDeleted == true` (this field is easy to drop
      silently since `UserDto`'s mapper doesn't carry an equivalent).

**Failure-path tests — one per write method, proving errors propagate
instead of getting swallowed.** `HouseholdService` has no try/catch of its
own anywhere (unlike `UserService.GetOrCreateAsync`, which deliberately
catches and retries) — these tests are what proves that's intentional, not
an oversight:

- [ ] `AddAsync` — mock `SaveDbChangesAsync` (not `CreateAsync`) to throw
      `DbUpdateException` → propagates unmodified. `CreateAsync` only stages
      the entity in EF's change tracker (`_db.AddAsync`); the real insert —
      and any constraint violation, like a future duplicate `InviteCode` —
      happens at `SaveDbChangesAsync`, so that's the realistic call to fail.
- [ ] `UpdateAsync` — mock `GetAsync` to throw `NotFoundException` for a
      missing id → propagates.
- [ ] `DeleteAsync` — mock `DeleteAsync` to throw `NotFoundException` for a
      missing id → propagates.

**Not yet testable — a design gap, not a test gap:** `AddAsync` does no
business/semantic validation (blank `Name`, malformed `InviteCode`, etc.)
before calling the repository. `domain.Exceptions.ValidationException`
already exists for exactly this, but nothing in `HouseholdService` calls it.
Decide whether the service should validate before saving; only once that
behavior exists is there anything to write a test against.

## 2. `HouseholdRepository` / `GenericRepository<Household>` — integration, InMemory DB

Same InMemory-DB pattern `WhoamiControllerTests` already uses.

- [ ] `CreateAsync` + `SaveDbChangesAsync` persists a row with a
      server-generated `Id` (not `Guid.Empty`).
- [ ] `GetAsync` on a missing id throws `NotFoundException` with `"Household"`
      in the message (confirms `typeof(T).Name` wasn't regressed back to
      something generic like the `nameof(id)` bug from earlier).
- [ ] `GetAllAsync` excludes a household with `IsDeleted == true` — this is
      the actual proof the `!IsDeleted` query filter is wired up and doing
      something, not just present in the code.
- [ ] `GetAsync` on a soft-deleted household's id also throws
      `NotFoundException` (global filters apply to `FindAsync` too, not just
      `ToListAsync` — worth confirming explicitly rather than assuming).

- [ ] **Deferred — needs a real Postgres fixture, not InMemory** (e.g. via
      Testcontainers): `CreateAsync` + `SaveDbChangesAsync` a household with
      `InviteCode = "DUP1"`, then `CreateAsync` a second household with the
      same code → the *second* `SaveDbChangesAsync` call throws
      `DbUpdateException`. EF Core's InMemory provider does not enforce
      unique indexes at all, so this can't be proven with the setup above —
      it would pass for the wrong reason (both inserts silently succeed).
      Until a real-DB fixture exists, treat this as a manual check after the
      migration runs, not an automated test.
- [ ] **Deliberately skipped:** required-field/max-length enforcement
      (`IsRequired`, `HasMaxLength`). `Name` is a non-nullable `string` in an
      NRT-enabled project, so the compiler already prevents passing `null`
      in normal code, and `HasMaxLength` isn't enforced by the InMemory
      provider or meaningfully exercised without a real Postgres column. Not
      worth a dedicated test — noting this so it reads as a decision, not an
      oversight.

## 3. Tenancy filter smoke test — the one the RFC calls a must-have

The RFC's Phase 7 checklist explicitly names this as a required correctness
property, and it's cheap to write now against `HouseholdMembership` rather
than deferring it:

- [ ] Seed two households, each with one membership row. With
      `ICurrentHouseholdContext` faked/stubbed to household A's id, querying
      `HouseholdMemberships` returns only household A's row — household B's
      row is invisible, not just unmatched by a `Where` clause you wrote by
      hand. This is the test that proves the global filter itself works, not
      just that your code remembers to filter.

## 4. `HouseholdResolutionMiddleware` — integration, once the prerequisite fix lands

- [ ] Authenticated request, valid `sub`/`useremail`/`username` claims, user
      already has a `HouseholdMembership` → `context.Items["HouseholdId"]` is
      set correctly, no exception. This is the direct regression test for
      the query-filter deadlock bug — the most important test in this whole
      plan, since nothing today exercises this path at all.
- [ ] Same, but the user has no `HouseholdMembership` yet →
      `context.Items["HouseholdId"]` stays unset, request still proceeds
      (no exception, no early-return skipping `next()`).
- [ ] Authenticated request, brand-new user (no `User` row) →
      `GetOrCreateAsync` creates the row; no household yet; no exception.
- [ ] Authenticated request missing `useremail` or `username` → middleware
      no-ops (still calls `next()`), doesn't attempt `GetOrCreateAsync`.
- [ ] Unauthenticated request → middleware no-ops immediately, no DB calls
      at all (cheap to assert via a spy/mock repository never being hit).

## Suggested order

1. Prerequisites (fix + TestAuthHandler + Moq).
2. Section 4's first bullet — write this one first, since it's the actual
   proof the bug is fixed, before anything else.
3. Section 1, success paths, then the failure-propagation tests (fast, no
   DB).
4. Section 2 + 3 (InMemory DB); leave the deferred duplicate-`InviteCode`
   case for whenever a real-Postgres fixture exists.
5. Remaining Section 4 cases.

## Out of scope for this plan, worth remembering later

`POST /households` (not built yet) will create a `Household` *and* an Owner
`HouseholdMembership` together. That's the point where partial-failure
matters for real — a `Household` saved without its owner membership is a
genuinely broken state, unlike anything `HouseholdService.AddAsync` does in
isolation today. Not a gap in this plan, just flagging it so it isn't a
surprise when that endpoint's test plan gets written.
