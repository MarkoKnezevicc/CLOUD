using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class SmartMeteringDbContextFactory : IDesignTimeDbContextFactory<SmartMeteringDbContext>
{
    public SmartMeteringDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SmartMeteringDbContext>();

       
        string connectionString = "Server=.\\SQLEXPRESS;Database=SmartMeteringDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        optionsBuilder.UseSqlServer(connectionString);

        return new SmartMeteringDbContext(optionsBuilder.Options);
    }
}