namespace FreakLete.Services;

public static class TabNavigationHelper
{
	public static async Task ResetToRootAsync(INavigation navigation, Func<Page> createPage, bool animated = false)
	{
		Page targetPage = createPage();
		Page? rootPage = navigation.NavigationStack.FirstOrDefault();
		Page? currentPage = navigation.NavigationStack.LastOrDefault();

		if (rootPage is null || currentPage is null)
		{
			await navigation.PushAsync(targetPage, animated);
			return;
		}

		if (rootPage.GetType() == targetPage.GetType())
		{
			if (navigation.NavigationStack.Count > 1)
			{
				await navigation.PopToRootAsync(animated);
			}

			return;
		}

		navigation.InsertPageBefore(targetPage, rootPage);
		await navigation.PopToRootAsync(animated);
	}

	public static async Task SwitchToTabAsync(Func<Page> createPage)
	{
		NavigationPage? navigationPage = Application.Current?.Windows.FirstOrDefault()?.Page as NavigationPage;
		INavigation? navigation = navigationPage?.Navigation;
		if (navigation is null)
		{
			return;
		}

		Page targetPage = createPage();
		Page? currentPage = navigation.NavigationStack.LastOrDefault();
		if (currentPage is not null &&
			currentPage.GetType() == targetPage.GetType() &&
			navigation.NavigationStack.Count == 1)
		{
			return;
		}

		await ResetToRootAsync(navigation, createPage, true);
	}
}
