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
    public DbSet<TarifniModel> TarifniModeli { get; set; }
    public DbSet<Racun> Racuni { get; set; }

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
            entity.Property(pb => pb.LimitVrednost)
                  .HasColumnType("decimal(18,2)");
            entity.Property(pb => pb.PocetnoStanjeMeseca)
                  .HasColumnType("decimal(18,4)");

            entity.HasOne(pb => pb.Objekat)
                  .WithMany(o => o.PametnaBrojila)
                  .HasForeignKey(pb => pb.ObjekatId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TarifniModel>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.CenaZ_VT).HasColumnType("decimal(18,4)");
            entity.Property(t => t.CenaZ_NT).HasColumnType("decimal(18,4)");
            entity.Property(t => t.CenaP_VT).HasColumnType("decimal(18,4)");
            entity.Property(t => t.CenaP_NT).HasColumnType("decimal(18,4)");
            entity.Property(t => t.CenaC_VT).HasColumnType("decimal(18,4)");
            entity.Property(t => t.CenaC_NT).HasColumnType("decimal(18,4)");
            entity.Property(t => t.CenaObracunskeSnage).HasColumnType("decimal(18,4)");
            entity.Property(t => t.TrosakSnabdevaca).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Racun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.EnergijaVT).HasColumnType("decimal(18,4)");
            entity.Property(r => r.EnergijaNT).HasColumnType("decimal(18,4)");
            entity.Property(r => r.IznosZelena).HasColumnType("decimal(18,2)");
            entity.Property(r => r.IznosPlava).HasColumnType("decimal(18,2)");
            entity.Property(r => r.IznosCrvena).HasColumnType("decimal(18,2)");
            entity.Property(r => r.FiksniTroskovi).HasColumnType("decimal(18,2)");
            entity.Property(r => r.UkupanIznos).HasColumnType("decimal(18,2)");

            entity.HasOne(r => r.Brojilo)
                  .WithMany()
                  .HasForeignKey(r => r.BrojiloId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Korisnik)
                  .WithMany()
                  .HasForeignKey(r => r.KorisnikId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}