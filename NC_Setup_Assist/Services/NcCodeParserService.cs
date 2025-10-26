using NC_Setup_Assist.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NC_Setup_Assist.Data; // NEU
using Microsoft.EntityFrameworkCore; // NEU

namespace NC_Setup_Assist.Services
{
    public class NcCodeParserService
    {
        public List<WerkzeugEinsatz> Parse(string filePath)
        {
            var werkzeugEinsaetze = new List<WerkzeugEinsatz>();
            if (!File.Exists(filePath))
            {
                return werkzeugEinsaetze;
            }

            // NEU: DbContext und Favoriten-Werkzeuge laden
            using var context = new NcSetupContext();

            var lines = File.ReadAllLines(filePath);
            string currentRevolverStation = "";
            int reihenfolgeCounter = 1;
            int natAnzahl = 0;

            int maximaleAbstechdrehzahl = 0;
            double xValue = 0;
            string abstechwerkzeugStangenanfang = string.Empty;
            string anschlagWerkzeug = string.Empty;
            double abstechPositionZ = 0;
            string vlmon1 = string.Empty;
            string vlmon2 = string.Empty;
            bool g81 = false;
            bool g41 = false;
            bool g73 = false;
            bool nat = false;
            bool sb = false;
            bool spindleOn = false;

            #region Regex Definitionen
            var g50Regex = new Regex(@"G50\s+S(\d+)");
            var smaxRegex = new Regex(@"SMAX=(\d+)", RegexOptions.IgnoreCase);
            var abwsRegex = new Regex(@"ABWS=(\d+)", RegexOptions.IgnoreCase);
            var wsRegex = new Regex(@"WS=(\d+)", RegexOptions.IgnoreCase);
            var apzRegex = new Regex(@"APZ=(-?[\d\.]+)");
            var callOlns5Regex = new Regex(@"CALL OLNS5", RegexOptions.IgnoreCase);
            var vlmonRegex = new Regex(@"VLMON\[(\d+)\]=(\d+)");
            var korbVorRegex = new Regex(@"\bM77\b");
            var korbZurückRegex = new Regex(@"\bM76\b");
            var toolRegex = new Regex(@"\bT(\d{2})(\d{2})?");
            var natRegex = new Regex(@"NAT(\d{2})");
            var xValueRegex = new Regex(@"\bX(-?[\d\.]+)", RegexOptions.IgnoreCase);
            var g71Regex = new Regex(@"G71", RegexOptions.IgnoreCase);
            var fRegex = new Regex(@"F(-?[\d\.]+)");
            var sbRegex = new Regex(@"SB=(-?[\d\.]+)");
            var g101Regex = new Regex(@"G101", RegexOptions.IgnoreCase);
            var callOplRegex = new Regex(@"CALL OPL", RegexOptions.IgnoreCase);
            var m3Regex = new Regex(@"\bM0?3\b", RegexOptions.IgnoreCase);
            var m4Regex = new Regex(@"\bM0?4\b", RegexOptions.IgnoreCase);
            var m5Regex = new Regex(@"\bM0?5\b", RegexOptions.IgnoreCase);
            var g74Regex = new Regex(@"G74", RegexOptions.IgnoreCase);
            var g81Regex = new Regex(@"G81", RegexOptions.IgnoreCase);
            var g41Regex = new Regex(@"G41", RegexOptions.IgnoreCase);
            var g73Regex = new Regex(@"G73", RegexOptions.IgnoreCase);
            var g87Regex = new Regex(@"G87", RegexOptions.IgnoreCase);
            #endregion

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // --- ZUERST KOMMENTARE UND EREIGNISSE PRÜFEN ---

                var g50Match = g50Regex.Match(line);
                if (g50Match.Success)
                {
                    int maximaleDrehzahl = int.Parse(g50Match.Groups[1].Value);
                    werkzeugEinsaetze.Add(new WerkzeugEinsatz
                    {
                        Kommentar = $"Max. Spindeldrehzahl = S{maximaleDrehzahl}",
                        Reihenfolge = reihenfolgeCounter++
                    });
                }

                var callOlns5Match = callOlns5Regex.Match(line);
                if (callOlns5Match.Success && !string.IsNullOrEmpty(abstechwerkzeugStangenanfang))
                {
                    string abstechStation = abstechwerkzeugStangenanfang.Substring(0, 2);
                    string abstechkorrektur = abstechwerkzeugStangenanfang.Substring(2, 2);
                    werkzeugEinsaetze.Add(new WerkzeugEinsatz
                    {
                        Anzahl = 1,
                        RevolverStation = abstechStation,
                        KorrekturNummer = (abstechStation != abstechkorrektur) ? abstechkorrektur : null,
                        Kommentar = $"Teil abstechen",
                        Reihenfolge = reihenfolgeCounter++,
                        FavoritKategorie = "Drehwerkzeuge",
                        FavoritUnterkategorie = "Einstechstahl Aussen",
                    });

                    string anschlagStation = anschlagWerkzeug.Substring(0, 2);
                    string anschlagKorrektur = anschlagWerkzeug.Substring(2, 2);
                    werkzeugEinsaetze.Add(new WerkzeugEinsatz
                    {
                        Anzahl = 1,
                        RevolverStation = anschlagStation,
                        KorrekturNummer = (anschlagStation != anschlagKorrektur) ? anschlagKorrektur : null,
                        Kommentar = $"Rohsteil herausziehen",
                        Reihenfolge = reihenfolgeCounter++,
                        FavoritKategorie = "Drehwerkzeuge",
                        FavoritUnterkategorie = "Einstechstahl Aussen",
                    });
                }

                var korbVorMatch = korbVorRegex.Match(line);
                if (korbVorMatch.Success)
                {
                    // M77 (Korb vor) - Aktuell keine Aktion erforderlich
                }

                var korbZurückMatch = korbZurückRegex.Match(line);
                if (korbZurückMatch.Success)
                {
                    // --- LOGIK FÜR KORB (M76) ---
                    // Finde den letzten *echten* Werkzeugeintrag (nicht nur einen Kommentar)
                    var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));
                    if (lastTool != null)
                    {
                        // Setze die Eigenschaft für die UI-Anzeige
                        lastTool.VerwendetKorb = true;
                    }
                    // --- ENDE LOGIK ---
                }

                // --- DANN DIE VARIABLEN-ZUWEISUNGEN ---

                var smaxMatch = smaxRegex.Match(line);
                if (smaxMatch.Success)
                {
                    maximaleAbstechdrehzahl = int.Parse(smaxMatch.Groups[1].Value);
                }

                var abwsMatch = abwsRegex.Match(line);
                if (abwsMatch.Success)
                {
                    abstechwerkzeugStangenanfang = abwsMatch.Groups[1].Value;
                }

                var wsMatch = wsRegex.Match(line);
                if (wsMatch.Success)
                {
                    anschlagWerkzeug = wsMatch.Groups[1].Value;
                }

                var apzMatch = apzRegex.Match(line);
                if (apzMatch.Success)
                {
                    abstechPositionZ = double.Parse(apzMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }

                var vlmonMatch = vlmonRegex.Match(line);
                if (vlmonMatch.Success)
                {
                    vlmon1 = vlmonMatch.Groups[1].Value;
                    vlmon2 = vlmonMatch.Groups[2].Value;
                }

                var xValueMatch = xValueRegex.Match(line);
                if (xValueMatch.Success)
                {
                    var g71Match = g71Regex.Match(line);
                    if (g71Match.Success) // G71 (Gewinde) gefunden
                    {
                        var fMatch = fRegex.Match(line);
                        double currentXValue = double.Parse(xValueMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));

                        // Prüfen, ob alle nötigen Infos da sind (letztes Werkzeug, F-Wert für Steigung)
                        if (lastTool != null && fMatch.Success)
                        {
                            // 1. Steigung (Pitch) aus F-Wert parsen
                            if (double.TryParse(fMatch.Groups[1].Value, System.Globalization.NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double parsedPitch))
                            {
                                Werkzeug? foundTool = null;

                                // 2. Bestimmen, ob Aussen- oder Innengewinde (basierend auf deiner X-Wert-Logik)
                                if (currentXValue < xValue)
                                {
                                    // AUSSENGEWINDE suchen
                                    // WICHTIG: .Include(w => w.Unterkategorie.Kategorie) um beide Namen zu haben
                                    foundTool = context.Werkzeuge
                                        .Include(w => w.Unterkategorie.Kategorie)
                                        .FirstOrDefault(w => w.Unterkategorie.Name == "Gewindedrehstahl Aussen" && w.Steigung == parsedPitch);
                                }
                                else if (currentXValue >= xValue)
                                {
                                    // INNENGEWINDE suchen
                                    foundTool = context.Werkzeuge
                                        .Include(w => w.Unterkategorie.Kategorie)
                                        .FirstOrDefault(w => w.Unterkategorie.Name == "Gewindedrehstahl Innen" && w.Steigung == parsedPitch);
                                }

                                // 3. Favoriten setzen, wenn ein Werkzeug mit EXAKT passender Steigung gefunden wurde
                                if (foundTool != null)
                                {
                                    // Setze die Favoriten basierend auf dem gefundenen Werkzeug
                                    lastTool.WerkzeugID = foundTool.WerkzeugID;
                                    lastTool.FavoritKategorie = foundTool.Unterkategorie.Kategorie.Name; // z.B. "Drehwerkzeug"
                                    lastTool.FavoritUnterkategorie = foundTool.Unterkategorie.Name; // z.B. "Gewindedrehstahl Aussen"
                                }
                            }
                        }
                    }

                    var g73Match = g73Regex.Match(line);
                    if (g73Match.Success) // G71 (Gewinde) gefunden
                    {
                        var fMatch = fRegex.Match(line);
                        double currentXValue = double.Parse(xValueMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));

                        // Prüfen, ob alle nötigen Infos da sind (letztes Werkzeug, F-Wert für Steigung)
                        if (lastTool != null && fMatch.Success && spindleOn == true)
                        {
                            Werkzeug? foundTool = null;

                            // 2. Bestimmen, ob Aussen- oder Innengewinde (basierend auf deiner X-Wert-Logik)
                            if (currentXValue < xValue)
                            {
                                // AUSSENEINSTICH suchen
                                ; lastTool.FavoritKategorie = "Drehwerkzeuge";
                                lastTool.FavoritUnterkategorie = "Einstechstahl Aussen";
                            }
                            else if (currentXValue >= xValue)
                            {
                                // INNENEINSTICH suchen
                                ; lastTool.FavoritKategorie = "Drehwerkzeuge";
                                lastTool.FavoritUnterkategorie = "Einstechstahl Innen";
                            }
                        }
                    }




                    xValue = double.Parse(xValueMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }

                var sbMatch = sbRegex.Match(line);
                if (sbMatch.Success)
                {
                    sb = true;
                }

                var g101Match = g101Regex.Match(line);
                if (g101Match.Success && sb == true)
                {
                    // HINZUGEFÜGT: Setze FräserAusrichtung und FavoritKategorie
                    var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));
                    if (lastTool != null)
                    {
                        lastTool.FräserAusrichtung = "←";
                        lastTool.FavoritKategorie = "Fräswerkzeuge";
                        lastTool.FavoritUnterkategorie = null; // Es ist nur "Fräsen", nicht spezifisch
                    }
                }

                var callOplMatch = callOplRegex.Match(line);
                if (callOplMatch.Success)
                {
                    var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));

                    if (lastTool != null)
                    {
                        // Weise die Favoriten für "Gravurstichel" (ID 5 -> "Kugelfräser" / "Fräser") zu
                        // Entferne die alte WerkzeugID-Zuweisung
                        // lastTool.WerkzeugID = 5; // ALT
                        lastTool.WerkzeugID = 160;
                        lastTool.FräserAusrichtung = "←";
                        lastTool.FavoritKategorie = "Fräswerkzeuge";
                        lastTool.FavoritUnterkategorie = "Kugelfräser";
                    }
                }

                var m3Match = m3Regex.Match(line);
                var m4Match = m4Regex.Match(line);
                if (m3Match.Success || m4Match.Success)
                {
                    spindleOn = true;
                }

                var m5Match = m5Regex.Match(line);
                if (m5Match.Success)
                {
                    spindleOn = false;
                }

                var g74Match = g74Regex.Match(line);
                if (g74Match.Success && spindleOn == true)
                {
                    var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));
                    if (lastTool != null)
                    {
                        lastTool.FavoritKategorie = "Bohrwerkzeuge";
                    }
                }

                var g81Match = g81Regex.Match(line);
                if (g81Match.Success)
                {
                    g81 = true;
                }

                var g41Match = g41Regex.Match(line);
                if (g41Match.Success)
                {
                    g41 = true;
                }

                if (g41 == true && g81 == true && spindleOn == true)
                {
                    var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));
                    if (lastTool != null)
                    {
                        lastTool.FavoritKategorie = "Drehwerkzeuge";
                        lastTool.FavoritUnterkategorie = "Innendrehstahl";
                    }
                }

                var natMatch = natRegex.Match(line);
                if (natMatch.Success)
                {
                    if (nat == true)
                    {
                        natAnzahl++;
                    }
                    nat = true;
                    sb = false;
                    g81 = false;
                    g41 = false;
                }

                // --- ZULETZT DIE WERKZEUGVERARBEITUNG ---
                var toolMatch = toolRegex.Match(line);
                if (toolMatch.Success)
                {
                    string toolCode = toolMatch.Groups[1].Value + (toolMatch.Groups[2].Success ? toolMatch.Groups[2].Value : "");

                    var lastTool = werkzeugEinsaetze.LastOrDefault();

                    if (toolCode.Length < 4)
                    {
                        if (line.Contains("G71") || line.Contains("G73") || line.Contains("G74") || line.Contains("G75"))
                        {
                            if (lastTool != null)
                            {
                                if (toolMatch.Groups[1].Value == lastTool.KorrekturNummer)
                                {
                                    continue;
                                }
                            }
                            else if (toolMatch.Groups[1].Value == lastTool.RevolverStation)
                            {
                                continue;
                            }
                            else if (lastTool != null && lastTool.KorrekturNummer != "")
                            {
                                lastTool.KorrekturNummer = toolMatch.Groups[1].Value;
                                continue;
                            }
                            if (lastTool != null)
                            {
                                lastTool.KorrekturNummer = toolMatch.Groups[1].Value;
                            }
                        }
                    }
                    else if (nat == true)
                    {
                        if (toolMatch.Groups[2].Value == "00")
                        {
                            if (nat == true)
                            {
                                lastTool.Anzahl += natAnzahl + 1;
                            }
                            nat = false;
                            natAnzahl = 0;
                            g81 = false;
                            g41 = true;
                            continue;
                        }
                        else if (lastTool != null && toolMatch.Groups[2].Value == lastTool.RevolverStation)
                        {
                            lastTool.Anzahl++;
                            nat = false;
                            natAnzahl = 0;
                            continue;
                        }
                        else if (lastTool != null)
                        {
                            var neuesWerkzeug = new WerkzeugEinsatz
                            {
                                RevolverStation = toolMatch.Groups[2].Value,
                                Reihenfolge = reihenfolgeCounter++,
                                Anzahl = 1,
                            };

                            // Wenn Gruppe1 != Gruppe2 -> Gruppe1 als Korrekturnummer setzen (falls parsebar)
                            if (toolMatch.Groups[1].Value != toolMatch.Groups[2].Value)
                            {
                                neuesWerkzeug.KorrekturNummer = toolMatch.Groups[1].Value;
                            }

                            werkzeugEinsaetze.Add(neuesWerkzeug);
                        }
                        else if (toolMatch.Groups[1].Value != toolMatch.Groups[2].Value)
                        {
                            lastTool.KorrekturNummer = toolMatch.Groups[1].Value;
                            continue;
                        }


                        nat = false;
                        natAnzahl = 0;
                    }
                    else if (lastTool != null && lastTool != null && toolMatch.Groups[2].Value == lastTool.RevolverStation)
                    {
                        if (toolMatch.Groups[1].Value == lastTool.KorrekturNummer)
                        {
                            continue;
                        }
                        else if (toolMatch.Groups[1].Value == lastTool.RevolverStation)
                        {
                            continue;
                        }
                        else if (lastTool.KorrekturNummer != "")
                        {
                            lastTool.KorrekturNummer = toolMatch.Groups[1].Value;
                            continue;
                        }
                        continue;
                    }
                    if (toolMatch.Groups[2].Value == "00")
                    {
                        g81 = false;
                        g41 = false;
                    }
                }
            }
            return werkzeugEinsaetze;
        }
    }
}