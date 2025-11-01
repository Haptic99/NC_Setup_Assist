// Data/DataSeeder.cs
using Microsoft.EntityFrameworkCore;
using NC_Setup_Assist.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NC_Setup_Assist.Data
{
    public static class DataSeeder
    {
        public static void Initialize(NcSetupContext context)
        {
            // --- Schritt 1: Prüfen, ob die Datenbank bereits befüllt ist ---
            // Wenn schon Kategorien vorhanden sind, brechen wir ab.
            if (context.WerkzeugKategorien.Any())
            {
                return;
            }

            // --- Schritt 2: Kategorien erstellen ---
            var katDreh = new WerkzeugKategorie { Name = "Drehwerkzeuge" };
            var katFraes = new WerkzeugKategorie { Name = "Fräswerkzeuge" };
            var katBohr = new WerkzeugKategorie { Name = "Bohrwerkzeuge" };
            var katGewinde = new WerkzeugKategorie { Name = "Gewindewerkzeuge" };
            var katReib = new WerkzeugKategorie { Name = "Reib-/Senkwerkzeuge" }; // Name aus Tabelle2

            context.WerkzeugKategorien.AddRange(katDreh, katFraes, katBohr, katGewinde, katReib);


            // --- Schritt 3: Unterkategorien erstellen (basierend auf Tabelle1) ---
            // (Wir mappen die Parameter-Spalten auf die "Benötigt..."-Flags)
            var subAusDreh = new WerkzeugUnterkategorie { Kategorie = katDreh, Name = "Aussendrehstahl", BenötigtRadius = true };
            var subInnDreh = new WerkzeugUnterkategorie { Kategorie = katDreh, Name = "Innendrehstahl", BenötigtRadius = true };
            var subEinstechA = new WerkzeugUnterkategorie { Kategorie = katDreh, Name = "Einstechstahl Aussen", BenötigtBreite = true, BenötigtMaxStechtiefe = true };
            var subEinstechI = new WerkzeugUnterkategorie { Kategorie = katDreh, Name = "Einstechstahl Innen", BenötigtBreite = true, BenötigtMaxStechtiefe = true };
            var subGewindeDrehA = new WerkzeugUnterkategorie { Kategorie = katDreh, Name = "Gewindedrehstahl Aussen", BenötigtSteigung = true };
            var subGewindeDrehI = new WerkzeugUnterkategorie { Kategorie = katDreh, Name = "Gewindedrehstahl Innen", BenötigtSteigung = true };
            var subAbstech = new WerkzeugUnterkategorie { Kategorie = katDreh, Name = "Abstechstahl", BenötigtBreite = true, BenötigtMaxStechtiefe = true };

            var subSchaft = new WerkzeugUnterkategorie { Kategorie = katFraes, Name = "Schaftfräser", BenötigtDurchmesser = true };
            var subKugel = new WerkzeugUnterkategorie { Kategorie = katFraes, Name = "Kugelfräser", BenötigtDurchmesser = true };
            var subEckradius = new WerkzeugUnterkategorie { Kategorie = katFraes, Name = "Eckradiusfräser", BenötigtDurchmesser = true, BenötigtRadius = true };
            var subMesser = new WerkzeugUnterkategorie { Kategorie = katFraes, Name = "Messerkopf", BenötigtDurchmesser = true };
            var subScheiben = new WerkzeugUnterkategorie { Kategorie = katFraes, Name = "Scheibenfräser", BenötigtDurchmesser = true, BenötigtBreite = true };
            var subFasen = new WerkzeugUnterkategorie { Kategorie = katFraes, Name = "Fasenfräser", BenötigtDurchmesser = true, BenötigtSpitzenwinkel = true };
            var subTNuten = new WerkzeugUnterkategorie { Kategorie = katFraes, Name = "T-Nutenfräser", BenötigtDurchmesser = true, BenötigtBreite = true }; // T1=Profil, T2=D/B. T2 ist spezifischer.

            var subSpiralHSS = new WerkzeugUnterkategorie { Kategorie = katBohr, Name = "Spiralbohrer (HSS)", BenötigtDurchmesser = true, BenötigtSpitzenwinkel = true };
            var subSpiralVHM = new WerkzeugUnterkategorie { Kategorie = katBohr, Name = "Vollhartmetallbohrer (VHM)", BenötigtDurchmesser = true, BenötigtSpitzenwinkel = true };
            var subWendeBohr = new WerkzeugUnterkategorie { Kategorie = katBohr, Name = "Wendeplattenbohrer", BenötigtDurchmesser = true };
            var subZentrier = new WerkzeugUnterkategorie { Kategorie = katBohr, Name = "Zentrierbohrer", BenötigtDurchmesser = true }; // T1=Nenndurchmesser, T2=Durchmesser.
            var subNCAnbohr = new WerkzeugUnterkategorie { Kategorie = katBohr, Name = "NC-Anbohrer", BenötigtDurchmesser = true, BenötigtSpitzenwinkel = true };

            var subGewindeBohr = new WerkzeugUnterkategorie { Kategorie = katGewinde, Name = "Gewindebohrer", BenötigtSteigung = true };
            var subGewindeForm = new WerkzeugUnterkategorie { Kategorie = katGewinde, Name = "Gewindeformer", BenötigtSteigung = true };
            var subGewindeFraes = new WerkzeugUnterkategorie { Kategorie = katGewinde, Name = "Gewindefräser", BenötigtSteigung = true, BenötigtDurchmesser = true };

            var subReibahle = new WerkzeugUnterkategorie { Kategorie = katReib, Name = "Maschinenreibahle", BenötigtDurchmesser = true };
            var subKegelsenker = new WerkzeugUnterkategorie { Kategorie = katReib, Name = "Kegelsenker", BenötigtDurchmesser = true, BenötigtSpitzenwinkel = true };

            context.WerkzeugUnterkategorien.AddRange(
                subAusDreh, subInnDreh, subEinstechA, subEinstechI, subGewindeDrehA, subGewindeDrehI, subAbstech,
                subSchaft, subKugel, subEckradius, subMesser, subScheiben, subFasen, subTNuten,
                subSpiralHSS, subSpiralVHM, subWendeBohr, subZentrier, subNCAnbohr,
                subGewindeBohr, subGewindeForm, subGewindeFraes,
                subReibahle, subKegelsenker
            );


            // --- Schritt 4: Standard-Werkzeuge erstellen (basierend auf Tabelle2) ---
            var werkzeuge = new List<Werkzeug>();

            // Hilfsfunktion zum Parsen der "1.0...10.0 (0.1er Schritte)"-Strings
            static IEnumerable<double> ParseRange(string rangeStr)
            {
                var list = new List<double>();
                if (rangeStr.Contains("..."))
                {
                    var parts = rangeStr.Split(new[] { "...", " (0.1er Schritte)" }, StringSplitOptions.RemoveEmptyEntries);
                    double start = double.Parse(parts[0]);
                    double end = double.Parse(parts[1]);
                    for (double d = start; d <= end; d += 0.1)
                    {
                        list.Add(Math.Round(d, 1));
                    }
                }
                return list;
            }

            // Hilfsfunktion zum Parsen der Array-Strings "[0.2, 0.4, ...]"
            static IEnumerable<double> ParseArray(string arrayStr)
            {
                return arrayStr.Trim('[', ']', ' ')
                               .Split(',')
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrEmpty(s))
                               .Select(double.Parse);
            }


            // Drehwerkzeuge
            foreach (var r in ParseArray("[0.2, 0.4, 0.8, 1.2, 1.6]"))
                werkzeuge.Add(new Werkzeug { Name = $"Aussendrehstahl R{r}", Unterkategorie = subAusDreh, Radius = r });

            foreach (var r in ParseArray("[0.2, 0.4, 0.8, 1.2, 1.6]"))
                werkzeuge.Add(new Werkzeug { Name = $"Innendrehstahl R{r}", Unterkategorie = subInnDreh, Radius = r });

            foreach (var b in ParseArray("[1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0, 6.0]"))
                foreach (var t in ParseArray("[3.0, 5.0, 8.0, 10.0, 12.0, 15.0]"))
                    werkzeuge.Add(new Werkzeug { Name = $"Einstechstahl Aussen B{b} Tmax{t}", Unterkategorie = subEinstechA, Breite = b, MaxStechtiefe = t });

            foreach (var b in ParseArray("[1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0]"))
                foreach (var t in ParseArray("[3.0, 5.0, 8.0, 10.0, 12.0]"))
                    werkzeuge.Add(new Werkzeug { Name = $"Einstechstahl Innen B{b} Tmax{t}", Unterkategorie = subEinstechI, Breite = b, MaxStechtiefe = t });

            foreach (var s in ParseArray("[0.5, 0.75, 0.8, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Gewindedrehstahl Aussen S{s}", Unterkategorie = subGewindeDrehA, Steigung = s });

            foreach (var s in ParseArray("[0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Gewindedrehstahl Innen S{s}", Unterkategorie = subGewindeDrehI, Steigung = s });

            foreach (var b in ParseArray("[2.0, 2.5, 3.0, 4.0, 5.0, 6.0]"))
                foreach (var t in ParseArray("[15.0, 20.0, 25.0, 30.0, 40.0, 50.0]"))
                    werkzeuge.Add(new Werkzeug { Name = $"Abstechstahl B{b} Tmax{t}", Unterkategorie = subAbstech, Breite = b, MaxStechtiefe = t });

            // Fräswerkzeuge
            foreach (var d in ParseArray("[1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 16.0, 20.0, 25.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Schaftfräser D{d}", Unterkategorie = subSchaft, Durchmesser = d });

            foreach (var d in ParseArray("[1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 16.0, 20.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Kugelfräser D{d}", Unterkategorie = subKugel, Durchmesser = d });

            foreach (var d in ParseArray("[4.0, 6.0, 8.0, 10.0, 12.0, 16.0, 20.0, 25.0]"))
                foreach (var r in ParseArray("[0.2, 0.5, 0.8, 1.0, 1.5, 2.0, 3.0]"))
                    werkzeuge.Add(new Werkzeug { Name = $"Eckradiusfräser D{d} R{r}", Unterkategorie = subEckradius, Durchmesser = d, Radius = r });

            foreach (var d in ParseArray("[40.0, 50.0, 63.0, 80.0, 100.0, 125.0, 160.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Messerkopf D{d}", Unterkategorie = subMesser, Durchmesser = d });

            // Scheibenfräser: In T2 steht "Breite" als P1, D wird nicht erwähnt. T1 sagt "Durchmesser, Breite".
            // Ich erstelle Werkzeuge pro Breite und nehme an D ist irrelevant für die Standard-Definition.
            foreach (var b in ParseArray("[2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 16.0, 32.0, 64.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Scheibenfräser B{b}", Unterkategorie = subScheiben, Breite = b });

            foreach (var d in ParseArray("[4.0, 6.0, 8.0, 10.0, 12.0, 16.0]"))
                foreach (var w in ParseArray("[60, 90, 120]"))
                    werkzeuge.Add(new Werkzeug { Name = $"Fasenfräser D{d} W{w}°", Unterkategorie = subFasen, Durchmesser = d, Spitzenwinkel = w });

            foreach (var d in ParseArray("[6.0, 8.0, 10.0, 12.0, 16.0, 32.0, 64.0]"))
                foreach (var b in ParseArray("[1, 2, 3]"))
                    werkzeuge.Add(new Werkzeug { Name = $"T-Nutenfräser D{d} B{b}", Unterkategorie = subTNuten, Durchmesser = d, Breite = b });

            // Bohrwerkzeuge
            var diaHSS = ParseRange("1.0...10.0 (0.1er Schritte)").ToList();
            diaHSS.AddRange(ParseArray("[10.2, 11.0, 12.0, 16.0, 20.0, 25.0]"));
            foreach (var d in diaHSS)
                werkzeuge.Add(new Werkzeug { Name = $"Spiralbohrer (HSS) D{d}", Unterkategorie = subSpiralHSS, Durchmesser = d, Spitzenwinkel = 118 });

            var diaVHM = ParseRange("1.0...10.0 (0.1er Schritte)").ToList();
            diaVHM.AddRange(ParseArray("[10.2, 11.0, 12.0, 16.0, 20.0, 25.0]"));
            foreach (var d in diaVHM)
                werkzeuge.Add(new Werkzeug { Name = $"Vollhartmetallbohrer (VHM) D{d}", Unterkategorie = subSpiralVHM, Durchmesser = d, Spitzenwinkel = 140 });

            foreach (var d in ParseArray("[13.5, 16.0, 18.0, 20.0, 22.0, 24.0, 25.0, 28.0, 30.0, 32.0, 35.0, 40.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Wendeplattenbohrer D{d}", Unterkategorie = subWendeBohr, Durchmesser = d });

            foreach (var d in ParseArray("[1.0, 1.6, 2.0, 2.5, 3.15, 4.0, 5.0, 6.3]"))
                werkzeuge.Add(new Werkzeug { Name = $"Zentrierbohrer D{d}", Unterkategorie = subZentrier, Durchmesser = d });

            foreach (var d in ParseArray("[3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 16.0]"))
                foreach (var w in ParseArray("[90, 120]"))
                    werkzeuge.Add(new Werkzeug { Name = $"NC-Anbohrer D{d} W{w}°", Unterkategorie = subNCAnbohr, Durchmesser = d, Spitzenwinkel = w });

            // Gewindewerkzeuge
            var steigungen = ParseArray("[0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0]");
            foreach (var s in steigungen)
                werkzeuge.Add(new Werkzeug { Name = $"Gewindebohrer S{s}", Unterkategorie = subGewindeBohr, Steigung = s });

            foreach (var s in steigungen)
                werkzeuge.Add(new Werkzeug { Name = $"Gewindeformer S{s}", Unterkategorie = subGewindeForm, Steigung = s });

            foreach (var d in ParseArray("[4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 16.0]"))
                foreach (var s in steigungen)
                    werkzeuge.Add(new Werkzeug { Name = $"Gewindefräser D{d} S{s}", Unterkategorie = subGewindeFraes, Durchmesser = d, Steigung = s });

            // Reib-/Senkwerkzeuge
            foreach (var d in ParseArray("[3.0, 4.0, 5.0, 6.0, 8.0, 10.0, 12.0, 16.0, 20.0, 25.0]"))
                werkzeuge.Add(new Werkzeug { Name = $"Maschinenreibahle D{d}", Unterkategorie = subReibahle, Durchmesser = d });

            foreach (var d in ParseArray("[6.3, 8.3, 10.4, 12.4, 16.5, 20.5, 25.0]"))
                foreach (var w in ParseArray("[60, 90, 120]"))
                    werkzeuge.Add(new Werkzeug { Name = $"Kegelsenker D{d} W{w}°", Unterkategorie = subKegelsenker, Durchmesser = d, Spitzenwinkel = w });


            // --- Schritt 5: Alle erstellten Werkzeuge zur Datenbank hinzufügen ---
            context.Werkzeuge.AddRange(werkzeuge);

            // --- SCHRITT 6: Standard-Standort hinzufügen (KORRIGIERT) ---
            var defaultStandort = new Standort
            {
                // Die StandortID wird von der Datenbank automatisch vergeben
                Name = "STB Maschinenbau AG",
                PLZ = "9032",
                Stadt = "Engelburg",
                Strasse = "Breitschachenstrasse",
                Hausnummer = "56"
            };
            context.Standorte.Add(defaultStandort); // <-- Dieser Befehl hat gefehlt

            var defaultHersteller = new Hersteller
            {
                // Die StandortID wird von der Datenbank automatisch vergeben
                HerstellerID = 1,
                Name = "Okuma",
            };
            context.Hersteller.Add(defaultHersteller); // <-- Dieser Befehl hat gefehlt

            var defaultMaschine = new Maschine
            {
                // Die StandortID wird von der Datenbank automatisch vergeben
                HerstellerID = 1,
                Name = "ES-L8",
                Seriennummer = "XXYY123",
                AnzahlStationen = 12,
                StandortID = 1
            };
            context.Maschinen.Add(defaultMaschine); // <-- Dieser Befehl hat gefehlt


            // --- SCHRITT 6b: Standardwerkzeuge für ES-L8 zuweisen ---

            // Holen Sie sich Referenzen auf die Werkzeuge, die Sie zuweisen möchten.
            // Diese wurden in Schritt 4 bereits zur 'werkzeuge'-Liste hinzugefügt.
            // Wir verwenden FirstOrDefault, um Fehler zu vermeiden, falls sich die Namen ändern.
            var werkzeugSt1 = werkzeuge.FirstOrDefault(w => w.Name == "Aussendrehstahl R0.4");
            var werkzeugSt2 = werkzeuge.FirstOrDefault(w => w.Name == "Aussendrehstahl R0.8");
            var werkzeugSt4 = werkzeuge.FirstOrDefault(w => w.Name == "Abstechstahl B3 Tmax15");

            // Erstellen Sie die Zuweisungen für die 'defaultMaschine'
            // Wir können 'werkzeugSt1' usw. direkt verwenden, auch wenn sie null sind.
            // EF Core wird die Zuweisung einfach überspringen, wenn das Werkzeug nicht gefunden wurde.
            if (werkzeugSt1 != null)
            {
                context.StandardWerkzeugZuweisungen.Add(new StandardWerkzeugZuweisung
                {
                    RevolverStation = 1,
                    ZugehoerigeMaschine = defaultMaschine, // Verknüpfung mit der Maschine
                    ZugehoerigesWerkzeug = werkzeugSt1      // Verknüpfung mit dem Werkzeug
                });
            }

            if (werkzeugSt2 != null)
            {
                context.StandardWerkzeugZuweisungen.Add(new StandardWerkzeugZuweisung
                {
                    RevolverStation = 2,
                    ZugehoerigeMaschine = defaultMaschine,
                    ZugehoerigesWerkzeug = werkzeugSt2
                });
            }

            if (werkzeugSt4 != null)
            {
                context.StandardWerkzeugZuweisungen.Add(new StandardWerkzeugZuweisung
                {
                    RevolverStation = 4,
                    ZugehoerigeMaschine = defaultMaschine,
                    ZugehoerigesWerkzeug = werkzeugSt4
                });
            }

            // --- Schritt 7 (vorher 6): Alle Änderungen auf einmal speichern ---
            // Dies ist die effizienteste Methode, da alles in einer Transaktion passiert.
            try
            {
                context.SaveChanges(); // <-- Speichert jetzt Werkzeuge, Kategorien UND den Standort
            }
            catch (Exception ex)
            {
                // Sollte ein Fehler auftreten, wird er in der Konsole ausgegeben
                // (im echten Programm besser ein Logging-System oder MessageBox verwenden)
                Console.WriteLine($"Fehler beim Seeden der Datenbank: {ex.Message}");
            }
        }
    }
}