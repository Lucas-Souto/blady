using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace Blady
{
	public static class Bot
	{
		private static bool started, redefine;
		private static Dictionary<string, string> access = new();
		private static Dictionary<string, ICommand> commands = new();
		private static DiscordSocketClient client;

		public static async Task Start(bool redefine)
		{
			if (!started)
			{
				started = true;
				Bot.redefine = redefine;
				DiscordSocketConfig config = new() { GatewayIntents = GatewayIntents.None };
				client = new(config);

				if (access.Count == 0)
				{
					string[] split;

					foreach (string item in File.ReadAllLines(".access"))
					{
						split = item.Split(':');

						access.Add(split[0], split[1]);
					}
				}

				client.Log += Log;
				client.Ready += Ready;
				client.SlashCommandExecuted += SlashHandler;

				await client.LoginAsync(TokenType.Bot, access["token"]);
				await client.StartAsync();
			}
		}

		private static Task Log(LogMessage log)
		{
			Console.WriteLine(log.ToString());

			return Task.CompletedTask;
		}

		private static async Task AddCommand(ICommand command)
		{
			if (redefine) await command.Define(client);

			commands.Add(command.Name, command);
		}
		private static async Task Ready()
		{
			try
			{
				if (redefine)
				{
					redefine = true;

					IReadOnlyCollection<SocketApplicationCommand> remove = await client.GetGlobalApplicationCommandsAsync();

					foreach (var command in remove) await command.DeleteAsync();

					Console.WriteLine("Commands defined!");
				}

				await AddCommand(new TranslateCommand());
			}
			catch (HttpException exception)
			{
				Console.WriteLine(JsonConvert.SerializeObject(exception.Errors, Formatting.Indented));
			}
		}

		private static async Task SlashHandler(SocketSlashCommand command)
		{
			if (commands.TryGetValue(command.Data.Name, out ICommand commandDefinition)) await commandDefinition.Run(command);
			else await command.RespondAsync("Hm... that command doesn't exist anymore. Try reloading the app! :D");
		}
	}
}
