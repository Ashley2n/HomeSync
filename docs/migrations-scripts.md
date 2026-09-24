## Dotnet Ef Migrations Commands

---
**Write Migration Script** 
```csharp
    dotnet ef migrations add [Name] --project .\infrastructure\infrastructure.csproj --startup-project .\api\api.csproj
```

**Apply Migrations** 
```csharp
    dotnet ef database update --project .\infrastructure\infrastructure.csproj --startup-project .\api\api.csproj
```