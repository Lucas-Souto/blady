using Discord;
using Discord.WebSocket;
using Npgsql;

namespace Blady
{
	public interface ICommand
	{
		public string Name { get; }

		SlashCommandBuilder Define();
		Task Initialize(NpgsqlConnection connection);
		Task Run(SocketSlashCommand command, NpgsqlConnection connection);
	}
}
