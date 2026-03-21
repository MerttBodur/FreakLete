using GymTracker.Services;

namespace GymTracker.Core.Tests;

public class ExerciseCatalogTests
{
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
}
