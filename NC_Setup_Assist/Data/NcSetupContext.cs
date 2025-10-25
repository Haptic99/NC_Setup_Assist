using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace NC_Setup_Assist.Data
{
    public class NcSetupContext : DbContext
    {
        // ... (Alle Ihre DbSet-Eigenschaften bleiben unverändert)
        public DbSet<Standort> Standorte { get; set; }
        public DbSet<Maschine> Maschinen { get; set; }
        public DbSet<Werkzeug> Werkzeuge { get; set; }
        public DbSet<StandardWerkzeugZuweisung> StandardWerkzeugZuweisungen { get; set; }
        public DbSet<Projekt> Projekte { get; set; }
        public DbSet<NCProgramm> NCProgramme { get; set; }
        public DbSet<WerkzeugEinsatz> WerkzeugEinsaetze { get; set; }
        public DbSet<WerkzeugKategorie> WerkzeugKategorien { get; set; }
        public DbSet<WerkzeugUnterkategorie> WerkzeugUnterkategorien { get; set; }
        public DbSet<Hersteller> Hersteller { get; set; }


        // Diese Methode konfiguriert die Verbindung zur Datenbank.
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 1. Finde den "%AppData%/Roaming"-Ordner des aktuellen Benutzers
            string appDataRoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // 2. Erstelle einen Unterordner für unsere Anwendung, falls er nicht existiert
            string appDataPath = Path.Combine(appDataRoamingPath, "NC-Setup-Assist");
            Directory.CreateDirectory(appDataPath);

            // 3. Kombiniere den Pfad zur Datenbankdatei
            string dbPath = Path.Combine(appDataPath, "nc_setup.db");

            Debug.WriteLine($"[DbContext] Using Database at: {dbPath}");

            // 4. Verwende diesen festen, professionellen Pfad
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        // --- NEUE METHODE FÜR DAS DATA SEEDING ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. WerkzeugKategorien (Basierend auf image_18a09a.png)
            modelBuilder.Entity<WerkzeugKategorie>().HasData(
                new WerkzeugKategorie { WerkzeugKategorieID = 1, Name = "Fräser" },
                new WerkzeugKategorie { WerkzeugKategorieID = 2, Name = "Bohrer" },
                new WerkzeugKategorie { WerkzeugKategorieID = 3, Name = "Drehwerkzeug" }
            );

            // 2. WerkzeugUnterkategorien
            modelBuilder.Entity<WerkzeugUnterkategorie>().HasData(
                new WerkzeugUnterkategorie
                {
                    WerkzeugUnterkategorieID = 1,
                    Name = "Gewindedrehstahl Aussen",
                    WerkzeugKategorieID = 3,
                    BenötigtPlattenwinkel = false,
                    BenötigtSteigung = true
                },
                new WerkzeugUnterkategorie
                {
                    WerkzeugUnterkategorieID = 2,
                    Name = "Gewindedrehstahl Innen",
                    WerkzeugKategorieID = 3,
                    BenötigtPlattenwinkel = false,
                    BenötigtSteigung = true
                },
                new WerkzeugUnterkategorie
                {
                    WerkzeugUnterkategorieID = 3,
                    Name = "Messerst.",
                    WerkzeugKategorieID = 3,
                    BenötigtPlattenwinkel = true,
                    BenötigtSteigung = false
                },
                new WerkzeugUnterkategorie
                {
                    WerkzeugUnterkategorieID = 4,
                    Name = "Abstechstähle",
                    WerkzeugKategorieID = 3,
                    BenötigtPlattenwinkel = false,
                    BenötigtSteigung = false
                },
                new WerkzeugUnterkategorie
                {
                    WerkzeugUnterkategorieID = 5,
                    Name = "Kugelfräser",
                    WerkzeugKategorieID = 1,
                    BenötigtPlattenwinkel = false,
                    BenötigtSteigung = false
                }
            );

            // 3. Werkzeuge (Basierend auf image_18a47f.png, mit angepassten Unterkategorie-IDs)
            modelBuilder.Entity<Werkzeug>().HasData(
                new Werkzeug
                {
                    WerkzeugID = 1,
                    Name = "Gewindedrehstahl Aussen P=0.75",
                    Steigung = 0.75,
                    Plattenwinkel = null,
                    WerkzeugUnterkategorieID = 1 // Alt: 3
                },
                new Werkzeug
                {
                    WerkzeugID = 2,
                    Name = "Messerst. 80°",
                    Steigung = null,
                    Plattenwinkel = 80.0,
                    WerkzeugUnterkategorieID = 3 // Alt: 38
                },
                new Werkzeug
                {
                    WerkzeugID = 3,
                    Name = "Messerst. 35° gekr.",
                    Steigung = null,
                    Plattenwinkel = 35.0,
                    WerkzeugUnterkategorieID = 3 // Alt: 38
                },
                new Werkzeug
                {
                    WerkzeugID = 4,
                    Name = "Abstechst. B=3mm", // Name aus Bild übernommen (ggf. "Abstech" korrigieren)
                    Steigung = null,
                    Plattenwinkel = null,
                    WerkzeugUnterkategorieID = 4 // Alt: 41
                },
                new Werkzeug
                {
                    WerkzeugID = 5,
                    Name = "Gravurstichel Ø1, R0.5",
                    Steigung = null,
                    Plattenwinkel = null,
                    WerkzeugUnterkategorieID = 5
                }
            );
        }
        // --- ENDE NEUE METHODE ---
    }
}