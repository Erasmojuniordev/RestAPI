using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Models;

namespace RestauranteAPI.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Comanda> Comandas => Set<Comanda>();
    public DbSet<Item> Itens => Set<Item>();
    public DbSet<ItemComanda> ItensComanda => Set<ItemComanda>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Comanda
        builder.Entity<Comanda>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.PrecoTotal).HasPrecision(10, 2);
            e.Property(c => c.Status).HasConversion<string>(); // salva enum como string legível
            e.HasMany(c => c.Itens)
             .WithOne(i => i.Comanda)
             .HasForeignKey(i => i.ComandaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Item
        builder.Entity<Item>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Preco).HasPrecision(10, 2);
            e.Property(i => i.Nome).HasMaxLength(100).IsRequired();
        });

        // ItemComanda
        builder.Entity<ItemComanda>(e =>
        {
            e.HasKey(ic => ic.Id);
            e.Property(ic => ic.PrecoUnitario).HasPrecision(10, 2);

            // PrecoTotal é calculado — ignorado pelo EF
            e.Ignore(ic => ic.PrecoTotal);

            e.HasOne(ic => ic.Item)
             .WithMany(i => i.ItensComanda)
             .HasForeignKey(ic => ic.ItemId)
             .OnDelete(DeleteBehavior.Restrict); // não deleta item se tiver em comanda
        });
    }
}
