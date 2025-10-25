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
            bool nat = false;
            bool sb = false;

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
                        RevolverStation = abstechStation,
                        KorrekturNummer = (abstechStation != abstechkorrektur) ? abstechkorrektur : null,
                        Kommentar = $"Teil abstechen",
                        Reihenfolge = reihenfolgeCounter++
                    });

                    string anschlagStation = anschlagWerkzeug.Substring(0, 2);
                    string anschlagKorrektur = anschlagWerkzeug.Substring(2, 2);
                    werkzeugEinsaetze.Add(new WerkzeugEinsatz
                    {
                        RevolverStation = anschlagStation,
                        KorrekturNummer = (anschlagStation != anschlagKorrektur) ? anschlagKorrektur : null,
                        Kommentar = $"Rohsteil herausziehen",
                        Reihenfolge = reihenfolgeCounter++
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
                                    foundTool = context.Werkzeuge
                                        .Include(w => w.Unterkategorie)
                                        .FirstOrDefault(w => w.Unterkategorie.Name == "Gewindedrehstahl Aussen" && w.Steigung == parsedPitch);
                                }
                                else if (currentXValue >= xValue)
                                {
                                    // INNENGEWINDE suchen
                                    foundTool = context.Werkzeuge
                                        .Include(w => w.Unterkategorie)
                                        .FirstOrDefault(w => w.Unterkategorie.Name == "Gewindedrehstahl Innen" && w.Steigung == parsedPitch);
                                }

                                // 3. Nur zuweisen, wenn ein Werkzeug mit EXAKT passender Steigung gefunden wurde
                                if (foundTool != null)
                                {
                                    lastTool.WerkzeugID = foundTool.WerkzeugID;
                                }
                                // Wenn foundTool == null, wird kein "Favorit" zugewiesen.
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
                    // HINZUGEFÜGT: Setze BearbeitungsArt für Fräswerkzeug (G101 + SB=true)
                    var lastTool = werkzeugEinsaetze.LastOrDefault(w => !string.IsNullOrEmpty(w.RevolverStation));
                    if (lastTool != null)
                    {
                        // "←" Symbol wird in der AnalysisView (XAML) basierend auf dieser Eigenschaft angezeigt.
                        // "Fräsen" wird für die spätere Filterlogik gespeichert.
                        lastTool.BearbeitungsArt = "Fräser";
                    }

                    // Der Rest der Anforderung (Spalte in AnalysisView.xaml, Filter in ToolManagementView.xaml)
                    // muss in den entsprechenden Dateien umgesetzt werden.
                }

                var callOplMatch = callOplRegex.Match(line);
                if (callOplMatch.Success)
                {
                    Werkzeug? foundTool = null;

                    //foundTool = context.Werkzeuge
                    //        .Include(w => w.Unterkategorie)
                    //        .FirstOrDefault(w => w.Unterkategorie.Name == "Gewindedrehstahl Aussen" && w.Steigung == parsedPitch);

                    //if (foundTool != null)
                    //{
                    //    lastTool.WerkzeugID = foundTool.WerkzeugID;
                    //}
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
                }
            }
            return werkzeugEinsaetze;
        }
    }
}