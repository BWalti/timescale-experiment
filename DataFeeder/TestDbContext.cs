using Microsoft.EntityFrameworkCore;

namespace DataFeeder;

public class TestDbContext : DbContext
{
    public DbSet<CountryTempAverage> Averages { get; set; }

    // docker run -d --name timescaledb -p 5432:5432 -e POSTGRES_PASSWORD=demo -e POSTGRES_DB=demo -e POSTGRES_USER=demo timescale/timescaledb:latest-pg13
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Database=demo;Username=demo;Password=demo");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CountryTempAverage>()
            .HasKey(nameof(CountryTempAverage.Date), nameof(CountryTempAverage.Country));
    }
}