#SQL Server
#dotnet ef dbcontext scaffold "Data Source=localhost;Initial Catalog=jewelry; Persist Security Info=False; User ID=masterjewelry;Password=P@ssw0rd#1; TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models/Jewelry --context-dir Context/ --context JewelryContext -f --no-pluralize

#PostgreSQL
dotnet ef dbcontext scaffold 'Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=winsun24;Trust Server Certificate=true;' Npgsql.EntityFrameworkCore.PostgreSQL -o Models/Jewelry --context-dir Context/ --context JewelryContext -f --no-pluralize 
#dotnet ef dbcontext scaffold "Data Source=localhost;Initial Catalog=miraculous; Persist Security Info=False; User ID=mastercoqo;Password=Winsun24@1;Trusted_Connection=True; TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServe -o Models/ --context-dir Context/ --context JewelryContext


#dotnet ef dbcontext scaffold "Data Source=localhost;Initial Catalog=miraculous; Persist Security Info=False; User ID=mastercoqo;Password=Winsun24@1;Trusted_Connection=True; TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models/



#dotnet ef migrations add Init
#dotnet ef database update
