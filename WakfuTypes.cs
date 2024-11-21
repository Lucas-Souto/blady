namespace Blady
{
	public enum WakfuOptions
	{
		States = 1,
		Items,
		Spells,
		Monsters,
		Dungeons,
		Places
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
		public WakfuLocalizedString title;
		public WakfuLocalizedString? description;
	}
}
