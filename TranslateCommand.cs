using Discord;
using Discord.WebSocket;

namespace Blady
{
	public class TranslateCommand : ICommand
	{
		public string Name { get; } = "translate";
		private enum Types
		{
			States = 1,
			Items,
			Spells,
			Monsters,
			Dungeons
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
					.AddChoice("states", (int)Types.States)
					.AddChoice("items", (int)Types.Items)
					.AddChoice("spells", (int)Types.Spells)
					.AddChoice("monsters", (int)Types.Monsters)
					.AddChoice("dungeons", (int)Types.Dungeons)
					.WithType(ApplicationCommandOptionType.Integer))
				.AddOption("text", ApplicationCommandOptionType.String, "The text you want to translate.");

			await client.CreateGlobalApplicationCommandAsync(builder.Build());
		}

		public async Task Run(SocketSlashCommand command)
		{
			await command.RespondAsync("This command wasn't implemented yet.");
		}
	}
}
