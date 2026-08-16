# EF Core Practice Notes

## 1. DbContext, DbSet and EntityEntry

- `DbContext` represents a unit of work and contains the EF Core `ChangeTracker`.
- `DbSet<T>` is used to query and work with a set of entities.
- `DbContext.Entry(entity)` returns an `EntityEntry<T>` — EF tracking information for one particular entity.
- `EntityEntry` provides access to:
  - `State`
  - `OriginalValues`
  - `CurrentValues`
  - properties
  - references / collections
  - `ReloadAsync()`
  - `GetDatabaseValuesAsync()`

**Memory**

- `DbSet` = query / data side
- `Entry` = tracking / state side

---

## 2. IQueryable and LINQ-to-SQL

Operations such as `Where`, `Select`, `OrderBy`, `Skip`, and `Take` build an `IQueryable`.

Terminal operations such as `ToListAsync`, `FirstOrDefaultAsync`, `SingleAsync`, and `CountAsync` execute the query.

### Important

Not every .NET method can be translated to SQL.

Example that failed:

```csharp
o.Status.Equals(status, StringComparison.CurrentCultureIgnoreCase)
```

For PostgreSQL case-insensitive search we used:

```csharp
EF.Functions.ILike(...)
```

**Memory**

> `IQueryable` builds an expression tree; terminal operators execute it.

---

## 3. Projection vs Include

For read-only APIs, prefer projection when possible:

```csharp
.Select(o => new OrderDetailsResponse
{
    Id = o.Id,
    OrderNumber = o.OrderNumber,
    Status = o.Status
})
```

Advantages:

- selects only required columns;
- creates DTOs directly;
- avoids exposing EF entities;
- avoids JSON cycles caused by bidirectional navigation properties.

Use `Include()` when the actual related entities are required, especially for business logic or updates.

**Memory**

- Need DTO / data shape → `Select`
- Need related entities → `Include`

---

## 4. Relationships

Model:

```text
Order 1 ---- * OrderItem
```

- `OrderItem.OrderId` = foreign key
- `OrderItem.Order` = reference navigation
- `Order.Items` = collection navigation

EF conventions detected:

- primary key;
- foreign key;
- one-to-many relationship;
- FK index;
- cascade delete.

Database cascade delete allowed PostgreSQL to delete `OrderItems` when an `Order` was deleted without EF loading those items.

---

## 5. N+1 Problem

Bad:

```csharp
var orders = await db.Orders.ToListAsync();

foreach (var order in orders)
{
    var count = await db.OrderItems
        .CountAsync(i => i.OrderId == order.Id);
}
```

Produces:

```text
1 query for Orders
+ N queries for related data
```

Better:

```csharp
.Select(o => new
{
    o.Id,
    ItemCount = o.Items.Count
})
```

EF translates the whole expression into one SQL query.

**Memory**

> A DB query inside a `foreach` is an N+1 warning sign.

---

## 6. Useful LINQ Translations

### Any

```csharp
Any()
```

→ SQL `EXISTS`.

### All

```csharp
All(condition)
```

→ usually SQL `NOT EXISTS` for rows violating the condition.

Important:

```csharp
emptyCollection.All(...) == true
```

If the collection must also contain at least one item:

```csharp
o.Items.Any() &&
o.Items.All(...)
```

### Count

`Count()` → SQL `COUNT(*)`.

### Sum

`Sum(...)` → SQL `SUM(...)`.

### GroupBy

`GroupBy(...)` → SQL `GROUP BY`.

### Contains over a collection

```csharp
statuses.Contains(o.Status)
```

With Npgsql it translated to:

```sql
"Status" = ANY (@statuses)
```

---

## 7. COUNT(_) vs SELECT _

These are completely different.

```sql
SELECT *
```

returns row data.

```sql
COUNT(*)
```

counts rows.

Use:

- `Any()` / `EXISTS` when only existence is required;
- `Count()` when the actual count is required.

---

## 8. ExecuteUpdateAsync

`ExecuteUpdateAsync()` executes an `UPDATE` directly in the database.

It:

- does not materialize entities;
- bypasses `ChangeTracker`;
- does not require `SaveChangesAsync()`.

Example:

```csharp
await db.Orders
    .Where(o => o.Status == OrderStatus.Pending)
    .ExecuteUpdateAsync(s =>
        s.SetProperty(
            o => o.Status,
            OrderStatus.Cancelled));
```

### Important

Already tracked entities are not automatically synchronized.

Use:

```csharp
await db.Entry(order).ReloadAsync();
```

if synchronization is required.

**Memory**

> `ExecuteUpdate` / `ExecuteDelete` bypass the `ChangeTracker`.

---

## 9. SaveChanges and Transactions

A single `SaveChanges()` / `SaveChangesAsync()` call is transactional by default when the provider supports transactions.

When saving:

```text
Order
└── OrderItems
```

a failure while saving the graph rolls the operation back.

We verified that a failed duplicate `Order` insert did not leave orphan `OrderItems`.

**Memory**

> One `SaveChanges` call is normally all-or-nothing.

---

## 10. Migrations

Typical workflow:

```text
Change EF model
→ migrations add
→ review Up / Down
→ inspect generated SQL
→ test migration
→ commit migration
→ CI/CD applies deployment script
```

Create a migration:

```powershell
dotnet ef migrations add MigrationName
```

Generate SQL:

```powershell
dotnet ef migrations script
```

Generate SQL between specific migrations:

```powershell
dotnet ef migrations script FromMigration ToMigration
```

### Important

Generated migrations must always be reviewed.

EF cannot infer every data migration.

Example: changing `Status` from arbitrary string usage to enum-backed strings required manually normalizing:

```text
pending → Pending
```

---

## 11. Enum Mapping

C#:

```csharp
OrderStatus.Pending
```

API representation:

```text
JsonStringEnumConverter
→ "Pending"
```

Database representation:

```text
HasConversion<string>()
→ PostgreSQL text "Pending"
```

These are two independent conversions.

**Memory**

```text
HTTP JSON ↔ C# enum ↔ PostgreSQL text
```

with separate converters on each boundary.

---

## 12. Database Constraints

We added a unique index on:

```text
Orders.OrderNumber
```

A duplicate insert produced:

```text
PostgreSQL error 23505
→ PostgresException
→ DbUpdateException
```

For this known conflict the API returns:

```text
409 Conflict
```

The exception filter also checks the specific constraint name.

---

## 13. Optimistic Concurrency

Problem:

```text
A reads Pending
B reads Pending

B saves Rejected
A saves Cancelled

without protection:
B's update is silently lost
```

This is a **lost update**.

### Terms

- **Concurrency** — simultaneous work with the same data.
- **Race condition** — result depends on execution timing / order.
- **Lost update** — one update silently overwrites another.
- **Stale data** — an outdated version of data.
- **Optimistic concurrency** — allow concurrent access and detect conflict when saving.
- **Concurrency token** — value used to detect a changed row.
- **Concurrency conflict** — database version no longer matches the version originally read.

For PostgreSQL / Npgsql we use the system column:

```text
xmin
```

mapped as:

```csharp
public uint Version { get; set; }
```

with:

```csharp
modelBuilder.Entity<Order>()
    .Property(o => o.Version)
    .IsRowVersion();
```

Generated update:

```sql
UPDATE "Orders"
SET ...
WHERE "Id" = @id
  AND xmin = @originalVersion
RETURNING xmin;
```

If another request changed the row:

```text
0 rows affected
→ DbUpdateConcurrencyException
→ HTTP 409 Conflict
```

**Memory**

> Optimistic concurrency = read freely, detect conflict on update.

---

## 14. Disconnected REST Concurrency

Web APIs are disconnected:

```text
GET
→ DbContext ends

time passes

PUT
→ new DbContext
```

Therefore the concurrency version must travel through the client:

```text
PostgreSQL xmin
→ EF Version
→ GET response
→ client
→ PUT request
→ EF OriginalValue
```

We set:

```csharp
db.Entry(order)
    .Property(o => o.Version)
    .OriginalValue = request.Version;
```

This allows EF to detect a client updating data based on an old version.

### Conflict values

- `OriginalValues` = what I originally read.
- `CurrentValues` = what I want to save.
- `DatabaseValues` = what is currently in the database.

### Conflict resolution strategies

- **Database wins**
- **Client wins**
- **Merge**

For REST APIs, returning `409 Conflict` and asking the client to reload is a good default.

`ETag` / `If-Match` is the HTTP-native alternative for exposing the same concept.

**Memory**

> Original = what I read. Current = what I want. Database = what exists now.

---

## 15. EntityEntry

`DbContext.Entry(entity)` gives access to EF tracking information for one specific entity.

Examples:

```csharp
var entry = db.Entry(order);

entry.State;
entry.OriginalValues;
entry.CurrentValues;
```

Reload from the database:

```csharp
await db.Entry(order).ReloadAsync();
```

Access a specific property:

```csharp
db.Entry(order)
    .Property(o => o.Version)
    .OriginalValue = request.Version;
```

**Memory**

```text
DbSet<T>
→ query / work with a set of entities

EntityEntry<T>
→ tracking / state of one specific entity
```

---

## 16. Single Query, Split Query and Cartesian Explosion

Multiple `JOIN`s are not automatically bad.

**Cardinality matters.**

Many-to-one / one-to-one joins often preserve row count:

```text
Order → Pharmacy
Order → Patient
Order → User
```

Multiple sibling one-to-many relationships can multiply rows:

```text
Order
├── 10 Items
└── 10 Comments

10 × 10 = 100 SQL rows
```

This is **cartesian explosion**.

### Why it happens

A `JOIN` does not merely “add columns”.

It creates one result row for every matching combination.

Example:

```text
Order #1
Items:    I1, I2
Comments: C1, C2, C3
```

A query joining both collections can produce:

```text
Order | I1 | C1
Order | I1 | C2
Order | I1 | C3
Order | I2 | C1
Order | I2 | C2
Order | I2 | C3
```

2 items × 3 comments = 6 rows.

EF can reconstruct the correct object graph afterwards, but the database, network and EF still process the expanded result set.

### AsSplitQuery

`AsSplitQuery()` separates collection loading:

```text
Query 1 → Orders
Query 2 → Items
Query 3 → Comments
```

Trade-off:

**Single query**

- fewer DB round-trips;
- potentially large duplicated result sets.

**Split query**

- avoids row multiplication between sibling collections;
- additional DB round-trips;
- separate queries can observe slightly different database states.

**Memory**

```text
One small collection
→ usually SingleQuery

Several large sibling collections
→ consider AsSplitQuery

Read-only API DTO
→ projection may be better than either
```

---

## 17. Practical Senior EF Core Checklist

Before considering a query production-ready, ask:

- Am I materializing more data than I need?
- Should this be a DTO projection instead of an entity graph?
- Is there a DB query inside a loop?
- Do I need `Any()` rather than `Count()`?
- Can this LINQ expression actually be translated to SQL?
- Should the query use `AsNoTracking()`?
- Am I loading multiple sibling collections and risking cartesian explosion?
- Would `ExecuteUpdateAsync()` / `ExecuteDeleteAsync()` be more appropriate?
- Do I understand the generated SQL?
- Are constraints enforced in the database?
- Can concurrent updates cause lost updates?
- Does the API carry a concurrency version in disconnected scenarios?
- Have I reviewed the generated migration before applying it?

---

## 18. Interview Memory Formulas

```text
DbSet = query/data side
Entry = tracking/state side
```

```text
Where / Select / OrderBy / Skip / Take
→ build IQueryable

ToListAsync / FirstAsync / CountAsync
→ execute
```

```text
Need DTO
→ Select projection

Need actual related entities
→ Include
```

```text
Any
→ EXISTS

All
→ NOT EXISTS violating row

Count
→ COUNT(*)

Sum
→ SUM
```

```text
Query inside foreach
→ suspect N+1
```

```text
ExecuteUpdate / ExecuteDelete
→ bypass ChangeTracker
```

```text
Optimistic concurrency
→ read freely
→ check version on update
```

```text
Lost update
→ one concurrent update silently overwrites another
```

```text
Original
→ what I read

Current
→ what I want

Database
→ what exists now
```

```text
Multiple sibling one-to-many JOINs
→ possible cartesian explosion
```

---

## 19. Project-Specific Decisions

For this practice project we intentionally use:

- .NET 8
- EF Core 8
- Npgsql
- PostgreSQL
- `OrderStatus` enum stored as PostgreSQL `text`
- `JsonStringEnumConverter` for API enum strings
- `numeric(18,2)` for `UnitPrice`
- one-to-many `Order → OrderItems`
- database cascade delete
- unique `OrderNumber`
- PostgreSQL `xmin` as optimistic concurrency token
- DTO projection for read APIs
- `409 Conflict` for known database / concurrency conflicts

The goal is not to use every EF Core feature, but to understand the trade-offs and generated SQL well enough to explain and defend the implementation in a Senior .NET interview.
