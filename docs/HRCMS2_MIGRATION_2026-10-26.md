# Local SQL to Azure hrcms2 — 26/10/2026

Requested destination: `hrcms.database.windows.net / hrcms2`.
Source: `.\SQLEXPRESS / HRCMS`, accessed through local Windows authentication.

## Copy verification

- Destination was confirmed empty before creating the schema.
- A consistent source snapshot was read in a serializable transaction and saved privately outside Git.
- Applied the checked-in EF SQL migration script: 15 migrations, 38 business tables and `__EFMigrationsHistory`.
- Copied every table, including empty tables, preserving IDs, account password hashes, relationships, notification read state, audit records and realtime outbox data.
- Replaced only migration seed rows in the new destination, inside one data transaction. Existing databases were not modified.
- Compared row counts and SHA-256 digests of every column and row for all 39 tables before committing. Every comparison matched: **520 rows** in total, including 15 migration history rows.
- Re-enabled and validated all foreign key and check constraints; none remained disabled or untrusted.

| Selected table | Rows copied |
| --- | ---: |
| Users | 10 |
| Inventory | 17 |
| Stables / Stalls | 3 / 14 |
| Templates / TrainerExpertise | 5 / 11 |
| Replenishments / StockMovements | 17 / 17 |
| Notifications | 68 |
| Audit | 71 |
| RealtimeOutboxMessages | 272 |
| Horses | 0 |

The private snapshot and per-table verification manifest are stored under `%LOCALAPPDATA%/HRCMS/migrations/hrcms2-20261026/`. They contain operational data and must stay outside Git. The row counts above describe the copy before the application resumes normal writes.

## Connection configuration

Production uses the Container App `sql-connection` secret via `ConnectionStrings__SqlServer`; the public appsettings target, deployment example and Azure SQL tools default to `hrcms2`. Passwords remain outside source control. Local development User Secrets continue to use `.\SQLEXPRESS / HRCMS`.

Runtime `Database__AutoMigrate` remains `false` because all migrations were applied before the connection switch. Future migrations must be applied to `hrcms2` before deploying code that requires them.
