using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace agrify.Data
{
    // This class's only job is to tell the 'Add-Migration' 
    // and 'Update-Database' commands how to get the connection string.
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AgrifyDbContext>
    {
        public AgrifyDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AgrifyDbContext>();

            // === PASTE YOUR SQL EXPRESS CONNECTION STRING HERE ===
            // It MUST be the same one you use in AgrifyDbContext.cs
            string connectionString = @"Server=.\SQLEXPRESS;Database=agrifyDB;Trusted_Connection=True;Encrypt=False;";

            optionsBuilder.UseSqlServer(connectionString);

            return new AgrifyDbContext(optionsBuilder.Options);
        }
    }
}
