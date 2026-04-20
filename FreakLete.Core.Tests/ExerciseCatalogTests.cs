using FreakLete.Services;

namespace FreakLete.Core.Tests;

public class ExerciseCatalogTests
{
	private const string MediaBaseUrl = "https://pub-e77340f896224d2a83b6c37ccdd6aabe.r2.dev/";

	[Fact]
	public void Categories_ContainsExpectedSections()
	{
		Assert.Equal(8, ExerciseCatalog.Categories.Count);
		Assert.Contains(ExerciseCatalog.Push, ExerciseCatalog.Categories);
		Assert.Contains(ExerciseCatalog.Pull, ExerciseCatalog.Categories);
		Assert.Contains(ExerciseCatalog.SquatVariation, ExerciseCatalog.Categories);
		Assert.Contains(ExerciseCatalog.DeadliftVariation, ExerciseCatalog.Categories);
		Assert.Contains(ExerciseCatalog.Sprint, ExerciseCatalog.Categories);
		Assert.Contains(ExerciseCatalog.Jumps, ExerciseCatalog.Categories);
		Assert.Contains(ExerciseCatalog.Plyometrics, ExerciseCatalog.Categories);
		Assert.Contains(ExerciseCatalog.OlympicLifts, ExerciseCatalog.Categories);
	}

	[Fact]
	public void GetAllItems_ReturnsCatalogItems_AndPositiveVersion()
	{
		IReadOnlyList<Models.ExerciseCatalogItem> items = ExerciseCatalog.GetAllItems();

		Assert.True(ExerciseCatalog.Version >= 1);
		Assert.NotEmpty(items);
		Assert.Contains(items, item => item.Name == "Conventional Deadlift");
	}

	[Fact]
	public void GetByName_IsCaseInsensitive()
	{
		Models.ExerciseCatalogItem? item = ExerciseCatalog.GetByName("power clean");

		Assert.NotNull(item);
		Assert.Equal("Power Clean", item!.Name);
		Assert.Equal(ExerciseCatalog.OlympicLifts, item.Category);
	}

	[Fact]
	public void GetByNameAndCategory_WithWrongCategory_ReturnsNull()
	{
		Models.ExerciseCatalogItem? item = ExerciseCatalog.GetByNameAndCategory("Power Clean", ExerciseCatalog.Push);

		Assert.Null(item);
	}

	[Fact]
	public void GetRecommendedItemsByCategory_ReturnsOrderedItemsFromSingleCategory()
	{
		IReadOnlyList<Models.ExerciseCatalogItem> items = ExerciseCatalog.GetRecommendedItemsByCategory(ExerciseCatalog.DeadliftVariation, 20);

		Assert.NotEmpty(items);
		Assert.All(items, item => Assert.Equal(ExerciseCatalog.DeadliftVariation, item.Category));

		List<Models.ExerciseCatalogItem> ordered = items
			.OrderBy(item => item.RecommendedRank)
			.ThenBy(item => item.DisplayName)
			.ToList();

		Assert.Equal(ordered.Select(item => item.Name), items.Select(item => item.Name));
	}

	[Fact]
	public void SearchItemsByCategory_WithEmptyQuery_ReturnsRecommendedItems()
	{
		IReadOnlyList<string> expected = ExerciseCatalog
			.GetRecommendedItemsByCategory(ExerciseCatalog.Jumps)
			.Select(item => item.Name)
			.ToList();

		IReadOnlyList<string> actual = ExerciseCatalog
			.SearchItemsByCategory(ExerciseCatalog.Jumps, "   ")
			.Select(item => item.Name)
			.ToList();

		Assert.Equal(expected, actual);
	}

	[Fact]
	public void SearchItemsByCategory_CanFindItemsByName()
	{
		IReadOnlyList<Models.ExerciseCatalogItem> results = ExerciseCatalog.SearchItemsByCategory(ExerciseCatalog.Jumps, "triple broad");

		Assert.NotEmpty(results);
		Assert.Contains(results, item => item.Name == "Triple Broad Jump");
	}

	[Fact]
	public void SearchItemsByCategory_CanFindItemsByMuscle()
	{
		IReadOnlyList<Models.ExerciseCatalogItem> results = ExerciseCatalog.SearchItemsByCategory(ExerciseCatalog.DeadliftVariation, "hamstrings");

		Assert.NotEmpty(results);
		Assert.All(results, item => Assert.Equal(ExerciseCatalog.DeadliftVariation, item.Category));
	}

	[Fact]
	public void GetItemsByCategories_ReturnsOnlyRequestedCategories()
	{
		HashSet<string> allowed = [ExerciseCatalog.Sprint, ExerciseCatalog.Jumps];
		IReadOnlyList<Models.ExerciseCatalogItem> items = ExerciseCatalog.GetItemsByCategories(allowed);

		Assert.NotEmpty(items);
		Assert.All(items, item => Assert.Contains(item.Category, allowed));
		Assert.Contains(items, item => item.Category == ExerciseCatalog.Sprint);
		Assert.Contains(items, item => item.Category == ExerciseCatalog.Jumps);
	}

	[Fact]
	public void MediaItems_UseR2BaseUrl()
	{
		IReadOnlyList<Models.ExerciseCatalogItem> mediaItems = ExerciseCatalog
			.GetAllItems()
			.Where(item => item.HasMedia)
			.ToList();

		Assert.NotEmpty(mediaItems);
		Assert.All(mediaItems, item =>
		{
			Assert.NotNull(item.MediaUrl);
			Assert.True(
				item.MediaUrl!.StartsWith(MediaBaseUrl, StringComparison.OrdinalIgnoreCase),
				$"Expected media URL for '{item.Name}' to start with '{MediaBaseUrl}', but found '{item.MediaUrl}'.");
		});
	}

	[Fact]
	public void MediaItems_DoNotContainLegacyOrIncorrectPaths()
	{
		IReadOnlyList<string> mediaUrls = ExerciseCatalog
			.GetAllItems()
			.Where(item => item.HasMedia)
			.Select(item => item.MediaUrl!)
			.ToList();

		Assert.DoesNotContain(mediaUrls, url => url.Contains("/powerclean.mp4", StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(mediaUrls, url => url.Contains("/powersnatch.mp4", StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(mediaUrls, url => url.Contains("/rsi.mp4", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void CanonicalMediaUrls_AreSetForPowerCleanPowerSnatchAndPushPress()
	{
		Models.ExerciseCatalogItem? powerClean = ExerciseCatalog.GetByNameAndCategory("Power Clean", ExerciseCatalog.OlympicLifts);
		Models.ExerciseCatalogItem? powerSnatch = ExerciseCatalog.GetByNameAndCategory("Power Snatch", ExerciseCatalog.OlympicLifts);
		Models.ExerciseCatalogItem? pushPress = ExerciseCatalog.GetByNameAndCategory("Push Press", ExerciseCatalog.Push);

		Assert.NotNull(powerClean);
		Assert.NotNull(powerSnatch);
		Assert.NotNull(pushPress);

		Assert.Equal($"{MediaBaseUrl}Clean.mp4", powerClean!.MediaUrl);
		Assert.Equal($"{MediaBaseUrl}Snatch.mp4", powerSnatch!.MediaUrl);
		Assert.Equal($"{MediaBaseUrl}pushpress.mp4", pushPress!.MediaUrl);
	}
}
