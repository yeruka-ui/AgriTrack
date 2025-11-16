using agrify.Models;
using Microsoft.EntityFrameworkCore;
using Windows.System;

namespace agrify.Data
{
    public class AgrifyDbContext : DbContext
    {

        public AgrifyDbContext(DbContextOptions<AgrifyDbContext> options) : base(options)
        {
        }

        public AgrifyDbContext() { }
        // Your login table
        public DbSet<agrify.Models.User> Users { get; set; }

        // Your teammates' tables
        public DbSet<agrify.Models.Livestock> Livestock { get; set; }

        public DbSet<Produce> Produce { get; set; }

        public DbSet<Holiday> Holidays { get; set; }

        public DbSet<CalendarTask> CalendarTasks { get; set; }
        public DbSet<agrify.Models.InventoryItem> InventoryItems { get; set; }
        // public DbSet<Produce> Produce { get; set; }
        // ...and so on!

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // This connection string stays the same because
            // all the tables are in the ONE database.
            string connectionString = @"Server=.\SQLEXPRESS;Database=agrifyDB;Trusted_Connection=True;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}
