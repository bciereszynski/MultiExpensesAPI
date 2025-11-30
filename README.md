# MultiExpensesAPI

Useful REST API for recording and managing expense/income transactions for groups.

---

## SUMMARY

MultiExpensesAPI is an **ASP.NET Core Web API** that stores `Transaction`, `User`, and `Group` entities in **SQL Server** using **Entity Framework Core**. It exposes CRUD endpoints for transactions, groups, users, and group invitations. The API includes **JWT authentication** and **Swagger** in development mode.

---

## REQUIREMENTS

* **.NET 9 SDK**
* **SQL Server** (LocalDB, SQL Express, or full SQL Server)
* A code editor/IDE such as Visual Studio 2022 or VS Code

---

## QUICK START

1.  **Clone the repository**
    * `git clone` the project or open the existing repo in Visual Studio.

2.  **Configure the database connection**
    * Edit `appsettings.json` and set the `DefaultConnection` connection string to your SQL Server instance.
    * **Example:**
        ```json
        "ConnectionStrings": {
          "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MultiExpensesDb;Trusted_Connection=True;"
        }
        ```

3.  **Configure JWT settings**
    * Edit `appsettings.json` and set the JWT configuration:
        ```json
        "Jwt": {
          "SecretKey": "your-secret-key-here",
          "Issuer": "your-issuer",
          "Audience": "your-audience"
        }
        ```

4.  **Apply EF migrations to create the database**
    * From the project folder run:
        ```bash
        dotnet ef migrations add Init
        dotnet ef database update
        ```
    * *Or* in Visual Studio use the Package Manager Console:
        ```powershell
        Update-Database
        ```

5.  **Run the API**
    * In Visual Studio open the solution and start debugging with F5.
    * *Or* run from command line:
        ```bash
        dotnet run
        ```

6.  **Access the API**
    * API will be available at `https://localhost:{port}`
    * Swagger UI is available in Development at `https://localhost:{port}/swagger`

---

## FEATURES

* User authentication with **JWT tokens**
* User registration and management
* Group creation and management
* **Group invitation system** with token-based invites
* Member management for groups
* Transaction tracking (expenses and income) per group
* **Role-based access control** for group members
* CORS support for cross-origin requests

---

## AUTHENTICATION

The API uses **JWT Bearer token authentication**. To access protected endpoints:

1.  Register a new user or login with existing credentials via `api/Auth/Login`
2.  Include the JWT token in the `Authorization` header for subsequent requests:
    > `Authorization: Bearer {your-token-here}`

---

## API ENDPOINTS

### --- Authentication (`api/Auth`) ---

* `POST api/Auth/Login`
    * Authenticate user and receive JWT token.
    * Body: `{ "email": "user@example.com", "password": "password" }`
* `POST api/Auth/Register`
    * Register a new user account.
    * Body: `{ "email": "user@example.com", "password": "password" }`

### --- Users (`api/Users`) ---

* `GET api/Users/All` - Returns all users (authenticated).
* `GET api/Users/Details/{id}` - Get a user by id.
* `POST api/Users/Create` - Create a user.
* `PUT api/Users/Update/{id}` - Update an existing user.
* `DELETE api/Users/Delete/{id}` - Delete a user by id.

### --- Groups (`api/Groups`) ---

* `GET api/Groups/All` - Returns all groups for the authenticated user.
* `GET api/Groups/Details/{id}` - Get a group by id.
* `POST api/Groups/Create` - Create a new group.
    * Body: `{ "name": "Group Name" }`
* `PUT api/Groups/Update/{id}` - Update an existing group.
* `DELETE api/Groups/Delete/{id}` - Delete a group by id.

### --- Group Invitations (`api/Groups/{groupId}/Invitations`) ---

* `POST api/Groups/{groupId}/Invitations/Create` - Create an invitation token for a group.
* `POST api/Groups/{groupId}/Invitations/Accept` - Accept a group invitation using a token.

### --- Members (`api/Groups/{groupId}/Members`) ---

* `GET api/Groups/{groupId}/Members/All` - Returns all members of a group.
* `DELETE api/Groups/{groupId}/Members/Remove/{userId}` - Remove a member from a group.

### --- Transactions (`api/Transactions`) ---

* `GET api/Transactions/All` - Returns all transactions.
* `GET api/Transactions/Details/{id}` - Get a transaction by id.
* `POST api/Transactions/Create` - Create a transaction.
    * Body: `{ "type": "Expense", "amount": 12.50, "category": "Food", "description": "Lunch", "groupId": 1 }`
* `PUT api/Transactions/Update/{id}` - Update an existing transaction.
* `DELETE api/Transactions/Delete/{id}` - Delete a transaction by id.

---

## DATA MODELS

### User

* **Id** (`int`)
* **Email** (`string`, unique)
* **Password** (`string`, hashed)
* **CreatedAt** (`DateTime`)
* **LastUpdatedAt** (`DateTime`)
* **Groups** (collection)
* **Transactions** (collection)

### Group

* **Id** (`int`)
* **Name** (`string`)
* **CreatedAt** (`DateTime`)
* **LastUpdatedAt** (`DateTime`)
* **Members** (collection of Users)
* **Transactions** (collection)

### Transaction

* **Id** (`int`)
* **Type** (`string`: Expense or Income)
* **Amount** (`double`)
* **Category** (`string`)
* **Description** (`string`, optional)
* **GroupId** (`int`)
* **UserId** (`int`, optional)
* **CreatedAt** (`DateTime`)
* **LastUpdatedAt** (`DateTime`)

### GroupInvitation

* **Id** (`int`)
* **Token** (`string`, unique)
* **GroupId** (`int`)
* **ExpiresAt** (`DateTime`)
* **CreatedAt** (`DateTime`)
* **LastUpdatedAt** (`DateTime`)

---

## DEVELOPMENT NOTES

* The API uses **Entity Framework Core** with **SQL Server**
* Password hashing is implemented using `PasswordHasher`
* CORS is configured to allow **all origins in development mode**
* JSON serialization ignores reference cycles
* Swagger UI includes **JWT authentication support**

---