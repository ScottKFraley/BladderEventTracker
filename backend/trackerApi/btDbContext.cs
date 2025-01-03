using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext 
{
    protectedoverridevoidOnConfiguring(DbContextOptionsBuilder optionsBuilder) 
    {
        optionsBuilder.UseNpgsql("Host=localhost;Database=myappdb;Username=admin;Password=password");
    }

    public DbSet<User> Users { get; set; }

    public classUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
