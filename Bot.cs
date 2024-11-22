using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using Npgsql;

namespace Blady
{
	public static class Bot
	{
		private static bool started, redefine, devMode;
		private static Dictionary<string, string> access = new();
		private static Dictionary<string, ICommand> commands = new();
		private static DiscordSocketClient client;
		private static NpgsqlConnection connection;

		private static async Task AddCommand(ICommand command)
		{
			await command.Initialize(connection);
			commands.Add(command.Name, command);
		}
		public static async Task Start(bool redefine, bool devMode)
		{
			if (!started)
			{
				started = true;
				Bot.redefine = redefine;
				Bot.devMode = devMode;
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

				connection = new NpgsqlConnection(string.Format("Host={0};Username={1};Password={2};Database={3}",
					"localhost", access["db_login"], access["db_password"], access[devMode ? "db_dev" : "db"]));

				await connection.OpenAsync();

				client.Log += Log;
				client.Ready += Ready;
				client.SlashCommandExecuted += SlashHandler;

				await client.LoginAsync(TokenType.Bot, access[devMode ? "token_dev" : "token"]);
				await client.StartAsync();

				Console.WriteLine("Creating commands internally...");
				await AddCommand(new TranslateCommand());
				Console.WriteLine("Commands created!");
			}
		}

		private static Task Log(LogMessage log)
		{
			Console.WriteLine(log.ToString());

			return Task.CompletedTask;
		}

		private static async Task Ready()
		{
			try
			{
				if (redefine)
				{
					Console.WriteLine("Defining commands on Discord...");

					IReadOnlyCollection<SocketApplicationCommand> remove = await client.GetGlobalApplicationCommandsAsync();

					foreach (var command in remove) await command.DeleteAsync();

					foreach (var command in commands) await command.Value.Define(client);

					Console.WriteLine("Commands defined!");

					redefine = false;
				}
			}
			catch (HttpException exception)
			{
				Console.WriteLine(JsonConvert.SerializeObject(exception.Errors, Formatting.Indented));
			}
		}

		private static async Task SlashHandler(SocketSlashCommand command)
		{
			if (commands.TryGetValue(command.Data.Name, out ICommand commandDefinition)) await commandDefinition.Run(command, connection);
			else await command.RespondAsync("Hm... that command doesn't exist anymore. Try reloading the app! :D");
		}
	}
}
