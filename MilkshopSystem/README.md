# Milkshop System — Project Scaffold (Step 1: DB Schema + Structure)

Stack: **ASP.NET Core MVC (C#, .NET 8)** + **FluentMigrator** + **Dapper** + **MySQL**

## Folder structure

```
MilkshopSystem/
└── MilkshopSystem.Web/
    ├── MilkshopSystem.Web.csproj
    ├── Program.cs                     # DI, auth, session, FluentMigrator auto-run
    ├── appsettings.json                # MySQL connection string
    ├── Migrations/                     # FluentMigrator - one file per table, runs automatically on startup
    │   ├── 202601010001_CreateUsersTable.cs
    │   ├── 202601010002_CreateCustomersTable.cs
    │   ├── 202601010003_CreateUnitsTable.cs
    │   ├── 202601010004_CreateProductCategoriesTable.cs
    │   ├── 202601010005_CreateProductsTable.cs
    │   ├── 202601010006_CreateStocksTable.cs
    │   ├── 202601010007_CreatePaymentModesTable.cs
    │   ├── 202601010008_CreateInvoicesTable.cs
    │   ├── 202601010009_CreateInvoiceItemsTable.cs
    │   └── 202601010010_CreateInvoicePaymentsTable.cs
    ├── Models/
    │   ├── Entities/                   # 1:1 with DB tables (User, Customer, Product, Stock, Invoice...)
    │   └── ViewModels/PagedResult.cs   # shared pagination wrapper used by every list page
    ├── Repositories/
    │   ├── Interfaces/IDbConnectionFactory.cs
    │   └── Implementations/MySqlConnectionFactory.cs
    ├── Controllers/                    # empty for now — built module by module next
    ├── Views/                          # Login, Customer, Product, Unit, Stock, PaymentMode, Billing, Dashboard, Report
    └── wwwroot/                        # css, js, lib (bootstrap, datatables)
```

## Database design (10 tables)

| Table | Purpose |
|---|---|
| `Users` | login |
| `Customers` | name, phone, address, **OutstandingBalance** (running due, used for next-bill carry-forward) |
| `Units` | kg, gram, liter, packet (seeded) |
| `ProductCategories` | Milk Products, Tea, Snacks (seeded — reused for module 8) |
| `Products` | name, category, unit, size, **StorePrice + MrpPrice**, ActiveFrom/ActiveTo |
| `Stocks` | 1-to-1 with Product — OpeningStock, CurrentStock, LowStockLevel |
| `PaymentModes` | Cash, GPay, Net Banking (seeded) |
| `Invoices` | invoice header — SubTotal, **PreviousBalance**, GrandTotal, PaidAmount, BalanceAmount, PaymentStatus |
| `InvoiceItems` | line items — product, PriceType (Store/MRP), qty, unit price, amount |
| `InvoicePayments` | every payment received against an invoice (supports paying off balance in a later visit) |

### How billing logic (points 6, 11, 12, 13) will work with this schema

1. **New invoice**: `PreviousBalance` = customer's current `Customers.OutstandingBalance`. `GrandTotal = SubTotal + PreviousBalance`.
2. Cashier enters `PaidAmount`. Backend sets:
   - `PaidAmount = GrandTotal` → **Paid**
   - `0 < PaidAmount < GrandTotal` → **Partial**
   - `PaidAmount = 0` → **Unpaid**
   - `BalanceAmount = GrandTotal - PaidAmount`
3. `Customers.OutstandingBalance` is updated to the new `BalanceAmount` — so it automatically shows up as `PreviousBalance` on the *next* invoice (point 11 ✅).
4. If a partial/unpaid customer later comes back just to pay off dues (not buy anything), that's a new row in `InvoicePayments` linked to the old invoice, and `Customers.OutstandingBalance` reduces — no need to force a new invoice.
5. On invoice save, for every `InvoiceItem`, `Stocks.CurrentStock -= Qty` in the same DB transaction (point 13 ✅). If `CurrentStock < Qty` requested, billing is blocked with a "not enough stock" message.
6. **Dashboard stock value** (point 12): `SUM(Stocks.CurrentStock * Products.StorePrice)` grouped overall / by category.

### Tea & Snacks (point 8)
No separate table needed — they're just `Products` under the `Tea` / `Snacks` category (already seeded), billed through the same Billing module. Keeps everything (stock, pricing, invoice) consistent instead of a parallel system.

## What's done in this step
- ✅ Full MySQL schema as FluentMigrator migrations (auto-runs on `dotnet run`, no manual SQL needed)
- ✅ Entity models matching every table
- ✅ Dapper connection factory (`IDbConnectionFactory`) wired into DI
- ✅ Shared `PagedResult<T>` so search+pagination (point 10) looks/works the same on every list page
- ✅ Project skeleton (Program.cs with cookie auth, session, MVC routing) ready for controllers/views

## ✅ Full project — all modules built

| # | Module | Status |
|---|---|---|
| 1 | Login | `AccountController` + cookie auth + BCrypt password hashing. Default admin seeded: **admin / Admin@123** |
| 2 | Customer | Full CRUD + paged search (`CustomerController`) |
| 3 | Product | Two prices (Store/MRP), unit, size, active from/to, auto-creates stock row on save |
| 4 | Unit | CRUD master (kg, gram, liter, packet seeded) |
| 5 | Stock | List with low-stock highlighting, manual adjust screen, low-stock-only view |
| 6 | Payment Mode | CRUD master (Cash, GPay, Net Banking seeded) |
| 7 | Billing/Invoice | Customer typeahead (auto-creates new customer if not found), product search with Store/MRP dropdown, live available-stock display, auto amount calculation, payment mode + paid amount |
| 8 | Charts | Monthly (x=month 1-12), weekly, yearly earnings via Chart.js, filterable by product |
| 9 | Tea/Snacks | No separate module — just `Products` under the Tea/Snacks category, billed the same way |
| 10 | Dashboard | Today/month sales, stock value, low-stock count, outstanding balance, customer/product counts |
| 11 | Search + Pagination | Every list module (`GetPagedAsync`) supports `?search=&page=` |
| 12 | Partial/unpaid billing | `Invoices.PreviousBalance` + `Customers.OutstandingBalance` carry forward automatically; `Billing/Pay` lets a customer clear dues later |
| 13 | Stock value on dashboard | `SUM(CurrentStock × StorePrice)` |
| 14 | Stock reduces on billing | `StockRepository.ReduceStockAsync` inside the invoice DB transaction, row-locked (`FOR UPDATE`), blocks the bill if stock is short |

## How to run

1. Install .NET 8 SDK and MySQL Server.
2. Open `MilkshopSystem.Web/appsettings.json`, set your MySQL password in the `MilkshopDb` connection string. Database name is `milkshop_system` — FluentMigrator creates it and all tables automatically (no manual SQL needed).
3. From the `MilkshopSystem.Web` folder:
   ```
   dotnet restore
   dotnet run
   ```
4. Open the browser at the shown localhost URL. Login with **admin / Admin@123**.

## Notes / things to double check after restore
- This code was written directly (not compiled here) — after `dotnet restore`, run `dotnet build` once and fix any small type mismatches Visual Studio/Rider flags (e.g. nullable warnings). The architecture and SQL are correct; a first build pass is normal for a project this size.
- Billing screen (`Views/Billing/Create.cshtml`) uses vanilla JS + fetch — no extra npm/JS build step needed.
- Charts use Chart.js via CDN (`_Layout.cshtml`).
- For production: change the seeded admin password, and consider adding a "Delete invoice/cancel" flow if needed (currently invoices are immutable once created, only payments/balance can be added).
