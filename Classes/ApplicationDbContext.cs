using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TaskManagerTelegramBot_Дегтянников.Classes
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Commands> Commands { get; set; }
        public ApplicationDbContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
                "server=127.0.0.1;port=3306;database=TaskManagerDB;user=root;password=;",
                new MySqlServerVersion(new Version(8, 0, 21))
            );
        }
    }
}
