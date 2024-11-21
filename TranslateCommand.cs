using System.Net;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Npgsql;

namespace Blady
{
	public class TranslateCommand : ICommand
	{
		public string Name { get; } = "translate";
		private const string VersionFile = "last_version",
			CreateCommand = @"CREATE TABLE IF NOT EXISTS translations (
				type VARCHAR(20) NOT NULL,
				title_fr TEXT NOT NULL,
				title_en TEXT NOT NULL,
				title_es TEXT NOT NULL,
				title_pt TEXT NOT NULL,
				description_fr TEXT,
				description_en TEXT,
				description_es TEXT,
				description_pt TEXT
			)",
			TruncateCommand = "TRUNCATE TABLE translations";
		private HttpClient wakfuCdn = new()
		{
			BaseAddress = new Uri("https://wakfu.cdn.ankama.com/gamedata/")
		};

		private async Task CreateTable(NpgsqlConnection connection)
		{
			await using (NpgsqlCommand command = new(CreateCommand, connection)) await command.ExecuteNonQueryAsync();
		}
		private async Task PopulateTable(NpgsqlConnection connection)
		{
			await using (NpgsqlCommand truncate = new(TruncateCommand, connection)) await truncate.ExecuteNonQueryAsync();
		}
		public async Task Initialize(NpgsqlConnection connection)
		{
			await CreateTable(connection);

			using (HttpResponseMessage response = await wakfuCdn.GetAsync("config.json"))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					WakfuConfig config = JsonConvert.DeserializeObject<WakfuConfig>(await response.Content.ReadAsStringAsync());
					bool shouldUpdate = !File.Exists(VersionFile) || File.ReadAllText(VersionFile) != config.version;

					if (shouldUpdate)
					{
						await PopulateTable(connection);
						File.WriteAllText(VersionFile, config.version);
					}
				}
				else Console.WriteLine("Error {0} while getting the current JSON data version.", response.StatusCode);
			}
		}

		public async Task Define(DiscordSocketClient client)
		{
			SlashCommandBuilder builder = new();

			builder.WithName(Name)
				.WithDescription("Get's the translations of an in-game thing.")
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("type")
					.WithDescription("The category of the thing you want to translate.")
					.WithRequired(true)
					.AddChoice("states", (int)WakfuOptions.States)
					.AddChoice("items", (int)WakfuOptions.Items)
					.AddChoice("spells", (int)WakfuOptions.Spells)
					.AddChoice("monsters", (int)WakfuOptions.Monsters)
					.AddChoice("dungeons", (int)WakfuOptions.Dungeons)
					.AddChoice("places", (int)WakfuOptions.Places)
					.WithType(ApplicationCommandOptionType.Integer))
				.AddOption("text", ApplicationCommandOptionType.String, "The text you want to translate.", isRequired: true);

			await client.CreateGlobalApplicationCommandAsync(builder.Build());
		}

		public async Task Run(SocketSlashCommand command)
		{
			await command.RespondAsync("This command wasn't implemented yet.");
		}
	}
}
