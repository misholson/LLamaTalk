using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LLamaBot
{
    public static class LLamaBot
    {
        private static TelegramBotClient botClient;

        [FunctionName("LLamaBot")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string telegramAPIKey = Environment.GetEnvironmentVariable("TelegramAPIKey");

            if (req.Query["key"] != telegramAPIKey)
            {
                log.LogError("Invalid key in URL query string");
                return new BadRequestResult();
            }

            botClient = new TelegramBotClient(telegramAPIKey);

            try
            {
                string body = await req.ReadAsStringAsync();
                if (Environment.GetEnvironmentVariable("LogRequest") == "true")
                {
                    log.LogInformation(body);
                }
                Update update = JsonConvert.DeserializeObject<Update>(body);

                if (update != null)
                {
                    Message message = update.Message;

                    // The day of the message.
                    DateTime date = message.Date.Date;

                    // The channel/group the bot is in.
                    long channelId = message.Chat.Id;

                    // Here's the text. Should parse out possible commands.
                    string text = message.Text;
                    string botName = Environment.GetEnvironmentVariable("BotName");

                    if (!string.IsNullOrEmpty(text))
                    {
                        var match = Regex.Match(text, $"\\/(\\w+)({botName})?(\\s+(.*))?");

                        if (match.Success)
                        {
                            string command = match.Groups[1].Value.ToLower();
                            string parameter = match.Groups.Count > 3 ? match.Groups[3].Value : null;
                            string[] parameters = parameter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            switch (command)
                            {
                                case "score":
                                    await RecordScore(channelId, parameters, message.From.Id, message.From.FirstName, message.From.LastName);
                                    break;
                                case "help":
                                case "start":
                                    await SendHelp(channelId);
                                    break;
                                case "status":
                                    await SendStatus(channelId);
                                    break;
                                case "settings":
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new StatusCodeResult(500);
            }
            return new OkObjectResult($"Success");
        }

        public static async Task RecordScore(long groupId, string[] parameters, int userId, string firstName, string lastName)
        {
            // This gets a request for number of users in a channel. Subtract one for the bot.
            int usersCount = await botClient.GetChatMembersCountAsync(new ChatId(groupId)) - 1;

            string parameter = "";
            if (parameters.Length > 0)
            {
                parameter = parameters[0];
            }

            LLamaTable table = new LLamaTable();
            if (parameter.ToLower() == "delete")
            {
                await table.DeleteScore(groupId, userId);
            }
            else if (int.TryParse(parameter, out int score))
            {
                if (score < 0 || score > 6)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: groupId,
                        text: $"Score must be from 0-6. (invalid score {parameter})"
                        );
                }
                else
                {
                    await table.AddScore(groupId, userId, score);
                    var results = await table.GetStatus(groupId);
                    if (results.Count >= usersCount)
                    {
                        await SendStatusMessage(results, groupId);
                    }
                    else if (results.Count == usersCount - 1)
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: groupId,
                            text: $"{results.Count} out of {usersCount} players have submitted. One more submission remaining!"
                            );
                    }
                }
            }
        }

        public static async Task SendHelp(long channelId)
        {
            await botClient.SendTextMessageAsync(
                chatId: channelId,
                text: @"Welcome to LLama Talk. This bot will help you chat about Learned League without inadvertently cheating.

This bot supports the following commands:
/score N - Submit the number of answers you got correct today (0-6). This will record your correct answers for the day. Once all members of the group have submitted a score, a message will be sent to the group indicating that you are free to discuss the answers.
/status - See who has submitted a score so far today.
"
                );
        }

        public static async Task SendStatus(long groupId)
        {
            LLamaTable table = new LLamaTable();
            var results = await table.GetStatus(groupId);
            await SendStatusMessage(results, groupId);
        }

        public static async Task SendStatusMessage(List<LLamaEntity> entities, long groupId, int usersCount = -1)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Learned League Status for {LLamaTable.Today.ToString("dddd, MMMM dd")}");
            List<Task<ChatMember>> chatMemberTasks = new List<Task<ChatMember>>();
            foreach (var entity in entities)
            {
                chatMemberTasks.Add(GetChatMember(groupId, entity));
            }
            ChatMember[] chatMembers = await Task.WhenAll<ChatMember>(chatMemberTasks);

            if (usersCount < 0)
            {
                usersCount = await botClient.GetChatMembersCountAsync(new ChatId(groupId)) - 1;
            }

            for (int i=0; i < entities.Count; i++)
            {
                int score = entities[i].Score;
                ChatMember chatMember = chatMembers[i];

                if (chatMember != null)
                {
                    sb.AppendLine($"  {chatMember.User.FirstName} {chatMember.User.LastName} - {score}");
                }
                else
                {
                    sb.AppendLine($"  User {entities[i].UserId} - {score}");
                }
            }

            if (entities.Count >= usersCount)
            {
                sb.AppendLine("");
                sb.AppendLine("All users have submitted. You can now discuss today's questions!");
            }


            await botClient.SendTextMessageAsync(
                chatId: groupId,
                text: sb.ToString()
                );
        }

        private static async Task<ChatMember> GetChatMember(long groupId, LLamaEntity entity)
        {
            try
            {
                return await botClient.GetChatMemberAsync(chatId: groupId, userId: entity.UserId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
