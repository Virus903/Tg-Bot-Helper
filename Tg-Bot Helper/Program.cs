using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot; // Основные классы для работы с Telegram Bot API
using Telegram.Bot.Polling; // Для обработки обновлений и ошибок (Polling)
using Telegram.Bot.Types; // Типы данных Telegram (например, Update, Message)
using Telegram.Bot.Types.Enums; // Перечисления, например, тип сообщения (MessageType)
using Telegram.Bot.Types.ReplyMarkups; // Для создания клавиатур (ReplyKeyboardMarkup)

namespace TelegramTipBot
{
    class Program
    {
        // Токен вашего бота от BotFather
        private const string BotToken = "8124603446:AAEtH6cI7ws7WwQJfC2FkK1t3NF4frNWx9k";

        // Список советов и связанных с ними гифок.
        // Кортеж: текст совета + URL гифки
        private static readonly List<(string Text, string GifUrl)> Tips = new()
        {
            ("Совет 90-х №1:Используй яркие цвета и фоновые GIF!", "https://i.pinimg.com/originals/0f/37/c8/0f37c8e9aa6101b14e91a875ea6366d8.gif"),
            ("Совет 90-х №2:Мерцающая анимация — зажигай!", "https://media.giphy.com/media/v1.Y2lkPTc5MGI3NjExcWpkdTVwM3lpN3BiYXQ3djdyNDlzbjY4dWl2c25mcXQzbXFkbXJpcCZlcD12MV9naWZzX3NlYXJjaCZjdD1n/Stx20aVIMgtArs2VIb/giphy.gif"),
            ("Совет 90-х №3:Не забудь о теге <marquee> для движения текста!", "https://media.giphy.com/media/v1.Y2lkPWVjZjA1ZTQ3Z3Vzd2Fla3FjZXVvdmJ3d3o4MHljbWZrNG9rajJ2a2x3Ync5Z2J3eSZlcD12MV9naWZzX3NlYXJjaCZjdD1n/esFMoBFa9O41xt5Lxf/giphy.gif"),
            ("Совет 90-х №4:Используй HTML таблицы для формирования макета — простота и стиль!", "https://media.geeksforgeeks.org/wp-content/uploads/20210524225221/animatedTable.gif"),
            ("Совет 90-х №5:Используй DEBUG и паузы!", "https://media3.giphy.com/media/v1.Y2lkPTZjMDliOTUyNWMxZHA3YnZtaTk0MTZ6MzFneWs0bTVrcW40OG83eG02bDR5YjJpdCZlcD12MV9naWZzX3NlYXJjaCZjdD1n/efuh1hLg1H438esuwG/200w.gif"),
        };

        // Клиент Telegram Bot API
        private static TelegramBotClient botClient;

        // Словарь для отслеживания текущего индекса совета для каждого пользователя (chatId)
        private static Dictionary<long, int> userTipIndices = new();

        static async Task Main()
        {
            // Создаем клиента с указанным токеном
            botClient = new TelegramBotClient(BotToken);

            // CancellationTokenSource для возможности остановки получения обновлений по необходимости
            using var cts = new CancellationTokenSource();

            // Запускаем получение обновлений (Polling)
            botClient.StartReceiving(
                HandleUpdateAsync, // метод для обработки всех обновлений (сообщений и пр.)
                HandleErrorAsync, // метод для обработки ошибок, которые могут возникнуть
                cancellationToken: cts.Token);

            // Получаем информацию о самом боте (например, его имя)
            var me = await botClient.GetMe();
            Console.WriteLine($"Бот @{me.Username} запущен.");

            // Чтобы приложение не закрывалось сразу, ожидаем ввода пользователя в консоли
            Console.ReadLine();

            // При закрытии программы останавливаем прием сообщений
            cts.Cancel();
        }


        // Метод-обработчик обновлений (Messages)
        private static async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {

            // Проверяем, что в обновлении есть сообщение и это именно текстовое сообщение
            if (update.Message == null || update.Message.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id; // Уникальный идентификатор чата для ответа
            var text = update.Message.Text.Trim(); // Текст сообщения, без лишних пробелов

            if (text == "/start")
            {
                // Если пользователь отправил команду /start, приветствуем его и показываем клавиатуру
                await client.SendMessage(chatId, "Привет,Будующий Програмист из 90-х! Нажми кнопку /tip, чтобы получить 5 советов для создания сайта!", replyMarkup: GetTipKeyboard(), cancellationToken: token); // Добавляем клавиатуру с кнопкой /tip
            }
            else if (text == "/tip")
            {
                // Если команда /tip, отправляем следующий совет с гифкой
                await SendNextTip(chatId, token);
            }
            else
            {
                // Если пришло любое другое сообщение, объясняем, как использовать бота
                await client.SendMessage(chatId, "Используй команду /tip чтобы получить 5 советов для создания сайта!", replyMarkup: GetTipKeyboard(), cancellationToken: token);
            }
        }

        // Метод для создания клавиатуры с одной кнопкой "/tip"
        private static ReplyKeyboardMarkup GetTipKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                // Создаем строку с одной кнопкой
                new KeyboardButton[] { "/tip" }
            })
            {
                ResizeKeyboard = true, // Клавиатура подстраивается по размеру под экран пользователя
                OneTimeKeyboard = false, // Клавиатура не исчезает после нажатия
            };
        }

        // Метод, который отправляет пользователю следующий совет с гифкой
        private static async Task SendNextTip(long chatId, CancellationToken token)
        {
            // Смотрим, какой совет последний раз отправляли этому пользователю
            int index = 0;
            if (userTipIndices.TryGetValue(chatId, out var idx))
            {
                index = idx;
            }

            var (text, gifUrl) = Tips[index];
            // Отправляем текст совета
            await botClient.SendMessage(chatId, text, cancellationToken: token);

            // Отправляем гифку (анимацию) с подписью
            await botClient.SendAnimation(chatId, gifUrl, caption: "гифка,описывающая совет", cancellationToken: token);

            // Обновляем индекс — следующий совет в списке, возвращаясь к первому после последнего
            index = (index + 1) % Tips.Count;
            userTipIndices[chatId] = index;
        }

        // Метод, который вызывается при возникновении ошибок в процессе получения обновлений
        private static Task HandleErrorAsync(ITelegramBotClient client, Exception exception, HandleErrorSource errorSource, CancellationToken cancellationToken)
        {
            // Просто выводим сообщение об ошибке в консоль
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}