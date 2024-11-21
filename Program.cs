namespace Blady
{
	class Program
	{
		static async Task Main(string[] args)
		{
			bool redefine = false, devMode = false;

			for (int i = 0; i < args.Length; i++)
			{
				switch(args[i])
				{
					case "-r": redefine = true; break;
					case "-d": devMode = true; break;
				}
			}
			await Bot.Start(redefine, devMode);
			await Task.Delay(Timeout.Infinite);
		}
	}
}
