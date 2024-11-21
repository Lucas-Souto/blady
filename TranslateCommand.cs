using System.Net;
using System.Data;
using System.Data.Common;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Npgsql;

namespace Blady
{
	public class TranslateCommand : ICommand
	{
		public string Name { get; } = "translate";
		private const string VersionFile = "last_version";
		private string version = "";
		private HttpClient wakfuCdn = new()
		{
			BaseAddress = new Uri("https://wakfu.cdn.ankama.com/gamedata/")
		};

		private async Task CreateTable(NpgsqlConnection connection)
		{
			await using (NpgsqlCommand command = new(@"CREATE TABLE IF NOT EXISTS translations (
					type INT NOT NULL,
					title_fr TEXT,
					title_en TEXT,
					title_es TEXT,
					title_pt TEXT,
					description_fr TEXT,
					description_en TEXT,
					description_es TEXT,
					description_pt TEXT
				);
				CREATE EXTENSION IF NOT EXISTS pg_trgm;", connection)) await command.ExecuteNonQueryAsync();
		}
		private NpgsqlCommand CreateInsertCommand(NpgsqlConnection connection)
		{
			NpgsqlCommand insertCommand = new(@"INSERT INTO translations
				(
					type,
					title_fr, title_en, title_es, title_pt,
					description_fr, description_en, description_es, description_pt
				) VALUES(@type, @tfr, @ten, @tes, @tpt, @dfr, @den, @des, @dpt)", connection);
			NpgsqlParameter type = new("type", DbType.Int32),
				tfr = new("tfr", DbType.String),
				ten = new("ten", DbType.String),
				tes = new("tes", DbType.String),
				tpt = new("tpt", DbType.String),
				dfr = new("dfr", DbType.String),
				den = new("den", DbType.String),
				des = new("des", DbType.String),
				dpt = new("dpt", DbType.String);

			insertCommand.Parameters.Add(type);
			insertCommand.Parameters.Add(tfr);
			insertCommand.Parameters.Add(ten);
			insertCommand.Parameters.Add(tes);
			insertCommand.Parameters.Add(tpt);
			insertCommand.Parameters.Add(dfr);
			insertCommand.Parameters.Add(den);
			insertCommand.Parameters.Add(des);
			insertCommand.Parameters.Add(dpt);
			insertCommand.Prepare();

			return insertCommand;
		}
		private NpgsqlCommand CreateDeleteCommand(NpgsqlConnection connection)
		{
			NpgsqlCommand deleteCommand = new("DELETE FROM translations WHERE type = @t;", connection);

			deleteCommand.Parameters.Add(new("t", DbType.Int32));
			deleteCommand.Prepare();

			return deleteCommand;
		}
		private async Task PopulateTable(NpgsqlConnection connection)
		{
			IEnumerable<WakfuOptions> options = Enum.GetValues(typeof(WakfuOptions)).Cast<WakfuOptions>();
			NpgsqlCommand insertCommand = CreateInsertCommand(connection), deleteCommand = CreateDeleteCommand(connection);

			foreach (WakfuOptions option in options)
			{
				using (HttpResponseMessage response = await wakfuCdn.GetAsync(string.Format("{0}/{1}.json", version, option.ToString().ToLower())))
				{
					if (response.StatusCode != HttpStatusCode.Forbidden && response.StatusCode != HttpStatusCode.NotFound)// The cdn returns 403 when the route doesn't exist
					{
						WakfuReference[] references = JsonConvert.DeserializeObject<WakfuReference[]>((await response.Content.ReadAsStringAsync()));

						deleteCommand.Parameters["t"].Value = (int)option;

						await deleteCommand.ExecuteNonQueryAsync();

						foreach (WakfuReference reference in references)
						{
							insertCommand.Parameters["type"].Value = (int)option;
							insertCommand.Parameters["tfr"].Value = reference.title.HasValue ? reference.title.Value.fr : DBNull.Value;
							insertCommand.Parameters["ten"].Value = reference.title.HasValue ? reference.title.Value.en : DBNull.Value;
							insertCommand.Parameters["tes"].Value = reference.title.HasValue ? reference.title.Value.es : DBNull.Value;
							insertCommand.Parameters["tpt"].Value = reference.title.HasValue ? reference.title.Value.pt : DBNull.Value;
							insertCommand.Parameters["dfr"].Value = reference.description.HasValue ? reference.description.Value.fr : DBNull.Value;
							insertCommand.Parameters["den"].Value = reference.description.HasValue ? reference.description.Value.en : DBNull.Value;
							insertCommand.Parameters["des"].Value = reference.description.HasValue ? reference.description.Value.es : DBNull.Value;
							insertCommand.Parameters["dpt"].Value = reference.description.HasValue ? reference.description.Value.pt : DBNull.Value;

							await insertCommand.ExecuteNonQueryAsync();
						}
					}
				}
			}
		}
		public async Task Initialize(NpgsqlConnection connection)
		{
			await CreateTable(connection);

			if (File.Exists(VersionFile)) version = File.ReadAllText(VersionFile);

			using (HttpResponseMessage response = await wakfuCdn.GetAsync("config.json"))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					WakfuConfig config = JsonConvert.DeserializeObject<WakfuConfig>(await response.Content.ReadAsStringAsync());
					bool shouldUpdate = version != config.version;

					if (shouldUpdate)
					{
						version = config.version;

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

		public async Task Run(SocketSlashCommand command, NpgsqlConnection connection)
		{
			await command.DeferAsync();

			EmbedBuilder builder = new();

			builder.Color = new(91, 68, 45);

			await using (NpgsqlCommand searchCommand = new(@"SELECT type,
				title_fr, title_en, title_es, title_pt,
				description_fr, description_en, description_es, description_pt
				FROM
					(SELECT *, similarity(lower(title_fr), lower(@term)) AS sfr,
						similarity(lower(title_en), lower(@term)) AS sen,
						similarity(lower(title_es), lower(@term)) AS ses,
						similarity(lower(title_pt), lower(@term)) AS spt
						FROM translations) subquery
				WHERE type = @type AND (sfr > .5 OR sen > .5 OR ses > .5 OR spt > .5)
				ORDER BY greatest(sfr, sen, ses, spt) DESC
				LIMIT 1", connection))
			{
				foreach (SocketSlashCommandDataOption option in command.Data.Options)
				{
					switch (option.Name)
					{
						case "type": searchCommand.Parameters.AddWithValue("type", option.Value); break;
						case "text": searchCommand.Parameters.AddWithValue("term", option.Value); break;
					}
				}

				DbDataReader reader = await searchCommand.ExecuteReaderAsync();
				
				if (!reader.HasRows)
				{
					builder.Title = "Not found!";
					builder.Description = string.Format("`{0}` isn't on my database yet... :/\nGet in touch with the dev! :D", searchCommand.Parameters["term"].Value);
				}
				else while (reader.Read())
				{
					builder.Title = string.Format("{0}:", ((WakfuOptions)reader.GetInt32(0)).ToString());
					builder.Description = "That was the closest one I found.";

					builder.AddField(String.Format("[FR] {0}:", reader.GetString(1)), !reader.IsDBNull(5) ? reader.GetString(5) : "N/A");
					builder.AddField(String.Format("[EN] {0}:", reader.GetString(2)), !reader.IsDBNull(6) ? reader.GetString(6) : "N/A");
					builder.AddField(String.Format("[ES] {0}:", reader.GetString(3)), !reader.IsDBNull(7) ? reader.GetString(7) : "N/A");
					builder.AddField(String.Format("[PT] {0}:", reader.GetString(4)), !reader.IsDBNull(8) ? reader.GetString(8) : "N/A");
				}

				await reader.CloseAsync();
			}

			await command.ModifyOriginalResponseAsync((m) => m.Embed = builder.Build());
		}
	}
}
