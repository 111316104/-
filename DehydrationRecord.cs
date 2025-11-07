using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        [Ignore] // 不儲存於資料庫，只用來顯示用
        public int Volume => DrainVolume - FillVolume;
    }
}
