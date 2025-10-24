using NC_Setup_Assist.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

            var lines = File.ReadAllLines(filePath);
            string currentRevolverStation = "";
            int reihenfolgeCounter = 1;

            int maximaleAbstechdrehzahl = 0;
            double xValue = 0;
            string abstechwerkzeugStangenanfang = string.Empty;
            string anschlagWerkzeug = string.Empty;
            double abstechPositionZ = 0;
            string vlmon1 = string.Empty;
            string vlmon2 = string.Empty;
            bool nat = false;

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
            var natRegex = new Regex(@"NAT(\d+)");
            var xValueRegex = new Regex(@"x(-?[\d\.]+)");
            var g71Regex = new Regex(@"G71", RegexOptions.IgnoreCase);
            var fRegex = new Regex(@"F(-?[\d\.]+)");
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
                    string station = abstechwerkzeugStangenanfang.Substring(0, 2);
                    string korrektur = abstechwerkzeugStangenanfang.Substring(2, 2);
                    werkzeugEinsaetze.Add(new WerkzeugEinsatz
                    {
                        Kommentar = $"Teil abstechen: WZ {station} / Korr. {korrektur}",
                        Reihenfolge = reihenfolgeCounter++
                    });
                }

                var korbVorMatch = korbVorRegex.Match(line);
                if (korbVorMatch.Success)
                {
                    werkzeugEinsaetze.Add(new WerkzeugEinsatz
                    {
                        Kommentar = "Korb Vor (M77)",
                        Reihenfolge = reihenfolgeCounter++
                    });
                }

                var korbZurückMatch = korbZurückRegex.Match(line);
                if (korbZurückMatch.Success)
                {
                    werkzeugEinsaetze.Add(new WerkzeugEinsatz
                    {
                        Kommentar = "Korb Zurück (M76)",
                        Reihenfolge = reihenfolgeCounter++
                    });
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
                    if (g71Match.Success)
                    {
                        var fMatch = fRegex.Match(line);
                        if (double.Parse(xValueMatch.Groups[1].Value, CultureInfo.InvariantCulture) < xValue)
                        {
                            //Wenn ich den Parser laufen lasse und er hier ankommt, sprich

                            //- xValue gefunden
                            //- g71 gefunden
                            //- Jetztiger x-Wert kleiner als der, der zuvor ausgelesen wurde

                            //Dann sollte jetzt beim letzt ausgelesenen Werkzeug in der Liste, Das Werkzeug, Wenn Vorhanden, mit der
                            //Unterkategorie "Gewindedrehstahl Aussen" und der Steigung "0.75" bei der Spalte Werkzeug hinzugefügt werden.

                            //Bei der späteren zuweisung der restlichen Stationen, sollte dieser "Favorit", wie ich ihn nenne, auch noch
                            //geändert werden können.

                            //Die Werkzeuge die als Standard zugewiesen werden, also auf der Maschine, haben die kleinere Priorität.
                            //Also sollte das Favoritenwerkzeug, wenn es im programm gefunden wird, dass Standardwerkzeug überschreiben
                            //auf dieser Station.

                            //Bei der späteren zuweisung im ToolAssignmentComparisonView.xaml Fenster. sollten die Standardwerkzeuge
                            //und die Favoritenwerkzeuge irgendwie unterscheidbar sein. Eventuelle die Standardwerkzege grün hinterlegt,
                            //die Favoriten blau und die noch nicht zugewisenen, und alle die geändert wurden von der vorlage aus Gelb.rot
                        }
                        else if(double.Parse(xValueMatch.Groups[1].Value, CultureInfo.InvariantCulture) >= xValue);
                        {
                            //Wenn ich den Parser laufen lasse und er hier ankommt, sprich

                            //- xValue gefunden
                            //- g71 gefunden
                            //- Jetztiger x-Wert grösser als der, der zuvor ausgelesen wurde

                            //Dann sollte jetzt beim letzt ausgelesenen Werkzeug in der Liste, Das Werkzeug, Wenn Vorhanden, mit der
                            //Unterkategorie "Gewindedrehstahl Innen" und der Steigung "0.75" bei der Spalte Werkzeug hinzugefügt werden.

                            //Bei der späteren zuweisung der restlichen Stationen, sollte dieser "Favorit", wie ich ihn nenne, auch noch
                            //geändert werden können.

                            //Die Werkzeuge die als Standard zugewiesen werden, also auf der Maschine, haben die kleinere Priorität.
                            //Also sollte das Favoritenwerkzeug, wenn es im programm gefunden wird, dass Standardwerkzeug überschreiben
                            //auf dieser Station.

                            //Bei der späteren zuweisung im ToolAssignmentComparisonView.xaml Fenster. sollten die Standardwerkzeuge
                            //und die Favoritenwerkzeuge irgendwie unterscheidbar sein. Eventuelle die Standardwerkzege grün hinterlegt,
                            //die Favoriten blau und die noch nicht zugewisenen, und alle die geändert wurden von der vorlage aus Gelb.rot
                        }
                    }
                    xValue = double.Parse(xValueMatch.Groups[1].Value);
                }

                var natMatch = natRegex.Match(line);
                if (natMatch.Success)
                {
                    nat = true;
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
                                lastTool.Anzahl++;
                            }
                            nat = false;
                            continue;
                        }
                        else if (lastTool != null && toolMatch.Groups[2].Value == lastTool.RevolverStation)
                        {
                            lastTool.Anzahl++;
                            nat = false;
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