using TaskManagerTelegramBot_Дегтянников.Classes;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TaskManagerTelegramBot_Дегтянников
{
    public class Worker : BackgroundService
    {
        readonly string Token = "8526951692:AAEESqoEWetKwK3U8JrJzNLc7CZVpKZGStQ";
        TelegramBotClient TelegramBotClient;
        List<Users> Users = new List<Users>();
        Timer Timer;
        List<string> Messages = new List<string>
        {
          "Здравствуйте! \nРад приветствовать вас в Telegram-боте «Напоминатор»!  \nНаш бот создан для того, чтобы напоминать вам о важных событиях и мероприятиях. С ним вы точно не пропустите ничего важного!  \nНе забудьте добавить бота в список своих контактов и настроить уведомления. Тогда вы всегда будете в курсе событий! \n\n" +
          " *Доступные форматы задач:*\n" +
          "1. *Однократная задача:*\n" +
          "   `12:51 26.07.2025`\n" +
          "   Сходить в магазин\n\n" +
          "2. *Повторяющаяся задача:*\n" +
          "   `21:00 СР,ВС`\n" +
          "   Полить цветы",
          "Укажите дату и время напоминания в следующем формате: \n<i><b>12:51 26.07.2025</b> \nНапомни о том что я хотел сходить в магазин.</i>",
          "Кажется, что-то не получилось. Укажите дату и время напоминания в следующем формате: \n<i><b>12:51 26.07.2025</b> \nНапомни о том что я хотел сходить в магазин.</i>",
          "Задачи пользователя не найдены.",
          "Событие удалено.",
          "Все события удалены.",
          "Укажите повторяющуюся задачу в формате:\n<b>21:00 ПН,СР,ПТ</b>\nНапомнить о поливе цветов",
          "Повторяющаяся задача успешно создана! "
        };

        private readonly Dictionary<string, DayOfWeek> DayMapping = new Dictionary<string, DayOfWeek>
        {
            { "ПН", DayOfWeek.Monday },
            { "ВТ", DayOfWeek.Tuesday },
            { "СР", DayOfWeek.Wednesday },
            { "ЧТ", DayOfWeek.Thursday },
            { "ПТ", DayOfWeek.Friday },
            { "СБ", DayOfWeek.Saturday },
            { "ВС", DayOfWeek.Sunday }
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TelegramBotClient = new TelegramBotClient(Token);

            await SetBotCommands();

            TelegramBotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                null,
                new CancellationTokenSource().Token
            );
            TimerCallback TimerCallback = new TimerCallback(Tick);
            Timer = new Timer(TimerCallback, 0, 0, 60 * 1000);
        }

        private async Task SetBotCommands()
        {
            var commands = new List<BotCommand>
            {
                new BotCommand { Command = "start", Description = "Запустить бота" },
                new BotCommand { Command = "create_task", Description = " Создать задачу" },
                new BotCommand { Command = "create_repeat_task", Description = " Создать повтор. задачу" },
                new BotCommand { Command = "delete_task", Description = " Удалить задачу" }
            };

            await TelegramBotClient.SetMyCommands(commands);
        }

        public bool CheckFormatDateTime(string value, out DateTime time)
        {
            return DateTime.TryParse(value, out time);
        }

        private static ReplyKeyboardMarkup GetButtons()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("Создать задачу"),
                    new KeyboardButton(" Повторяющаяся")
                },
                new[]
                {
                    new KeyboardButton(" Мои задачи"),
                    new KeyboardButton(" Удалить задачу")
                },
                new[]
                {
                    new KeyboardButton(" Удалить все")
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        public static void UsersSaved(string message, string Commandos)
        {
            using (ApplicationDbContext dbContext = new ApplicationDbContext())
            {
                try
                {
                    var command = new Commands
                    {
                        User = "@" + message,
                        Commandos = Commandos,
                        Timestamp = DateTime.Now
                    };
                    dbContext.Commands.Add(command);
                    dbContext.SaveChanges();
                    Console.WriteLine($" Команда сохранена в базу: {Commandos} - {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Ошибка при сохранении команды: {ex.Message}");
                }
            }
        }

        private InlineKeyboardMarkup CreateTasksInlineKeyboard(Users user)
        {
            var inlineKeyboard = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < user.Events.Count; i++)
            {
                var task = user.Events[i];
                var buttonText = $"{i + 1}. {task.Time:HH:mm dd.MM.yyyy}";

                inlineKeyboard.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        buttonText,
                        $"delete_{i}"
                    )
                });
            }

            return new InlineKeyboardMarkup(inlineKeyboard);
        }

        public async void SendMessage(long chatId, int typeMessage, string additionalInfo = "")
        {
            string messageText = Messages[typeMessage];

            if (typeMessage == 3 && !string.IsNullOrEmpty(additionalInfo))
            {
                messageText = $"Указанное вами время и дата не могут быть установлены " +
                             $"потому что сейчас уже: {additionalInfo}";
            }

            await TelegramBotClient.SendMessage(
                chatId,
                messageText,
                ParseMode.Html,
                replyMarkup: GetButtons()
            );
        }

        public async void Command(long chatId, string command)
        {
            UsersSaved(command, $"chatId: {chatId}");

            switch (command.ToLower())
            {
                case "/start":
                    SendMessage(chatId, 0);
                    break;

                case "/create_task":
                    await TelegramBotClient.SendMessage(
                        chatId,
                        Messages[1],
                        ParseMode.Html,
                        replyMarkup: GetButtons()
                    );
                    break;

                case "/create_repeat_task":
                    await TelegramBotClient.SendMessage(
                        chatId,
                        Messages[6],
                        ParseMode.Html,
                        replyMarkup: GetButtons()
                    );
                    break;

                case "/delete_task":
                    await ShowDeleteTaskMenu(chatId);
                    break;

                default:
                    await TelegramBotClient.SendMessage(
                        chatId,
                        "Неизвестная команда.",
                        replyMarkup: GetButtons()
                    );
                    break;
            }
        }

        private async Task ShowUserTasks(long chatId)
        {
            Users User = Users.Find(x => x.IdUser == chatId);

            if (User == null || User.Events.Count == 0)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    Messages[3],
                    replyMarkup: GetButtons()
                );
                return;
            }

            string tasksList = "*Ваши задачи:*\n\n";

            for (int i = 0; i < User.Events.Count; i++)
            {
                var task = User.Events[i];
                tasksList += $"*{i + 1}.* {task.Time:HH:mm dd.MM.yyyy}\n" +
                           $"{task.Message}\n\n";
            }

            await TelegramBotClient.SendMessage(
                chatId,
                tasksList,
                ParseMode.Markdown,
                replyMarkup: GetButtons()
            );
        }

        private async Task ShowDeleteTaskMenu(long chatId)
        {
            Users User = Users.Find(x => x.IdUser == chatId);

            if (User == null || User.Events.Count == 0)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    Messages[3],
                    replyMarkup: GetButtons()
                );
                return;
            }

            var inlineKeyboard = CreateTasksInlineKeyboard(User);

            await TelegramBotClient.SendMessage(
                chatId,
                "Выберите задачу для удаления:",
                replyMarkup: inlineKeyboard
            );
        }

        private void GetMessages(Message message)
        {
            Console.WriteLine("Получено сообщение: " + message.Text + " от пользователя: " + message.Chat.Username);

            UsersSaved(message.Text, message.Chat.Username);

            if (message.Text.Contains("/"))
            {
                Command(message.Chat.Id, message.Text);
            }
            else if (message.Text.Equals("Удалить все"))
            {
                Users User = Users.Find(x => x.IdUser == message.Chat.Id);
                if (User == null || (User.Events.Count == 0 && User.RepeatEvents.Count == 0))
                {
                    SendMessage(message.Chat.Id, 3);
                }
                else
                {
                    User.Events.Clear();
                    User.RepeatEvents.Clear();
                    SendMessage(message.Chat.Id, 5);
                }
            }
            else if (message.Text.Equals("Создать задачу"))
            {
                Command(message.Chat.Id, "/create_task");
            }
            else if (message.Text.Equals(" Повторяющаяся"))
            {
                Command(message.Chat.Id, "/create_repeat_task");
            }
            else if (message.Text.Equals(" Мои задачи"))
            {
                UsersSaved("Просмотр задач", message.Chat.Username);
                ShowUserTasks(message.Chat.Id);
            }
            else if (message.Text.Equals(" Удалить задачу"))
            {
                Command(message.Chat.Id, "/delete_task");
            }
            else
            {
                ProcessTaskMessage(message);
            }
        }

        private async void ProcessTaskMessage(Message message)
        {
            Users User = Users.Find(x => x.IdUser == message.Chat.Id);
            if (User == null)
            {
                User = new Users(message.Chat.Id);
                Users.Add(User);
            }

            string[] Info = message.Text.Split('\n');
            if (Info.Length < 2)
            {
                SendMessage(message.Chat.Id, 2);
                return;
            }

            string firstLine = Info[0].Trim();

            if (DateTime.TryParse(firstLine, out DateTime Time))
            {
                if (Time < DateTime.Now)
                {
                    SendMessage(message.Chat.Id, 3, DateTime.Now.ToString("HH:mm dd.MM.yyyy"));
                    return;
                }

                string taskMessage = message.Text.Replace(Time.ToString("HH:mm dd.MM.yyyy") + "\n", "");
                User.Events.Add(new Events(Time, taskMessage));

                UsersSaved($"Создана задача: {Time:HH:mm dd.MM.yyyy}", message.Chat.Username);

                await TelegramBotClient.SendMessage(
                    message.Chat.Id,
                    $" *Задача создана!*\n\n" +
                    $" *Время:* {Time:HH:mm dd.MM.yyyy}\n" +
                    $" *Описание:* {taskMessage}",
                    ParseMode.Markdown,
                    replyMarkup: GetButtons()
                );
            }
            else if (TryParseRepeatTask(firstLine, out TimeSpan timeSpan, out List<DayOfWeek> days))
            {
                string taskMessage = string.Join("\n", Info.Skip(1)).Trim();
                var repeatTask = new ZadachiRepeat(days, timeSpan, taskMessage);
                User.RepeatEvents.Add(repeatTask);

                string daysText = FormatDays(days);

                UsersSaved($"Создана повторяющаяся задача: {timeSpan:hh\\:mm} {daysText}", message.Chat.Username);

                await TelegramBotClient.SendMessage(
                    message.Chat.Id,
                    $" *Повторяющаяся задача создана!*\n\n" +
                    $" *Время:* {timeSpan:hh\\:mm}\n" +
                    $" *Дни:* {daysText}\n" +
                    $" *Описание:* {taskMessage}",
                    ParseMode.Markdown,
                    replyMarkup: GetButtons()
                );
            }
            else
            {
                SendMessage(message.Chat.Id, 2);
            }
        }

        private bool TryParseRepeatTask(string input, out TimeSpan time, out List<DayOfWeek> days)
        {
            time = TimeSpan.Zero;
            days = new List<DayOfWeek>();

            try
            {
                var parts = input.Split(' ');
                if (parts.Length < 2) return false;

                if (!TimeSpan.TryParse(parts[0], out time))
                {
                    var timeParts = parts[0].Split(':');
                    if (timeParts.Length != 2) return false;
                    if (!int.TryParse(timeParts[0], out int hours) || !int.TryParse(timeParts[1], out int minutes))
                        return false;

                    if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                        return false;

                    time = new TimeSpan(hours, minutes, 0);
                }

                var daysPart = string.Join(" ", parts.Skip(1));
                var dayTokens = daysPart.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var token in dayTokens)
                {
                    var upperToken = token.ToUpper();
                    if (DayMapping.ContainsKey(upperToken))
                    {
                        if (!days.Contains(DayMapping[upperToken]))
                        {
                            days.Add(DayMapping[upperToken]);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                return days.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task HandleUpdateAsync(
          ITelegramBotClient client,
          Update update,
          CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
                GetMessages(update.Message);
            else if (update.Type == UpdateType.CallbackQuery)
            {
                CallbackQuery query = update.CallbackQuery;
                Users User = Users.Find(x => x.IdUser == query.Message.Chat.Id);

                UsersSaved($"Callback: {query.Data}", query.Message.Chat.Username);

                if (User == null || User.Events.Count == 0)
                {
                    await TelegramBotClient.SendMessage(
                        query.Message.Chat.Id,
                        Messages[3],
                        replyMarkup: GetButtons()
                    );
                    return;
                }

                string callbackData = query.Data;

                if (callbackData.StartsWith("delete_"))
                {
                    string indexStr = callbackData.Replace("delete_", "");
                    if (int.TryParse(indexStr, out int taskIndex) && taskIndex >= 0 && taskIndex < User.Events.Count)
                    {
                        var removedTask = User.Events[taskIndex];
                        User.Events.RemoveAt(taskIndex);

                        await TelegramBotClient.AnswerCallbackQuery(
                            query.Id,
                            "Задача удалена! "
                        );

                        UsersSaved($"Удалена задача: {removedTask.Time:HH:mm dd.MM.yyyy}", query.Message.Chat.Username);

                        await TelegramBotClient.SendMessage(
                            query.Message.Chat.Id,
                            $" *Задача удалена:*\n\n" +
                            $" {removedTask.Time:HH:mm dd.MM.yyyy}\n" +
                            $" {removedTask.Message}",
                            ParseMode.Markdown,
                            replyMarkup: GetButtons()
                        );

                        await TelegramBotClient.DeleteMessage(
                            query.Message.Chat.Id,
                            query.Message.MessageId
                        );
                    }
                }
            }
        }

        private async Task HandleErrorAsync(
            ITelegramBotClient client,
            Exception exception,
            HandleErrorSource source,
            CancellationToken token
        )
        {
            Console.WriteLine("Ошибка: " + exception.Message);
        }

        public async void Tick(object obj)
        {
            DateTime currentTime = DateTime.Now;

            foreach (Users User in Users)
            {
                for (int i = User.Events.Count - 1; i >= 0; i--)
                {
                    if (User.Events[i].Time <= currentTime)
                    {
                        UsersSaved($"Отправлено напоминание: {User.Events[i].Message}", User.IdUser.ToString());

                        await TelegramBotClient.SendMessage(
                            User.IdUser,
                            $"⏰ *Напоминание!*\n\n" +
                            $"📝 {User.Events[i].Message}",
                            ParseMode.Markdown,
                            replyMarkup: GetButtons()
                        );
                        User.Events.RemoveAt(i);
                    }
                }
            }

            foreach (Users User in Users)
            {
                for (int i = 0; i < User.RepeatEvents.Count; i++)
                {
                    var repeatTask = User.RepeatEvents[i];

                    if (repeatTask.Days.Contains(currentTime.DayOfWeek))
                    {
                        DateTime notificationTime = new DateTime(
                            currentTime.Year,
                            currentTime.Month,
                            currentTime.Day,
                            repeatTask.Time.Hours,
                            repeatTask.Time.Minutes,
                            0);

                        if (currentTime >= notificationTime &&
                            (!repeatTask.LastNotification.HasValue ||
                             repeatTask.LastNotification.Value.Date < currentTime.Date))
                        {
                            string daysText = FormatDays(repeatTask.Days);

                            UsersSaved($"Отправлено повторяющееся напоминание: {repeatTask.Message}", User.IdUser.ToString());

                            await TelegramBotClient.SendMessage(
                                User.IdUser,
                                $" *Повторяющееся напоминание!*\n\n" +
                                $" {repeatTask.Message}\n\n" +
                                $" *Время:* {repeatTask.Time:hh\\:mm}\n" +
                                $" *Дни:* {daysText}",
                                ParseMode.Markdown,
                                replyMarkup: GetButtons()
                            );

                            repeatTask.LastNotification = currentTime;
                        }
                    }
                }
            }
        }

        private string FormatDays(List<DayOfWeek> days)
        {
            var dayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "ПН" },
                { DayOfWeek.Tuesday, "ВТ" },
                { DayOfWeek.Wednesday, "СР" },
                { DayOfWeek.Thursday, "ЧТ" },
                { DayOfWeek.Friday, "ПТ" },
                { DayOfWeek.Saturday, "СБ" },
                { DayOfWeek.Sunday, "ВС" }
            };

            if (days.Count == 7) return "Каждый день";

            return string.Join(", ", days.Select(d => dayNames[d]).OrderBy(d => d));
        }
    }
}