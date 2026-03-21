using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using GymTracker.Models;

namespace GymTracker.Services;

public static class ExerciseCatalog
{
	private const string CatalogResourceName = "GymTracker.Resources.Raw.exercise_catalog.json";

	public const string Push = "Push";
	public const string Pull = "Pull";
	public const string SquatVariation = "Squat Variation";
	public const string DeadliftVariation = "Deadlift Variation";
	public const string Sprint = "Sprint";
	public const string Jumps = "Jumps";
	public const string Plyometrics = "Plyometrics";
	public const string OlympicLifts = "Olympic Lifts";

	public static IReadOnlyList<string> Categories { get; } =
	[
		Push,
		Pull,
		SquatVariation,
		DeadliftVariation,
		Sprint,
		Jumps,
		Plyometrics,
		OlympicLifts
	];

	private static readonly Lazy<IReadOnlyList<ExerciseCatalogItem>> _items = new(LoadItems);

	public static IReadOnlyList<ExerciseCatalogItem> GetAllItems()
	{
		return _items.Value;
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetItemsByCategory(string category)
	{
		return _items.Value
			.Where(item => item.Category == category)
			.OrderBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.ToList();
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetRecommendedItemsByCategory(string category, int take = 20)
	{
		return _items.Value
			.Where(item => item.Category == category)
			.OrderBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.Take(take)
			.ToList();
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetItemsByCategories(IEnumerable<string> categories)
	{
		HashSet<string> allowed = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return _items.Value
			.Where(item => allowed.Contains(item.Category))
			.OrderBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.ToList();
	}

	public static ExerciseCatalogItem? GetByName(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		return _items.Value.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
	}

	public static ExerciseCatalogItem? GetByNameAndCategory(string? name, string? category)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		return _items.Value.FirstOrDefault(item =>
			string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase) &&
			(string.IsNullOrWhiteSpace(category) || string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)));
	}

	private static IReadOnlyList<ExerciseCatalogItem> LoadItems()
	{
		Assembly assembly = typeof(ExerciseCatalog).Assembly;
		using Stream? stream = assembly.GetManifestResourceStream(CatalogResourceName);
		if (stream is null)
		{
			throw new InvalidOperationException($"Embedded exercise catalog not found: {CatalogResourceName}");
		}

		using var reader = new StreamReader(stream);
		string json = reader.ReadToEnd();

		JsonSerializerOptions options = new()
		{
			PropertyNameCaseInsensitive = true
		};
		options.Converters.Add(new JsonStringEnumConverter());

		CatalogPayload? payload = JsonSerializer.Deserialize<CatalogPayload>(json, options);
		if (payload?.Items is null || payload.Items.Count == 0)
		{
			throw new InvalidOperationException("Exercise catalog JSON was empty.");
		}

		return payload.Items
			.OrderBy(item => item.Category)
			.ThenBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.ToList();
	}

	private sealed class CatalogPayload
	{
		public int Version { get; set; }

		public List<ExerciseCatalogItem> Items { get; set; } = [];
	}
}
