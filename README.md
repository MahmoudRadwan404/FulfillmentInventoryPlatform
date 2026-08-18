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
