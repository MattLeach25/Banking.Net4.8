# Modernization Plan: LegacyBanking Azure Cloud Readiness

**Project**: LegacyBanking

---

## Technical Framework

- **Language**: C# (.NET Framework 4.8)
- **Framework**: ASP.NET MVC 5 + WCF
- **Build Tool**: MSBuild
- **Database**: SQL Server (on-premises / LocalDB)
- **Key Dependencies**: System.Data.SqlClient, Windows Authentication, local file I/O

---

## Overview

This migration prepares the LegacyBanking application for Azure by removing
cloud-readiness blockers identified in the AppCAT assessment
(`report-20260616125211`). The application currently relies on local file
system access, on-premises SQL Server with `System.Data.SqlClient`, Windows
Active Directory authentication, and plaintext connection strings. The
modernized architecture will:

- Replace local file I/O with cloud-appropriate storage handling.
- Use the actively maintained `Microsoft.Data.SqlClient` driver.
- Connect to Azure SQL Database using Managed Identity (passwordless).
- Authenticate users through Microsoft Entra ID instead of Windows AD.
- Protect secrets with Managed Identity and Azure Key Vault.

The migration follows a sequenced approach: storage and data-access
foundations first, then the SQL platform move, then identity and secret
management.

---

## Migration Impact Summary

| Application       | Original Service        | New Azure Service          | Authentication   | Comments                                  |
|-------------------|-------------------------|----------------------------|------------------|-------------------------------------------|
| LegacyBanking.Data | Local/network file I/O  | Cloud-appropriate storage  | Managed Identity | System.IO.File usage in BankingDbContext  |
| LegacyBanking      | System.Data.SqlClient   | Microsoft.Data.SqlClient   | Managed Identity | Driver prerequisite for Azure SQL + MI    |
| LegacyBanking      | SQL Server (on-prem)    | Azure SQL Database         | Managed Identity | Passwordless connection                   |
| LegacyBanking.Web  | Windows AD              | Microsoft Entra ID         | Entra ID         | Windows authentication in Web.config      |
| LegacyBanking.Web  | Plaintext credentials   | Azure Key Vault            | Managed Identity | Connection strings without secret store   |

---

## Tasks

The detailed, executable task breakdown is maintained in
[tasks.json](tasks.json). Summary of scoped tasks:

1. **File System Management Migration** — Replace local/network file I/O.
2. **System.Data.SqlClient → Microsoft.Data.SqlClient** — Driver modernization.
3. **SQL Server → Azure SQL Database (Managed Identity)** — Passwordless data platform.
4. **Windows AD → Microsoft Entra ID** — Cloud identity.
5. **Plaintext Credentials → Managed Identity + Azure Key Vault** — Secret protection.

---

## Clarifications

The following item was noted during planning and may warrant confirmation:

1. **Data layer persistence state**
   - **Why needed**: The current data layer appears to use a local text-file
     store (`LegacyBankingDb.txt`), while the assessment flagged SQL Server
     connection strings in `Web.config`. The SQL Server and SqlClient tasks
     assume relational SQL access is (or will be) the active persistence path.
   - **Options**: (a) Proceed with the SQL-targeted tasks as scoped; (b) drop
     the SQL tasks if the file-based store is the intended final state.
   - **Recommendation**: Proceed as scoped; the SqlClient and Azure SQL tasks
     are no-ops if no live SQL access remains.
