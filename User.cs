using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PD_app.Models
{
    public class User
    {
        [PrimaryKey]
        public string Id { get; set; }  // 身分證為主鍵

        public string password { get; set; }  // 出生日期
    }
}
