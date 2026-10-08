# Azure SQL — HRCMS

Server: `hrcms.database.windows.net:1433`. Database: `HRCMS`. SQL administrator login: `hrcms`.

Backend uses SQL Server EF migrations. Frontend continues to call the backend at localhost:5299 through the Vite proxy; database credentials never belong in frontend configuration.

`appsettings.json` contains the public Azure target only. The full SQL Login connection string, including password, must be supplied by User Secrets or `ConnectionStrings__SqlServer`. Existing local SQL Server User Secrets override appsettings until replaced.

Run from the backend repository in your own PowerShell terminal:

```powershell
./tools/Set-AzureSqlConnection.ps1
```

The script prompts for a hidden password and updates only the SQL connection/provider/migration settings. It keeps other existing secrets. It does not apply migrations, print credentials or write a password into the repository. User Secrets are local development storage; for deployment provide the connection via the hosting environment's secret configuration.

Current schema: [azure-sql-schema.sql](azure-sql-schema.sql), generated from all seven migrations, including the three realtime outbox migrations. It contains 32 application tables plus __EFMigrationsHistory. Use only an empty database or one already managed by these migrations. No DROP TABLE or DROP DATABASE is included.

After saving the password, [Update-AzureSqlDatabase.ps1](../tools/Update-AzureSqlDatabase.ps1) applies this script and verifies 32 application tables and the migrations present in the DAL source (currently seven). It reads the connection from User Secrets and supplies the password through SQLCMDPASSWORD instead of command-line arguments. It rejects the wrong target, existing unmanaged tables or unknown migration history. Requires sqlcmd.

Verified on 07/10/2026: the Azure database was empty before migration. All three migrations were applied successfully; 31 business tables plus __EFMigrationsHistory are present. The full SQL Login connection is stored in local User Secrets, without a password in source.

The backend was restarted at localhost:5299 using Azure SQL. Direct health, Swagger and frontend proxy health at localhost:5173 returned HTTP 200. The frontend live smoke test passed login/me, enum metadata, dashboard and horse-list reads, then logout, using the existing Bootstrap Manager configuration. Startup created the Manager from that configuration in the new Azure database. No fixture data was inserted and no local database data was copied to Azure.

Runtime AutoMigrate remains false. Future schema changes require reviewed migrations and updating the Azure deployment script. This verifies local API-to-Azure connectivity; it does not deploy the API or frontend to Azure hosting.

TLS uses Encrypt=True and TrustServerCertificate=False. Reference: [Microsoft SqlClient connection string syntax](https://learn.microsoft.com/en-us/sql/connect/ado-net/connection-string-syntax?view=sql-server-ver17).

The fourth migration adds composite query indexes and replaces redundant single-column indexes. No business tables/columns or stored records are changed. The earlier three-migration verification above is the initial Azure setup history.

Verified again on 08/10/2026 at the user's request: the three realtime outbox migrations were applied to shared HRCMS. Current schema has 32 application tables plus __EFMigrationsHistory and seven migrations. docs/azure-sql-schema.sql now includes all seven migrations; Update-AzureSqlDatabase.ps1 verifies the current count. Temporary API smoke passed health, login/me, dashboard, horse/notification reads, SignalR negotiate and authenticated WebSocket handshake. Outbox insert/update/read and rollback passed without retaining a test row. Workers and dispatcher were disabled during this smoke; the temporary API was stopped afterward. Full integration fixtures remain isolated from the shared database.
