# Fulfillment & Inventory Management Platform — Milestone 1

Product & Inventory Foundation: products, categories, warehouses, per-warehouse
stock, and a fully traceable stock adjustment history — built with ASP.NET Core
8, EF Core, and Clean Architecture (Domain / Application / Infrastructure /
Presentation).

## Architecture

```
src/
  Domain/          Entities, enums — no dependencies on other layers
  Application/     DTOs, service interfaces + implementations, business rules,
                    custom exceptions (NotFound/Validation/Conflict/Concurrency)
  Infrastructure/   EF Core DbContext, entity configurations, repositories,
                    UnitOfWork (transactions), JWT auth, password hashing
  Presentation/     Controllers, global exception middleware, Program.cs
```

Dependency direction: `Presentation → Infrastructure → Application → Domain`.
`Application` only depends on `Domain` and knows nothing about EF Core or
ASP.NET Core.

## Data model .

- **Category** — self-referencing (`ParentCategoryId`) for nested categories.
- **Product** — belongs to one Category. Name only, no SKU (see Assumptions).
- **Warehouse** — a physical location.
- **WarehouseStock** — the link between Product and Warehouse: composite key
  `(ProductId, WarehouseId)`, holds the current `Quantity` and a `RowVersion`
  (optimistic concurrency token). A product can exist in many warehouses,
  each with an independent quantity.
- **StockAdjustment** — an append-only audit row for every quantity change:
  `Delta`, `ResultingQuantity`, `Reason` (enum), `Notes`, `PerformedByUserId`,
  `TimestampUtc`. `WarehouseStock.Quantity` is **never** written to directly
  outside `StockService` — every change, including the very first time a
  product is added to a warehouse, produces a matching `StockAdjustment` row,
  so there is no gap in the audit trail.
- **User / Role** — custom tables (no ASP.NET Identity). Roles are fixed:
  `Administrator`, `WarehouseOperator`, `Manager`.

## How stock integrity is enforced

- `StockService.AdjustStockAsync` validates the resulting quantity is ≥ 0
  before saving.
- The quantity update and the `StockAdjustment` insert happen inside a single
  DB transaction (`IUnitOfWork.ExecuteInTransactionAsync`) — both succeed or
  neither does.
- `WarehouseStock.RowVersion` is an EF Core concurrency token. If two requests
  adjust the same stock row at once, the second one gets a `409 Conflict`
  (`ConcurrencyConflictException`) instead of silently overwriting the first.

## Access control

JWT bearer authentication, roles embedded as claims at login (no ASP.NET
Identity — custom `User`/`Role` tables + BCrypt password hashing).

| Role              | Access                                               |
| ----------------- | ---------------------------------------------------- |
| Administrator     | Full CRUD on products, categories, warehouses, users |
| WarehouseOperator | Read products/warehouses; assign stock; adjust stock |
| Manager           | Read-only everywhere                                 |

Enforced via `[Authorize(Roles = "...")]` on controllers/actions — never just
hidden in the UI.

## Test users (seeded automatically on first run)

| Username   | Password         | Role              |
| ---------- | ---------------- | ----------------- |
| `admin`    | `Admin@12345`    | Administrator     |
| `operator` | `Operator@12345` | WarehouseOperator |
| `manager`  | `Manager@12345`  | Manager           |

Login via `POST /api/auth/login` with `{ "username": "...", "password": "..." }`
to get a JWT, then send it as `Authorization: Bearer <token>` on subsequent
requests. A sample request is in `src/Presentation/Presentation.http`.

## Assumptions made

- Products are identified by name only .
- Soft delete (`IsActive` flag) for Product/Category/Warehouse, and it does
  **not** cascade — deactivating a Category or Warehouse does not
  auto-deactivate its Products/WarehouseStocks; the Admin can deactivate
  children separately if needed.
- Stock adjustment reasons use a fixed enum (`InitialStock`, `Restock`,
  `Sale`, `Damage`, `Correction`, `Return`, `Other`) rather than free text.
- The first time a product is assigned to a warehouse, that starting quantity
  is recorded as an `InitialStock` adjustment (from 0), not a silent initial
  value — so 100% of stock history, including the very first unit, is
  traceable.
- Full Administrator user-management endpoints are included (create/update/
  deactivate users, assign roles), beyond just seeded test users.
- Categories support nested parent/child hierarchy (not explicitly required,
  but implied by "categories" as a real-world concept).
- No automated test project for this milestone, per explicit direction.

## Known limitations / not in scope for Milestone 1

- No order processing.
- No pagination on list endpoints .
- No refresh-token flow — JWTs simply expire (`Jwt:ExpiryMinutes`, default
  120 minutes) and the user logs in again.

---

# Milestone 2 — Order Processing & Operational Readiness

Everything below is additive to Milestone 1. Nothing in the M1 feature set
(products, categories, warehouses, stock, users/auth) changed behaviour,
except for two small, unavoidable touches called out explicitly in
**Assumptions** below.

## What was added

```
src/Domain/Entities/        Customer, Order, OrderItem, OrderHistory, IdempotencyRecord
src/Domain/Enums/           OrderStatus
src/Application/Services/
  Orders/                   OrderService.cs (create/browse/items), OrderProcessingService.cs (lifecycle)
  Customers/                CustomerService.cs
  Idempotency/               IdempotencyService.cs
src/Infrastructure/Persistence/Repositories/
                             OrderRepository, OrderHistoryRepository, CustomerRepository, IdempotencyRepository
src/Presentation/Controllers/
                             OrdersController, CustomersController

```

Per the requested layout, each new service lives in its own subfolder under
`Application/Services/` (`Orders/`, `Customers/`, `Idempotency/`) — the
Milestone 1 services (`ProductService`, `StockService`, ...) were left exactly
where they were.

## Order lifecycle

```
Pending ──process──> Processing ──complete──> Completed   (terminal)
   │                      │
 cancel                cancel
   │                      │
   ▼                      ▼
Cancelled (terminal)   Cancelled (terminal)
```

- **Pending → Processing** (`OrderProcessingService.ProcessAsync`): checks and
  deducts stock for every line, all-or-nothing, inside one DB transaction.
- **Processing → Completed**: status-only, no stock impact.
- **Cancelled**: reachable from `Pending` (no stock impact — nothing was
  deducted yet) or from `Processing` (stock is restored). `Completed` and
  `Cancelled` are both terminal — any other transition throws
  `ConflictException` → `409`, and because it's thrown *before* anything is
  written, business data is left completely unchanged.

Every transition (including order creation) writes one immutable
`OrderHistory` row — `FromStatus`, `ToStatus`, who did it, when, and a note.
`GET /api/orders/{id}/history` returns them, most recent first.

## How the M2 business/DB constraints are enforced

| Requirement | Mechanism |
|---|---|
| Order values must never silently change | `OrderItem.ProductNameSnapshot` / `UnitPriceSnapshot` are copied from `Product` at creation time and never re-read from it afterwards. |
| Invalid quantities/products can't create an order | `ValidationException` (400) before anything is persisted — quantity ≤ 0, inactive product/warehouse, unknown IDs. Backed by a DB `CHECK` constraint (`CK_OrderItems_Quantity_Positive`) as a safety net. |
| Not every lifecycle transition is valid | Explicit state checks in `OrderProcessingService`; invalid ones throw `ConflictException` (409) with no data changes. |
| Stock must never go negative | Checked in code (`remaining < 0` before any write) **and** enforced at the DB with `CK_WarehouseStocks_Quantity_NonNegative`. |
| Two orders can't consume the same last unit | `WarehouseStock.RowVersion` (existing M1 optimistic-concurrency token) — a losing concurrent write throws `ConcurrencyConflictException`, which `ProcessAsync`/`CancelAsync` catch and retry (up to 3 attempts) by re-reading the current quantity; if stock is genuinely gone by the retry, the caller gets a normal `ConflictException("Insufficient stock...")`, not a 500. |
| A failed operation must fail safely | Stock deduction/restoration, the status change, and the history row are all written inside one `IUnitOfWork.ExecuteInTransactionAsync` block — if anything throws partway through, the whole transaction rolls back (existing M1 UnitOfWork behaviour, reused as-is). |
| Cancellation must not restore stock twice | `Order.StockDeducted` is flipped to `false` in the same transaction that restores stock, and cancelling an already-`Cancelled`/`Completed` order is rejected outright by the state check above — so even a duplicate cancel call can never double-restore. |
| Repeated critical requests are safe | Optional `Idempotency-Key` header on `POST /api/orders`, `POST /api/orders/{id}/process`, and `POST /api/orders/{id}/cancel`. The response is cached (`IdempotencyRecord`) in the *same* transaction as the business change, keyed on `(Key, Endpoint)` with a unique DB index as the real safety net; a repeated request with the same key replays the original response instead of re-running the action. |
| Growing lists need search/filter/sort/pagination | `GET /api/orders?page=&pageSize=&status=&customerId=&search=&sortBy=&sortDescending=` — filtering/sorting/paging all happen in the DB query (`Skip`/`Take`), and the list projection deliberately does **not** load order items, so browsing never pulls the whole table into memory. |
| Errors are understandable, internals aren't leaked | Reused the existing M1 `ExceptionHandlingMiddleware` as-is — every new exception type used here (`NotFoundException`, `ValidationException`, `ConflictException`, `ConcurrencyConflictException`) was already mapped to a clean `{status, title, traceId}` JSON body; no middleware changes were needed. |
| Failure visibility | `OrderProcessingService` logs (`ILogger`) every processed/completed/cancelled order, every concurrency retry, and every exhausted-retries failure, all tagged with the order ID for correlation with the middleware's `traceId`. |

## Roles

No new roles were introduced (Milestone 1 defines a fixed set: `Administrator`,
`WarehouseOperator`, `Manager`). Order endpoints are mapped onto the existing
three roles as the closest fit to the M2 personas:

| M2 persona | Mapped to | Can do |
|---|---|---|
| Sales agent | `Administrator` | Create orders, add items, cancel |
| Warehouse operator | `Administrator` **or** `WarehouseOperator` | Process (deduct stock), complete |
| Manager | any authenticated role | Browse/read orders, view audit history (read-only by convention — no write endpoints are opened up to `Manager`) |

## Assumptions 

- **Customer data**: no `Customer` entity existed in M1. Added a minimal one
  (`Name`, `Email`, `Phone`, `IsActive`) — just enough for "an order belongs
  to a customer." No customer-management workflow beyond create/get/list.
- **Where stock changes happen**: stock is deducted the moment an order
  enters `Processing` (not at creation, and not at `Completed`) — that's the
  point in the process the milestone calls "required stock is checked and
  handled," and it's the only point where two concurrent orders can race for
  the same units.
- **Order states**: `Pending → Processing → Completed`, with `Cancelled`
  reachable from the first two. Kept to four states because the milestone
  doesn't specify a longer pipeline (e.g. shipped/delivered) and partial
  fulfillment/returns are explicitly listed as *optional* enhancements.
- **Two necessary M1 touches**: (1) `Product` gained a `Price` property —
  M1 had no commercial value on a product at all, so there was nothing to
  snapshot onto an order line without it. (2) `CreateProductDto`/
  `UpdateProductDto`/`ProductResponseDto` were extended with that same
  `Price` field, purely so an Admin has a way to set it — otherwise it could
  never be anything but its default. No other M1 file's behavior changed.
- One warehouse per order line: an `OrderItem` fulfills from a single
  `WarehouseId` (matching how stock is already tracked per-warehouse in M1);
  splitting one product across warehouses within a single order line isn't
  supported.

## Demo script

```
POST /api/auth/login                          { "username": "admin", "password": "Admin@12345" }
POST /api/products                             price included in the body now
POST /api/stock/assign                          give the product stock in a warehouse
POST /api/customers                             { "name": "Acme Co" }

POST /api/orders                                Idempotency-Key: demo-1   -> creates with multiple items, Pending
POST /api/orders/{id}/process                   Idempotency-Key: demo-2   -> deducts stock, -> Processing
POST /api/orders/{id}/complete                                             -> Completed
POST /api/orders/{id}/cancel  (on a different, still-Pending order)        -> Cancelled, no stock touched
POST /api/orders/{completedId}/cancel                                     -> 409 (invalid transition, nothing changes)

# Two users racing for the last unit: send two POST /api/orders/{id}/process
# for two different orders against the same nearly-empty stock row at once -
# one succeeds, the other gets 409 "Insufficient stock..." (or a transparent
# retry if it was a pure RowVersion race rather than an actual stock shortage).

# Repeated critical request: resend the exact same POST /api/orders/{id}/process
# with the same Idempotency-Key header - the cached response comes back and
# stock is not deducted a second time.
```


## What I'd improve next

- Partial fulfillment and a return flow (both listed as optional enhancements).
- An `OrderReferenceNumber` that's human-friendly (currently just the numeric ID).
- Pagination/search extended to the M1 list endpoints (Products/Warehouses),
  which were intentionally left untouched for this milestone.
- Read-side caching for the order list once real usage patterns are known —
  premature right now given there's no production traffic to profile.
- A background job to auto-expire `IdempotencyRecord` rows after some retention window.
