namespace FreakLete.Services;

public static class TabNavigationHelper
{
	public static async Task SwitchToTabAsync(Func<Page> createPage)
	{
		NavigationPage? navigationPage = Application.Current?.Windows.FirstOrDefault()?.Page as NavigationPage;
		INavigation? navigation = navigationPage?.Navigation;
		if (navigation is null)
		{
			return;
		}

		Page targetPage = createPage();
		Page? rootPage = navigation.NavigationStack.FirstOrDefault();
		Page? currentPage = navigation.NavigationStack.LastOrDefault();

		if (rootPage is null || currentPage is null)
		{
			Application.Current!.Windows.First().Page = new NavigationPage(targetPage);
			return;
		}

		if (currentPage.GetType() == targetPage.GetType() && navigation.NavigationStack.Count == 1)
		{
			return;
		}

		if (rootPage.GetType() == targetPage.GetType())
		{
			await navigation.PopToRootAsync(true);
			return;
		}

		navigation.InsertPageBefore(targetPage, rootPage);
		await navigation.PopToRootAsync(true);
	}
}
