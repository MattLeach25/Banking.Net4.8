# LegacyBanking

A legacy .NET Framework 4.8 banking monolith built with ASP.NET MVC 5, WCF, Entity Framework 6, and SQL Server LocalDB.

## What is included
- MVC dashboard, customer onboarding, account management, and payment entry screens
- A WCF service endpoint hosted in the same web application
- A LocalDB-backed data layer seeded with sample customers, accounts, and transactions
- A classic monolithic structure that is intentionally suitable for modernization exercises

## Solution layout
- `src/LegacyBanking.Domain` - entities and enums
- `src/LegacyBanking.Data` - EF6 context, seed data, and repository operations
- `src/LegacyBanking.Application` - bank workflows and DTO mapping
- `src/LegacyBanking.Web` - ASP.NET MVC UI plus hosted WCF service

## Data storage
The app now uses a local text file instead of SQL Server/LocalDB.

- Data file: `src/LegacyBanking.Web/LegacyBankingDb.txt`
- The file is created automatically on first run with seeded sample customers, accounts, and transactions.
- No external database provider is required.

## Notes
This is intentionally a legacy-style architecture for later modernization work. It is not designed as a greenfield banking platform.
