using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagerTelegramBot_Дегтянников.Classes
{
    public class Users
    {
        public long IdUser { get; set; }
        public List<Events> Events { get; set; }
        public List<ZadachiRepeat> RepeatEvents { get; set; }
        public Users(long idUser) 
        {
            this.IdUser = idUser;
            Events = new List<Events>();
            RepeatEvents = new List<ZadachiRepeat>();
        }
    }
}
