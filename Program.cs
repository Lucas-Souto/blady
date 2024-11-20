namespace Blady
{
	class Program
	{
		static async Task Main(string[] args)
		{
			await Bot.Start(args.Length > 0 && args[0] == "-r");
			await Task.Delay(Timeout.Infinite);
		}
	}
}
