namespace Blady
{
	public enum WakfuOptions
	{
		States = 1,
		Items,
		Monsters
	}

	public struct WakfuConfig
	{
		public string version;

		public WakfuConfig() => version = "";
	}

	public struct WakfuLocalizedString
	{
		public string fr, en, es, pt;

		public WakfuLocalizedString() => fr = en = es = pt = "";
	}

	public struct WakfuReference
	{
		public WakfuLocalizedString? title, description;
	}
}
