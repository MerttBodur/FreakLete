using SQLite;

namespace FreakLete.Models;

public class CatalogSeedState
{
	[PrimaryKey]
	public string Key { get; set; } = string.Empty;

	public int Version { get; set; }
}
