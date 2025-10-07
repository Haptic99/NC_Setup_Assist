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
    }
}