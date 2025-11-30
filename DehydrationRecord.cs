using System;
using SQLite;

namespace PD_app.Models
{
    public class DehydrationRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string Session { get; set; }

        public int FillVolume { get; set; }

        public int DrainVolume { get; set; }

        // 體重 (kg)
        public float? Weight { get; set; }

        // 血壓 (mmHg)
        public int? Systolic { get; set; }
        public int? Diastolic { get; set; }
        // 這次要存進資料庫
        public int Volume { get; set; }
    }
}
