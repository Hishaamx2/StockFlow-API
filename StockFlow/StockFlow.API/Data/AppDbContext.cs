using Microsoft.EntityFrameworkCore;
using StockFlow.API.Models;

namespace StockFlow.API.Data;

//Defines what the API knows when trying to talk tyo the database, as in which tables and columns

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) // <-- EF Core DBContext base class (How To)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();  //there is a warehouse and there is an items table
    public DbSet<Item> Items => Set<Item>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>()
            .HasIndex(i => i.Sku)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}
