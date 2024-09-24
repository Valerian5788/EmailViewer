using Microsoft.EntityFrameworkCore;
using EmailViewer.Models;

namespace EmailViewer.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("YOUR_CONNECTION_STRING_HERE");
        }
    }
}