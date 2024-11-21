using Discord.WebSocket;
using Npgsql;

namespace Blady
{
	public interface ICommand
	{
		public string Name { get; }

		Task Define(DiscordSocketClient client);
		Task Initialize(NpgsqlConnection connection);
		Task Run(SocketSlashCommand command);
	}
}
