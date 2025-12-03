using Microsoft.EntityFrameworkCore;

namespace TicketApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Burada DbSet'lerinizi tanımlayacaksınız
    // Örnek: public DbSet<Ticket> Tickets { get; set; }
}