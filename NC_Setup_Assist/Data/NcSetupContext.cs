// Data/NcSetupContext.cs
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Models;
using System.IO;
using System;

namespace NC_Setup_Assist.Data
{
    public class NcSetupContext : DbContext
    {
        public DbSet<Werkzeug> Werkzeuge { get; set; } = null!;
        public DbSet<WerkzeugKategorie> WerkzeugKategorien { get; set; } = null!;
        public DbSet<WerkzeugUnterkategorie> WerkzeugUnterkategorien { get; set; } = null!;
        public DbSet<Projekt> Projekte { get; set; } = null!;
        public DbSet<NCProgramm> NCProgramme { get; set; } = null!;
        public DbSet<WerkzeugEinsatz> WerkzeugEinsaetze { get; set; } = null!;
        public DbSet<Maschine> Maschinen { get; set; } = null!;
        public DbSet<Standort> Standorte { get; set; } = null!;
        public DbSet<Hersteller> Hersteller { get; set; } = null!;
        public DbSet<StandardWerkzeugZuweisung> StandardWerkzeugZuweisungen { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Stellt sicher, dass die Datenbank im Anwendungsordner gespeichert wird
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nc_setup.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Hier könnten in Zukunft noch weitere Model-Konfigurationen
            // (z.B. Unique Constraints) vorgenommen werden.
        }
    }
}