using System;

namespace ShayanParsaiApp
{
    public class Measurement
    {
        public int Id { get; set; } // Primärnyckel
        public DateTime Date { get; set; } // Datum och tid
        public string Location { get; set; } // Plats (t.ex. "Inne" eller "Ute")
        public float Temperature { get; set; } // Temperatur i Celsius
        public int Humidity { get; set; } // Luftfuktighet i procent
    }
}