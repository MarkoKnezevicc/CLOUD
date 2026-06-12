using Microsoft.EntityFrameworkCore;

public class SmartMeteringDbContext : DbContext
{
    public SmartMeteringDbContext(DbContextOptions<SmartMeteringDbContext> options)
        : base(options)
    {
    }

    public DbSet<Korisnik> Korisnici { get; set; }
    public DbSet<Objekat> Objekti { get; set; }
    public DbSet<PametnoBrojilo> PametnaBrojila { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Korisnik>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => k.Email).IsUnique();
            entity.Property(k => k.Uloga).IsRequired();
        });

        modelBuilder.Entity<Objekat>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasOne(o => o.Korisnik)
                  .WithMany(k => k.Objekti)
                  .HasForeignKey(o => o.KorisnikId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PametnoBrojilo>(entity =>
        {
            entity.HasKey(pb => pb.Id);
            entity.HasIndex(pb => pb.SerijskiBroj).IsUnique();

           
            entity.HasIndex(pb => pb.Uuid)
                  .IsUnique()
                  .HasFilter("[Uuid] IS NOT NULL");

            entity.Property(pb => pb.MaksimalnaOdobrenaSnaga)
                  .HasColumnType("decimal(18,2)");

            entity.HasOne(pb => pb.Objekat)
                  .WithMany(o => o.PametnaBrojila)
                  .HasForeignKey(pb => pb.ObjekatId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}