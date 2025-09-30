# StealAllTheCats - ASP.NET Core Web API (.NET 8)

This is a Web API that "steals" cat images from TheCatAPI (https://thecatapi.com/) and stores them in a SQL Server database along with their temperament tags.  
You can fetch, query, and filter cats with paging support.

## Projects

-**StealAllTheCatsWebApi**-> ASP.NET Core Web API  
-**ORM** -> Contains `StealTheCatsContext` and EF migrations  
-**LibCat** -> Library  
-**UnitTests** ->  Contains all the unit tests for the web api  

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)  
- [SQL Server 2019+](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

## Configuration

Update **`appsettings.json`** in `StealAllTheCatsWebApi`
with your connection string in sql server and your api-key

Update method  **`OnConfiguring`** with your sql server connection string


##  Database Setup

If your schema is not applied yet, run migrations from **/ORM project**:
Open Package Manager Console and run
# From the /ORM project folder
```
dotnet ef migrations add Initial
update-database
```

## Build & Run

From the **Web API project**:

```bash
dotnet restore
dotnet build
dotnet run --launch-profile "https"
```

The API will be available at:

- `https://localhost:7169/swagger`

Swagger UI will show all endpoints.

---

## Endpoints

### Import cats from TheCatAPI
```
POST /api/cats/fetch
```
- Fetches 25 cat images with breeds from TheCatAPI.  
- Saves **new cats only** (no duplicates).  
- Extracts temperaments into **tags** (unique by name).
---

### Query cats from the database

- `GET /api/cats/{id}`  
  Get a single cat by its database id.

- `GET /api/cats?page=1&pageSize=10`  
  Get paged cats.

- `GET /api/cats?tag=Playful&page=1&pageSize=10`  
  Get paged cats filtered by a specific tag.
---

### Query directly from TheCatAPI
```
GET /api/cats/external/{id}
```
- Fetches a cat by its **TheCatAPI image id** (not your DB id).  
- Return cat,image URL, and temperament tags.
---

## Run Web Api with Docker

From the **StealAllTheCatsWebApi** project folder 
```bash
docker build -t cats-api -f Dockerfile ..
winpty docker run --rm -p 7169:8080 -e ASPNETCORE_ENVIRONMENT=Development cats-api
```
The API will be available at:

- `https://localhost:7169/swagger`
---

## Notes

- **Image storage**: Only the **URL** is stored in the DB.  
- **Tags**: Parsed from `breeds\temperament` (comma-separated), stored uniquely.  
- **No duplicates**: Import skips cats already in DB.   

---

## Git Ignore

If build artifacts are showing up in Git, make sure you have `.gitignore`:

```
bin/
obj/
*.dll
*.exe
*.pdb
```

To clean already tracked build files:

```bash
git rm -r --cached .
git add .
git commit -m "Clean and apply .gitignore"
```


