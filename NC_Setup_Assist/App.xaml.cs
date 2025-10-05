using NC_Setup_Assist.Data;
using NC_Setup_Assist.Models;
using NC_Setup_Assist.ViewModels;
using NC_Setup_Assist.Views;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace NC_Setup_Assist
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // NEU: Datenbank beim Start initialisieren und füllen
            InitializeDatabase();

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            mainWindow.Show();
        }

        private void InitializeDatabase()
        {
            // Erstelle eine Instanz des DbContext
            using (var context = new NcSetupContext())
            {
                // Stelle sicher, dass die Datenbank existiert
                context.Database.Migrate();

                // Fülle die Kategorien, falls sie noch leer sind
                if (context.WerkzeugKategorien.Any() == false)
                {
                    // 1. Hauptkategorien erstellen
                    var fraeserKat = new WerkzeugKategorie { Name = "Fräser" };
                    var bohrerKat = new WerkzeugKategorie { Name = "Bohrer" };
                    var drehKat = new WerkzeugKategorie { Name = "Drehwerkzeug" };
                    context.WerkzeugKategorien.AddRange(fraeserKat, bohrerKat, drehKat);
                    context.SaveChanges();

                    // 2. Unterkategorien für "Fräser"
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Schaftfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "T-Nuten-Fräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Winkelfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Messerköpfe", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Scheibenfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Gewindefräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Gravierfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Kugelfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Radiusfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Hartfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Vollradiusfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Fasenfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Formfräser", Kategorie = fraeserKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Sonstiges", Kategorie = fraeserKat });
                    // ... fügen Sie hier bei Bedarf weitere Fräser hinzu ...

                    // 3. Unterkategorien für "Bohrer"
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "HSS-Bohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "VHM-Bohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Wendeplattenbohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "NC-Anbohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Zentrierbohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Stufenbohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Tieflochbohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Reibahle", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Senker", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Gewindebohrer", Kategorie = bohrerKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Sonstiges", Kategorie = bohrerKat });
                    // ... fügen Sie hier bei Bedarf weitere Bohrer hinzu ...

                    // 4. Unterkategorien für "Drehwerkzeuge"
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Aussen SR", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Aussen SR Links", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Innen SR", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Innen SR Links", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Abstechstahl", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Abstechschwert", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Gewindedrehstahl", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Radiusdrehstahl", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Profildrehstahl", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Gewindedrehstahl Aussen", Kategorie = drehKat });
                    context.WerkzeugUnterkategorien.Add(new WerkzeugUnterkategorie { Name = "Gewindedrehstahl Innen", Kategorie = drehKat });
                    // ... fügen Sie hier bei Bedarf weitere Drehwerkzeuge hinzu ...

                    context.SaveChanges();
                }

                // Fülle die Werkzeuge, falls sie noch leer sind
                if (context.Werkzeuge.Any() == false)
                {
                    var werkzeuge = new List<Werkzeug>();

                    // === FRÄSER ===
                    var ukSchaft = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Schaftfräser");
                    var ukKugel = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Kugelfräser");
                    var ukRadius = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Radiusfräser");
                    var ukMesser = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Messerköpfe");
                    var ukGewindeF = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Gewindefräser");
                    var ukFasen = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Fasenfräser");

                    // Schaftfräser generieren
                    double[] schaftDia = { 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 20 };
                    foreach (var d in schaftDia)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"Schaftfräser VHM D{d}", Unterkategorie = ukSchaft });
                        werkzeuge.Add(new Werkzeug { Name = $"Schaftfräser HSS D{d}", Unterkategorie = ukSchaft });
                    }

                    // Kugelfräser generieren
                    double[] kugelDia = { 2, 3, 4, 6, 8, 10, 12, 16 };
                    foreach (var d in kugelDia)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"Kugelfräser VHM R{d / 2}", Unterkategorie = ukKugel });
                    }

                    // Radiusfräser generieren
                    werkzeuge.Add(new Werkzeug { Name = "Radiusfräser D8 R0.5", Unterkategorie = ukRadius });
                    werkzeuge.Add(new Werkzeug { Name = "Radiusfräser D10 R1", Unterkategorie = ukRadius });
                    werkzeuge.Add(new Werkzeug { Name = "Radiusfräser D12 R1", Unterkategorie = ukRadius });
                    werkzeuge.Add(new Werkzeug { Name = "Radiusfräser D12 R2", Unterkategorie = ukRadius });


                    // Messerköpfe
                    werkzeuge.Add(new Werkzeug { Name = "Messerkopf D40", Unterkategorie = ukMesser, Beschreibung = "5 Schneiden" });
                    werkzeuge.Add(new Werkzeug { Name = "Messerkopf D50", Unterkategorie = ukMesser, Beschreibung = "6 Schneiden" });
                    werkzeuge.Add(new Werkzeug { Name = "Messerkopf D63", Unterkategorie = ukMesser, Beschreibung = "7 Schneiden" });
                    werkzeuge.Add(new Werkzeug { Name = "Messerkopf D80", Unterkategorie = ukMesser, Beschreibung = "8 Schneiden" });

                    // Gewindefräser
                    string[] gewindeM = { "M3", "M4", "M5", "M6", "M8", "M10", "M12", "M16" };
                    foreach (var g in gewindeM)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"Gewindefräser {g}", Unterkategorie = ukGewindeF });
                    }

                    // Fasenfräser
                    werkzeuge.Add(new Werkzeug { Name = "Fasenfräser 90° D10", Unterkategorie = ukFasen });
                    werkzeuge.Add(new Werkzeug { Name = "Fasenfräser 60° D12", Unterkategorie = ukFasen });

                    // === BOHRER ===
                    var ukHss = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "HSS-Bohrer");
                    var ukVhm = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "VHM-Bohrer");
                    var ukNc = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "NC-Anbohrer");
                    var ukGewindeB = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Gewindebohrer");
                    var ukReib = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Reibahle");
                    var ukSenk = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Senker");

                    // HSS Bohrer generieren (0.5mm Schritte)
                    for (double d = 1.0; d <= 13.0; d += 0.5)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"HSS Bohrer D{d:F1}", Unterkategorie = ukHss });
                    }
                    // Kernlochbohrer HSS
                    werkzeuge.Add(new Werkzeug { Name = "HSS Bohrer D3.3 (M4)", Unterkategorie = ukHss });
                    werkzeuge.Add(new Werkzeug { Name = "HSS Bohrer D4.2 (M5)", Unterkategorie = ukHss });
                    werkzeuge.Add(new Werkzeug { Name = "HSS Bohrer D6.8 (M8)", Unterkategorie = ukHss });
                    werkzeuge.Add(new Werkzeug { Name = "HSS Bohrer D8.5 (M10)", Unterkategorie = ukHss });
                    werkzeuge.Add(new Werkzeug { Name = "HSS Bohrer D10.2 (M12)", Unterkategorie = ukHss });

                    // VHM Bohrer generieren
                    double[] vhmDia = { 3, 4, 5, 6, 8, 10, 12, 14, 16 };
                    foreach (var d in vhmDia)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"VHM Bohrer D{d}", Unterkategorie = ukVhm });
                        werkzeuge.Add(new Werkzeug { Name = $"VHM Bohrer D{d} IK", Unterkategorie = ukVhm, Beschreibung = "Mit Innenkühlung" });
                    }

                    // NC Anbohrer
                    werkzeuge.Add(new Werkzeug { Name = "NC Anbohrer D6 90°", Unterkategorie = ukNc });
                    werkzeuge.Add(new Werkzeug { Name = "NC Anbohrer D8 90°", Unterkategorie = ukNc });
                    werkzeuge.Add(new Werkzeug { Name = "NC Anbohrer D10 90°", Unterkategorie = ukNc });
                    werkzeuge.Add(new Werkzeug { Name = "NC Anbohrer D12 120°", Unterkategorie = ukNc });

                    // Gewindebohrer
                    foreach (var g in gewindeM)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"Gewindebohrer {g}", Unterkategorie = ukGewindeB });
                    }

                    // Reibahlen
                    double[] reibDia = { 4, 5, 6, 8, 10, 12, 15, 16 };
                    foreach (var d in reibDia)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"Reibahle D{d} H7", Unterkategorie = ukReib });
                    }

                    // Senker
                    werkzeuge.Add(new Werkzeug { Name = "Kegelsenker 90° D16", Unterkategorie = ukSenk });
                    werkzeuge.Add(new Werkzeug { Name = "Kegelsenker 90° D20", Unterkategorie = ukSenk });

                    // === DREHWERKZEUGE ===
                    var ukAussen = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Aussen SR");
                    var ukInnen = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Innen SR");
                    var ukAbstech = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Abstechstahl");
                    var ukGewindeDA = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Gewindedrehstahl Aussen");
                    var ukGewindeDI = context.WerkzeugUnterkategorien.Single(uk => uk.Name == "Gewindedrehstahl Innen");

                    // Aussenbearbeitung
                    string[] wendeAussen = { "CNMG", "DNMG", "WNMG", "VNMG" };
                    string[] radien = { "04", "08", "12" };
                    foreach (var platte in wendeAussen)
                    {
                        foreach (var radius in radien)
                        {
                            werkzeuge.Add(new Werkzeug { Name = $"Aussenstahl {platte} 1204{radius}", Unterkategorie = ukAussen });
                        }
                    }

                    // Innenbearbeitung
                    string[] wendeInnen = { "CCMT", "DCMT" };
                    foreach (var platte in wendeInnen)
                    {
                        foreach (var radius in radien.Take(2)) // 04, 08
                        {
                            werkzeuge.Add(new Werkzeug { Name = $"Innenstahl {platte} 09T3{radius}", Unterkategorie = ukInnen });
                        }
                    }

                    // Abstechstähle
                    werkzeuge.Add(new Werkzeug { Name = "Abstecher 2mm", Unterkategorie = ukAbstech });
                    werkzeuge.Add(new Werkzeug { Name = "Abstecher 3mm", Unterkategorie = ukAbstech });
                    werkzeuge.Add(new Werkzeug { Name = "Abstecher 4mm", Unterkategorie = ukAbstech });

                    // Gewindedrehstähle
                    double[] steigung = { 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0 };
                    foreach (var s in steigung)
                    {
                        werkzeuge.Add(new Werkzeug { Name = $"Gew.-Stahl Aussen 60° P{s:F2}", Unterkategorie = ukGewindeDA });
                        werkzeuge.Add(new Werkzeug { Name = $"Gew.-Stahl Innen 60° P{s:F2}", Unterkategorie = ukGewindeDI });
                    }


                    // Alle generierten Werkzeuge zur Datenbank hinzufügen
                    context.Werkzeuge.AddRange(werkzeuge);
                    context.SaveChanges();
                }


                // Fülle die Hersteller, falls sie noch leer sind
                if (context.Hersteller.Any() == false)
                {
                    var hersteller = new List<Hersteller>
                    {
                        new Hersteller { Name = "Okuma" },
                        new Hersteller { Name = "DMT" },
                        new Hersteller { Name = "Mazak" },
                        new Hersteller { Name = "DMG Mori" }
                    };
                    context.Hersteller.AddRange(hersteller);
                    context.SaveChanges();
                }

                // Fülle die Firma/Standorte/Maschinen, falls sie noch leer sind
                if (context.Firmen.Any() == false)
                {
                    // 1. Firma erstellen
                    var firma = new Firma { Name = "Musterfirma GmbH" };
                    context.Firmen.Add(firma);

                    // 2. Standorte erstellen
                    var standort1 = new Standort { Name = "STB Maschinenbau AG", PLZ = "9032", Stadt = "Engelburg", Strasse = "Breitschachenstrasse", Hausnummer = "56", ZugehoerigeFirma = firma };
                    var standort2 = new Standort { Name = "Gebrüder Egli Maschinen AG", PLZ = "9512", Stadt = "Rossrüti", Strasse = "Konstanzerstrasse", Hausnummer = "14", ZugehoerigeFirma = firma };
                    context.Standorte.AddRange(standort1, standort2);

                    // 3. Maschinen erstellen und zuweisen
                    var okumaHersteller = context.Hersteller.SingleOrDefault(h => h.Name == "Okuma");
                    var dmtHersteller = context.Hersteller.SingleOrDefault(h => h.Name == "DMT");

                    var maschine1 = new Maschine { Name = "Okuma ES-L8", Hersteller = okumaHersteller, ZugehoerigerStandort = standort1 };
                    var maschine2 = new Maschine { Name = "Okuma LU-15", Hersteller = okumaHersteller, ZugehoerigerStandort = standort1 };
                    var maschine3 = new Maschine { Name = "Kern", Hersteller = dmtHersteller, ZugehoerigerStandort = standort2 };
                    context.Maschinen.AddRange(maschine1, maschine2, maschine3);

                    context.SaveChanges();
                }
            }
        }
    }
}