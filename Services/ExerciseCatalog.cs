using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FreakLete.Models;

namespace FreakLete.Services;

public static class ExerciseCatalog
{
	private const string CatalogResourceName = "FreakLete.Resources.Raw.exercise_catalog.json";
	public const string CatalogStateKey = "exercise_catalog";

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

	private static readonly Lazy<CatalogPayload> _payload = new(LoadPayload);

	public static int Version => _payload.Value.Version <= 0 ? 1 : _payload.Value.Version;

	public static IReadOnlyList<ExerciseCatalogItem> GetAllItems()
	{
		return _payload.Value.Items;
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetItemsByCategory(string category)
	{
		return _payload.Value.Items
			.Where(item => item.Category == category)
			.OrderBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.ToList();
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetRecommendedItemsByCategory(string category, int take = 20)
	{
		return _payload.Value.Items
			.Where(item => item.Category == category)
			.OrderBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.Take(take)
			.ToList();
	}

	public static IReadOnlyList<ExerciseCatalogItem> SearchItemsByCategory(string category, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return GetRecommendedItemsByCategory(category);
		}

		string trimmedQuery = query.Trim();
		return _payload.Value.Items
			.Where(item =>
				item.Category == category &&
				(ContainsIgnoreCase(item.Name, trimmedQuery) ||
				 ContainsIgnoreCase(item.DisplayName, trimmedQuery) ||
				 ContainsIgnoreCase(item.EnglishName, trimmedQuery) ||
				 ContainsIgnoreCase(item.TurkishName, trimmedQuery) ||
				 item.PrimaryMuscles.Any(muscle => ContainsIgnoreCase(muscle, trimmedQuery)) ||
				 item.SecondaryMuscles.Any(muscle => ContainsIgnoreCase(muscle, trimmedQuery))))
			.OrderBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.ToList();
	}

	public static IReadOnlyList<ExerciseCatalogItem> GetItemsByCategories(IEnumerable<string> categories)
	{
		HashSet<string> allowed = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return _payload.Value.Items
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

		return _payload.Value.Items.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
	}

	public static ExerciseCatalogItem? GetByNameAndCategory(string? name, string? category)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		return _payload.Value.Items.FirstOrDefault(item =>
			string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase) &&
			(string.IsNullOrWhiteSpace(category) || string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)));
	}

	private static bool ContainsIgnoreCase(string? text, string query)
	{
		return !string.IsNullOrWhiteSpace(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	private static CatalogPayload LoadPayload()
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

		payload.Items = payload.Items
			.OrderBy(item => item.Category)
			.ThenBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.ToList();

		if (payload.Version <= 0)
		{
			payload.Version = 1;
		}

		return payload;
	}

	private sealed class CatalogPayload
	{
		public int Version { get; set; }

		public List<ExerciseCatalogItem> Items { get; set; } = [];
	}
}
