# MultiExpensesAPI

Useful REST API for recording and managing expense/income transactions for groups.

## Summary
`MultiExpensesAPI` is an ASP.NET Core Web API that stores `Transaction` and `User` entities in SQL Server using Entity Framework Core. It exposes basic CRUD endpoints for transactions and includes Swagger in development.

## Requirements
- .NET 9 SDK
- SQL Server (LocalDB, SQL Express, or full SQL Server)
- A code editor/IDE such as Visual Studio 2022 or VS Code

## Quick start

1. Clone the repository:
   - git clone the project or open the existing repo in Visual Studio.

2. Configure the database connection:
   - Edit `appsettings.json` and set the `DefaultConnection` connection string to your SQL Server instance.
     Example:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MultiExpensesDb;Trusted_Connection=True;"
     }
     ```

3. Apply EF migrations to create the database:
   - From the project folder run:
     - __dotnet ef migrations add Init__ (if you change schema)
     - __dotnet ef database update__

   - Or in Visual Studio use the Package Manager Console:
     - `Update-Database`

4. Run the API:
   - In Visual Studio open the solution and start debugging with __F5__ (or run the project).
   - Or run from command line:
     - __dotnet run__

5. API will be available at `https://localhost:{port}`. Swagger UI is available in Development at `https://localhost:{port}/swagger`.

## Endpoints
Base route: `api/Transactions`

- GET `api/Transactions/All`  
  Returns all transactions.

- GET `api/Transactions/Details/{id}`  
  Get a transaction by id.

- POST `api/Transactions/Create`  
  Create a transaction. Body: JSON matching `PostTransactionDto`.

- PUT `api/Transactions/Update/{id}`  
  Update an existing transaction. Body: JSON matching `PostTransactionDto`.

- DELETE `api/Transactions/Delete/{id}`  
  Delete a transaction by id.

## DTO / Body example
Request body for `Create` and `Update` (JSON):
{ "type": "Expense", "amount": 12.50, "category": "Food", "description": "Lunch" }
