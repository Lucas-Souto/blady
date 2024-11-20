using Discord.WebSocket;

namespace Blady
{
	public interface ICommand
	{
		public string Name { get; }

		Task Define(DiscordSocketClient client);
		Task Run(SocketSlashCommand command);
	}
}
