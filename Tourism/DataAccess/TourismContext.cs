using Microsoft.EntityFrameworkCore;
using Tourism.Models;

namespace Tourism.DataAccess
{
    public class TourismContext : DbContext
    {
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }

        public TourismContext(DbContextOptions<TourismContext> options) : base(options) { }
    }
}
