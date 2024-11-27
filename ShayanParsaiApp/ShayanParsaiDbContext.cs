using Microsoft.EntityFrameworkCore;

namespace ShayanParsaiApp
{
    public class ShayanParsaiDbContext : DbContext
    {
        public DbSet<Measurement> Measurements { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Använd SQL Server och anslut till en lokal databas
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=ShayanParsaiDb;Trusted_Connection=True;");
        }

        // Säkerställ att databasen migreras till senaste versionen
        public void EnsureDatabaseMigrated()
        {
            Database.Migrate();
        }
    }
}