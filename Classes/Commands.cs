using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerTelegramBot_Дегтянников.Classes
{
    public class Commands
    {
        public int Id { get; set; }
        public string User { get; set; }
        public string Commandos { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
