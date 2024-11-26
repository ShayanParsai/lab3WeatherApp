using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ShayanParsaiApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var filePath = "TempFuktData.csv";

            // Kontrollera att filen existerar
            if (!File.Exists(filePath))
            {
                Console.WriteLine("CSV-filen hittades inte. Kontrollera filvägen.");
                return;
            }

            using (var context = new ShayanParsaiDbContext()) // Initiera databaskontext
            {
                // Rensa tabellen innan ny import
                context.Measurements.RemoveRange(context.Measurements);
                context.SaveChanges();
                Console.WriteLine("Tidigare data rensad från databasen.");

                // Läs och importera CSV-data
                var lines = File.ReadAllLines(filePath); // Läs alla rader från filen
                foreach (var line in lines.Skip(1)) // Hoppa över rubrikraden
                {
                    try
                    {
                        var parts = line.Split(',');

                        // Kontrollera och parsa datan
                        if (parts.Length != 4) continue;
                        if (!DateTime.TryParse(parts[0], out var date)) continue;
                        var location = parts[1];
                        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature)) continue;
                        if (!int.TryParse(parts[3], out var humidity)) continue;

                        var measurement = new Measurement
                        {
                            Date = date,
                            Location = location,
                            Temperature = temperature,
                            Humidity = humidity
                        };

                        context.Measurements.Add(measurement);
                    }
                    catch (Exception ex) // Så att vi inte dyker på näsan :) 
                    {
                        Console.WriteLine($"Fel vid bearbetning av rad: {line}. Fel: {ex.Message}");
                    }
                }

                context.SaveChanges(); // Spara ändringar till databasen
                Console.WriteLine("CSV-data har importerats till databasen.");
                // --------------------------------------------------------------------------------
                // ---- Medeltemperatur för valt datum --------------------------------
                Console.WriteLine("Ange ett datum (YYYY-MM-DD) för att beräkna medeltemperatur:");
                var inputDate = Console.ReadLine();

                if (DateTime.TryParse(inputDate, out var selectedDate))
                {
                    var outdoorMeasurements = context.Measurements
                        .Where(m => m.Date.Date == selectedDate.Date && m.Location == "Ute")
                        .ToList();

                    var indoorMeasurements = context.Measurements
                        .Where(m => m.Date.Date == selectedDate.Date && m.Location == "Inne")
                        .ToList();

                    if (outdoorMeasurements.Any())
                    {
                        var averageOutdoorTemperature = outdoorMeasurements.Average(m => m.Temperature);
                        Console.WriteLine($"Utomhus medeltemperatur för {selectedDate:yyyy-MM-dd}: {averageOutdoorTemperature:F2}°C.");
                    }
                    else
                    {
                        Console.WriteLine($"Inga utomhusmätningar hittades för {selectedDate:yyyy-MM-dd}.");
                    }

                    if (indoorMeasurements.Any())
                    {
                        var averageIndoorTemperature = indoorMeasurements.Average(m => m.Temperature);
                        Console.WriteLine($"Inomhus medeltemperatur för {selectedDate:yyyy-MM-dd}: {averageIndoorTemperature:F2}°C.");
                    }
                    else
                    {
                        Console.WriteLine($"Inga inomhusmätningar hittades för {selectedDate:yyyy-MM-dd}.");
                    }
                }
                else
                {
                    Console.WriteLine("Ogiltigt datumformat.");
                }
                // -------------------------------------------------------------------------
                // ---- Varmaste till kallaste dag -----------------------------------------
                Console.WriteLine("Sorterar dagar från varmast till kallast (medeltemperatur):");
                var warmToColdOutdoor = context.Measurements
                    .Where(m => m.Location == "Ute")
                    .GroupBy(m => m.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        AverageTemperature = g.Average(m => m.Temperature)
                    })
                    .OrderByDescending(g => g.AverageTemperature)
                    .ToList();

                Console.WriteLine("Utomhus:");
                foreach (var day in warmToColdOutdoor)
                {
                    Console.WriteLine($"{day.Date:yyyy-MM-dd}: {day.AverageTemperature:F2}°C");
                }

                var warmToColdIndoor = context.Measurements
                    .Where(m => m.Location == "Inne")
                    .GroupBy(m => m.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        AverageTemperature = g.Average(m => m.Temperature)
                    })
                    .OrderByDescending(g => g.AverageTemperature)
                    .ToList();

                Console.WriteLine("Inomhus:");
                foreach (var day in warmToColdIndoor)
                {
                    Console.WriteLine($"{day.Date:yyyy-MM-dd}: {day.AverageTemperature:F2}°C");
                }
                // -------------------------------------------------------------------------
                // ---- Torraste till fuktigaste dag ----------------------------------------
                Console.WriteLine("Sorterar dagar från torrast till fuktigast (medelluftfuktighet):");

                var dryToHumidOutdoor = context.Measurements
                    .Where(m => m.Location == "Ute")
                    .GroupBy(m => m.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        AverageHumidity = g.Average(m => m.Humidity)
                    })
                    .OrderBy(g => g.AverageHumidity)
                    .ToList();

                Console.WriteLine("Utomhus:");
                foreach (var day in dryToHumidOutdoor)
                {
                    Console.WriteLine($"{day.Date:yyyy-MM-dd}: {day.AverageHumidity:F2}% luftfuktighet");
                }

                var dryToHumidIndoor = context.Measurements
                    .Where(m => m.Location == "Inne")
                    .GroupBy(m => m.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        AverageHumidity = g.Average(m => m.Humidity)
                    })
                    .OrderBy(g => g.AverageHumidity)
                    .ToList();

                Console.WriteLine("Inomhus:");
                foreach (var day in dryToHumidIndoor)
                {
                    Console.WriteLine($"{day.Date:yyyy-MM-dd}: {day.AverageHumidity:F2}% luftfuktighet");
                }
                // -------------------------------------------------------------------------
                // ---- Mögelrisk (inomhus och utomhus) -----------------------------------------
                Console.WriteLine("Sorterar dagar från minst till störst risk för mögel:");

                Console.WriteLine("\nInomhus:");
                var moldRiskDataIndoor = context.Measurements
                    .Where(m => m.Location == "Inne")
                    .GroupBy(m => m.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        MoldRisk = g.Average(m => (m.Humidity - 78) * (m.Temperature / 15))
                    })
                    .OrderBy(r => r.MoldRisk)
                    .ToList();

                foreach (var day in moldRiskDataIndoor)
                {
                    var riskStatus = day.MoldRisk > 0 ? "Risk för mögel" : "Ingen risk för mögel";
                    Console.WriteLine($"{day.Date:yyyy-MM-dd}: Mögelrisk {day.MoldRisk:F2} - {riskStatus}");
                }

                Console.WriteLine("\nUtomhus:");
                var moldRiskDataOutdoor = context.Measurements
                    .Where(m => m.Location == "Ute")
                    .GroupBy(m => m.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        MoldRisk = g.Average(m => (m.Humidity - 78) * (m.Temperature / 15))
                    })
                    .OrderBy(r => r.MoldRisk)
                    .ToList();

                foreach (var day in moldRiskDataOutdoor)
                {
                    var riskStatus = day.MoldRisk > 0 ? "Risk för mögel" : "Ingen risk för mögel";
                    Console.WriteLine($"{day.Date:yyyy-MM-dd}: Mögelrisk {day.MoldRisk:F2} - {riskStatus}");
                }
                // -------------------------------------------------------------------------

                // ---- Meteorologisk höst och vinter (endast utomhus) ----------------------
                Console.WriteLine("\nBeräknar datum för meteorologisk höst och vinter (endast utomhus):");

                var dailyAveragesOutdoor = context.Measurements
                    .Where(m => m.Location == "Ute")
                    .GroupBy(m => m.Date.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        AverageTemperature = g.Average(m => m.Temperature)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Hitta meteorologisk höst
                DateTime? autumnStart = null;
                int consecutiveAutumnDays = 0;

                foreach (var day in dailyAveragesOutdoor)
                {
                    if (day.AverageTemperature < 10)
                    {
                        consecutiveAutumnDays++;
                        if (consecutiveAutumnDays == 7)
                        {
                            autumnStart = day.Date.AddDays(-6);
                            break;
                        }
                    }
                    else
                    {
                        consecutiveAutumnDays = 0;
                    }
                }

                // Hitta meteorologisk vinter
                DateTime? winterStart = null;
                int consecutiveWinterDays = 0;

                foreach (var day in dailyAveragesOutdoor)
                {
                    if (day.AverageTemperature < 0)
                    {
                        consecutiveWinterDays++;
                        if (consecutiveWinterDays == 7)
                        {
                            winterStart = day.Date.AddDays(-6);
                            break;
                        }
                    }
                    else
                    {
                        consecutiveWinterDays = 0;
                    }
                }

                // Skriv ut resultaten
                if (autumnStart.HasValue)
                    Console.WriteLine($"Meteorologisk höst börjar (utomhus): {autumnStart.Value:yyyy-MM-dd}");
                else
                    Console.WriteLine("Meteorologisk höst hittades inte i datan.");

                if (winterStart.HasValue)
                    Console.WriteLine($"Meteorologisk vinter börjar (utomhus): {winterStart.Value:yyyy-MM-dd}");
                else
                    Console.WriteLine("Meteorologisk vinter hittades inte i datan.");
                // -------------------------------------------------------------------------
            }
        }
    }
}