using FreakLete;
using FreakLete.Services;

namespace FreakLete.Page.Tests;

/// <summary>
/// Real ProfilePage tests that instantiate the actual ProfilePage class
/// and exercise its real MAUI controls (Entry, Editor, Label) as the
/// interaction surface.
///
/// How it works:
///   - ProfilePage is created via the headless test constructor, which
///     creates real MAUI control instances (Label, Entry, Editor, etc.)
///     without calling InitializeComponent/XAML — avoiding the WinUI3
///     handler registration that requires a platform host.
///   - The page's own methods (LoadProfileAsync, OnSaveProfileClicked,
///     OnSaveCoachProfileClicked) run against these real controls.
///   - FakeApiClient provides an in-memory profile store that mimics
///     the real server's per-section save behavior.
///
/// What IS tested through the real page:
///   - Real ProfilePage instantiation (not a mock or wrapper)
///   - Real MAUI controls as interaction surface (Entry.Text, Editor.Text, Label.Text)
///   - LoadProfileAsync → VM creation → control population
///   - Entry.TextChanged event → VM sync (WeightEntry, BodyFatEntry)
///   - OnSaveProfileClicked → VM.SaveAsync → post-save control sync
///   - OnSaveCoachProfileClicked → Editor.Text push → VM.SaveAsync → post-save control sync
///   - Page reload: new page instance loads persisted data from fake store
///   - All 15 profile fields × 3 datasets
///
/// What is NOT tested here (requires Phase 2 emulator automation):
///   - XAML resource resolution ({StaticResource} styles, colors)
///   - Navigation.PushAsync for OptionPickerPage / DateSelectorPage
///   - Tap gesture recognition (OnSportSelectorTapped, etc.)
///   - Visual rendering, layout, scrolling
/// </summary>
public class ProfilePageTests
{
	// ── Helpers ───────────────────────────────────────────────────────

	private static (ProfilePage page, FakeApiClient api, FakeSessionProvider session) CreatePage(
		UserProfileResponse? initialProfile = null)
	{
		var api = new FakeApiClient();
		if (initialProfile is not null)
			api.Profile = initialProfile;
		var session = new FakeSessionProvider();
		var page = new ProfilePage(api, session, headless: true);
		return (page, api, session);
	}

	/// <summary>
	/// Creates a page, loads profile from fake API, returns ready-to-test page.
	/// This mirrors the real app flow: page created → OnAppearing → LoadProfileAsync.
	/// </summary>
	private static async Task<(ProfilePage page, FakeApiClient api)> CreateAndLoadPage(
		UserProfileResponse? initialProfile = null)
	{
		var (page, api, _) = CreatePage(initialProfile);
		await page.LoadProfileAsync();
		return (page, api);
	}

	/// <summary>
	/// Reloads a fresh page against the same backing store (same FakeApiClient).
	/// Simulates navigating away and back — the real app creates a new page
	/// instance but hits the same server data.
	/// </summary>
	private static async Task<ProfilePage> ReloadPage(FakeApiClient api)
	{
		var session = new FakeSessionProvider();
		var page = new ProfilePage(api, session, headless: true);
		await page.LoadProfileAsync();
		return page;
	}

	/// <summary>
	/// Simulates user typing in an Entry by setting .Text and relying on
	/// the TextChanged event handler wired in LoadProfileAsync.
	/// For WeightEntry and BodyFatEntry, the page subscribes TextChanged → VM sync.
	/// </summary>
	private static void TypeInEntry(Entry entry, string text)
	{
		entry.Text = text;
	}

	/// <summary>
	/// Simulates selector callback by directly setting VM values and updating labels.
	/// On the real app, this happens via OptionPickerPage callback.
	/// In headless tests we call the VM setters (which is what the callback does)
	/// and then verify the label sync after save.
	/// </summary>
	/// <summary>
	/// Reads the page's private _sportCatalog field (populated by LoadProfileAsync
	/// from the fake API's GetSportCatalogAsync). This is the same catalog the page
	/// uses to resolve sport names into SportDefinitionResponse objects.
	/// </summary>
	private static List<SportDefinitionResponse> GetSportCatalog(ProfilePage page)
	{
		var field = typeof(ProfilePage).GetField("_sportCatalog",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		return (List<SportDefinitionResponse>)field!.GetValue(page)!;
	}

	private static void SetAthleteSelectors(
		ProfilePage page,
		DateOnly? dob = null,
		string? sportName = null,
		string? position = null,
		string? gymExperience = null)
	{
		if (dob.HasValue && page._athleteVm is not null)
			page._athleteVm.DateOfBirth = dob;

		if (sportName is not null && page._athleteVm is not null)
		{
			// Same lookup the real page does in OnSportSelectorTapped callback
			var sport = GetSportCatalog(page).FirstOrDefault(s =>
				string.Equals(s.Name, sportName, StringComparison.OrdinalIgnoreCase));
			if (sport is not null)
				page._athleteVm.SelectedSport = sport;
		}

		if (position is not null && page._athleteVm is not null)
			page._athleteVm.SelectedPosition = position;

		if (gymExperience is not null && page._athleteVm is not null)
			page._athleteVm.SelectedGymExperience = gymExperience;
	}

	private static void SetCoachSelectors(
		ProfilePage page,
		string? trainingDays = null,
		string? sessionDuration = null,
		string? primaryGoal = null,
		string? secondaryGoal = null,
		string? dietaryPreference = null)
	{
		if (page._coachVm is null) return;
		if (trainingDays is not null) page._coachVm.SelectedTrainingDays = trainingDays;
		if (sessionDuration is not null) page._coachVm.SelectedSessionDuration = sessionDuration;
		if (primaryGoal is not null) page._coachVm.SelectedPrimaryGoal = primaryGoal;
		if (secondaryGoal is not null) page._coachVm.SelectedSecondaryGoal = secondaryGoal;
		if (dietaryPreference is not null) page._coachVm.SelectedDietaryPreference = dietaryPreference;
	}

	/// <summary>
	/// Gets the named control from the real page. These are actual MAUI control instances.
	/// </summary>
	private static Label GetLabel(ProfilePage page, string purpose) => purpose switch
	{
		"FullName" => GetField<Label>(page, "FullNameLabel"),
		"Email" => GetField<Label>(page, "EmailLabel"),
		"DateOfBirth" => GetField<Label>(page, "DateOfBirthLabel"),
		"Age" => GetField<Label>(page, "AgeLabel"),
		"Sport" => GetField<Label>(page, "SportLabel"),
		"Position" => GetField<Label>(page, "PositionLabel"),
		"GymExperience" => GetField<Label>(page, "GymExperienceLabel"),
		"TrainingDays" => GetField<Label>(page, "TrainingDaysLabel"),
		"SessionDuration" => GetField<Label>(page, "SessionDurationLabel"),
		"PrimaryGoal" => GetField<Label>(page, "PrimaryGoalLabel"),
		"SecondaryGoal" => GetField<Label>(page, "SecondaryGoalLabel"),
		"DietaryPreference" => GetField<Label>(page, "DietaryPreferenceLabel"),
		"Status" => GetField<Label>(page, "StatusLabel"),
		_ => throw new ArgumentException($"Unknown label: {purpose}")
	};

	private static T GetField<T>(ProfilePage page, string fieldName)
	{
		var field = typeof(ProfilePage).GetField(fieldName,
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		return (T)field!.GetValue(page)!;
	}

	private static Entry GetWeightEntry(ProfilePage page) => GetField<Entry>(page, "WeightEntry");
	private static Entry GetBodyFatEntry(ProfilePage page) => GetField<Entry>(page, "BodyFatEntry");
	private static Editor GetEquipmentEditor(ProfilePage page) => GetField<Editor>(page, "EquipmentEditor");
	private static Editor GetInjuryHistoryEditor(ProfilePage page) => GetField<Editor>(page, "InjuryHistoryEditor");
	private static Editor GetCurrentPainEditor(ProfilePage page) => GetField<Editor>(page, "CurrentPainEditor");
	private static Editor GetPhysicalLimitationsEditor(ProfilePage page) => GetField<Editor>(page, "PhysicalLimitationsEditor");

	// ════════════════════════════════════════════════════════════════════
	//  ATHLETE PROFILE — 3 DATASETS through real page
	// ════════════════════════════════════════════════════════════════════

	[Fact]
	public async Task RealPage_AthleteSave_MinimumValid()
	{
		var (page, api) = await CreateAndLoadPage();

		// Interact through real page controls
		TypeInEntry(GetWeightEntry(page), "20");
		TypeInEntry(GetBodyFatEntry(page), "0");

		// Set selectors (VM path — same as OptionPickerPage callback)
		SetAthleteSelectors(page,
			dob: new DateOnly(1900, 1, 1),
			sportName: "Powerlifting",
			gymExperience: "< 1 year");

		// Trigger save through real page method
		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50); // Let async void complete

		// Verify API received correct values
		Assert.NotNull(api.LastAthleteSave);
		Assert.Equal(new DateOnly(1900, 1, 1), api.LastAthleteSave.DateOfBirth);
		Assert.Equal(20.0, api.LastAthleteSave.WeightKg);
		Assert.Equal(0.0, api.LastAthleteSave.BodyFatPercentage);
		Assert.Equal("Powerlifting", api.LastAthleteSave.SportName);
		Assert.Null(api.LastAthleteSave.Position);
		Assert.Equal("< 1 year", api.LastAthleteSave.GymExperienceLevel);

		// Verify real controls show post-save state
		Assert.Equal("20", GetWeightEntry(page).Text);
		Assert.Equal("0", GetBodyFatEntry(page).Text);
		Assert.Contains("1900", GetLabel(page, "DateOfBirth").Text);
		Assert.False(page._athleteVm!.IsDirty);

		// Reload: new page, same backing store
		var reloaded = await ReloadPage(api);
		Assert.Equal("20", GetWeightEntry(reloaded).Text);
		Assert.Equal("0", GetBodyFatEntry(reloaded).Text);
		Assert.Contains("1900", GetLabel(reloaded, "DateOfBirth").Text);
	}

	[Fact]
	public async Task RealPage_AthleteSave_AverageValid()
	{
		var (page, api) = await CreateAndLoadPage();

		TypeInEntry(GetWeightEntry(page), "82.5");
		TypeInEntry(GetBodyFatEntry(page), "15.2");
		SetAthleteSelectors(page,
			dob: new DateOnly(2000, 6, 15),
			sportName: "Basketball",
			position: "Point Guard",
			gymExperience: "3-4 years");

		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		Assert.NotNull(api.LastAthleteSave);
		Assert.Equal(new DateOnly(2000, 6, 15), api.LastAthleteSave.DateOfBirth);
		Assert.Equal(82.5, api.LastAthleteSave.WeightKg);
		Assert.Equal(15.2, api.LastAthleteSave.BodyFatPercentage);
		Assert.Equal("Basketball", api.LastAthleteSave.SportName);
		Assert.Equal("Point Guard", api.LastAthleteSave.Position);
		Assert.Equal("3-4 years", api.LastAthleteSave.GymExperienceLevel);

		// Verify real controls updated by page's post-save sync
		Assert.Contains("2000", GetLabel(page, "DateOfBirth").Text);
		Assert.Equal("Basketball", GetLabel(page, "Sport").Text);
		Assert.Equal("Point Guard", GetLabel(page, "Position").Text);
		Assert.Equal("3-4 years", GetLabel(page, "GymExperience").Text);
		Assert.False(page._athleteVm!.IsDirty);

		// Reload
		var reloaded = await ReloadPage(api);
		Assert.Equal("Basketball", GetLabel(reloaded, "Sport").Text);
		Assert.Equal("Point Guard", GetLabel(reloaded, "Position").Text);
	}

	[Fact]
	public async Task RealPage_AthleteSave_MaximumValid()
	{
		var (page, api) = await CreateAndLoadPage();

		TypeInEntry(GetWeightEntry(page), "400");
		TypeInEntry(GetBodyFatEntry(page), "100");
		SetAthleteSelectors(page,
			dob: new DateOnly(2026, 3, 26),
			sportName: "Soccer",
			position: "Striker",
			gymExperience: "5+ years");

		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		Assert.NotNull(api.LastAthleteSave);
		Assert.Equal(new DateOnly(2026, 3, 26), api.LastAthleteSave.DateOfBirth);
		Assert.Equal(400.0, api.LastAthleteSave.WeightKg);
		Assert.Equal(100.0, api.LastAthleteSave.BodyFatPercentage);
		Assert.Equal("Soccer", api.LastAthleteSave.SportName);
		Assert.Equal("Striker", api.LastAthleteSave.Position);
		Assert.Equal("5+ years", api.LastAthleteSave.GymExperienceLevel);

		Assert.Equal("400", GetWeightEntry(page).Text);
		Assert.Equal("100", GetBodyFatEntry(page).Text);
		Assert.Equal("Soccer", GetLabel(page, "Sport").Text);
		Assert.Equal("Striker", GetLabel(page, "Position").Text);
		Assert.Equal("5+ years", GetLabel(page, "GymExperience").Text);
		Assert.False(page._athleteVm!.IsDirty);

		// Reload
		var reloaded = await ReloadPage(api);
		Assert.Equal("400", GetWeightEntry(reloaded).Text);
		Assert.Equal("Soccer", GetLabel(reloaded, "Sport").Text);
		Assert.Equal("Striker", GetLabel(reloaded, "Position").Text);
	}

	// ════════════════════════════════════════════════════════════════════
	//  COACH PROFILE — 3 DATASETS through real page
	// ════════════════════════════════════════════════════════════════════

	[Fact]
	public async Task RealPage_CoachSave_MinimumValid()
	{
		var (page, api) = await CreateAndLoadPage();

		// Set selectors (VM path)
		SetCoachSelectors(page,
			trainingDays: "1",
			sessionDuration: "30",
			primaryGoal: "Strength",
			dietaryPreference: "No preference");

		// Type in real Editor controls
		GetEquipmentEditor(page).Text = "Bands";
		GetPhysicalLimitationsEditor(page).Text = "None";
		GetInjuryHistoryEditor(page).Text = "None";
		GetCurrentPainEditor(page).Text = "None";

		// Trigger save through real page method — this reads Editor.Text and pushes to VM
		page.OnSaveCoachProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		Assert.NotNull(api.LastCoachSave);
		Assert.Equal(1, api.LastCoachSave.TrainingDaysPerWeek);
		Assert.Equal(30, api.LastCoachSave.PreferredSessionDurationMinutes);
		Assert.Equal("Strength", api.LastCoachSave.PrimaryTrainingGoal);
		Assert.Null(api.LastCoachSave.SecondaryTrainingGoal);
		Assert.Equal("No preference", api.LastCoachSave.DietaryPreference);
		Assert.Equal("Bands", api.LastCoachSave.AvailableEquipment);
		Assert.Equal("None", api.LastCoachSave.PhysicalLimitations);
		Assert.Equal("None", api.LastCoachSave.InjuryHistory);
		Assert.Equal("None", api.LastCoachSave.CurrentPainPoints);

		// Verify real controls updated after save
		Assert.Equal("1", GetLabel(page, "TrainingDays").Text);
		Assert.Equal("30", GetLabel(page, "SessionDuration").Text);
		Assert.Equal("Strength", GetLabel(page, "PrimaryGoal").Text);
		Assert.Equal("No preference", GetLabel(page, "DietaryPreference").Text);
		Assert.Equal("Bands", GetEquipmentEditor(page).Text);
		Assert.False(page._coachVm!.IsDirty);

		// Reload
		var reloaded = await ReloadPage(api);
		Assert.Equal("1", GetLabel(reloaded, "TrainingDays").Text);
		Assert.Equal("Bands", GetEquipmentEditor(reloaded).Text);
		Assert.Equal("None", GetPhysicalLimitationsEditor(reloaded).Text);
		Assert.Equal("None", GetInjuryHistoryEditor(reloaded).Text);
		Assert.Equal("None", GetCurrentPainEditor(reloaded).Text);
	}

	[Fact]
	public async Task RealPage_CoachSave_AverageValid()
	{
		var (page, api) = await CreateAndLoadPage();

		SetCoachSelectors(page,
			trainingDays: "4",
			sessionDuration: "75",
			primaryGoal: "Athletic Performance",
			secondaryGoal: "Hypertrophy",
			dietaryPreference: "Mediterranean");

		GetEquipmentEditor(page).Text = "Full gym, dumbbells, pull-up bar";
		GetPhysicalLimitationsEditor(page).Text = "Mild ankle stiffness";
		GetInjuryHistoryEditor(page).Text = "Minor hamstring strain 2024";
		GetCurrentPainEditor(page).Text = "Occasional knee tightness";

		page.OnSaveCoachProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		Assert.NotNull(api.LastCoachSave);
		Assert.Equal(4, api.LastCoachSave.TrainingDaysPerWeek);
		Assert.Equal(75, api.LastCoachSave.PreferredSessionDurationMinutes);
		Assert.Equal("Athletic Performance", api.LastCoachSave.PrimaryTrainingGoal);
		Assert.Equal("Hypertrophy", api.LastCoachSave.SecondaryTrainingGoal);
		Assert.Equal("Mediterranean", api.LastCoachSave.DietaryPreference);
		Assert.Equal("Full gym, dumbbells, pull-up bar", api.LastCoachSave.AvailableEquipment);
		Assert.Equal("Mild ankle stiffness", api.LastCoachSave.PhysicalLimitations);
		Assert.Equal("Minor hamstring strain 2024", api.LastCoachSave.InjuryHistory);
		Assert.Equal("Occasional knee tightness", api.LastCoachSave.CurrentPainPoints);

		// Verify controls
		Assert.Equal("Athletic Performance", GetLabel(page, "PrimaryGoal").Text);
		Assert.Equal("Hypertrophy", GetLabel(page, "SecondaryGoal").Text);
		Assert.Equal("Mediterranean", GetLabel(page, "DietaryPreference").Text);
		Assert.Equal("Full gym, dumbbells, pull-up bar", GetEquipmentEditor(page).Text);
		Assert.Equal("Occasional knee tightness", GetCurrentPainEditor(page).Text);
		Assert.False(page._coachVm!.IsDirty);

		// Reload
		var reloaded = await ReloadPage(api);
		Assert.Equal("Athletic Performance", GetLabel(reloaded, "PrimaryGoal").Text);
		Assert.Equal("Occasional knee tightness", GetCurrentPainEditor(reloaded).Text);
		Assert.Equal("Mild ankle stiffness", GetPhysicalLimitationsEditor(reloaded).Text);
	}

	[Fact]
	public async Task RealPage_CoachSave_MaximumValid()
	{
		var (page, api) = await CreateAndLoadPage();

		SetCoachSelectors(page,
			trainingDays: "7",
			sessionDuration: "120",
			primaryGoal: "Body Recomposition",
			secondaryGoal: "Olympic Weightlifting",
			dietaryPreference: "Kosher");

		GetEquipmentEditor(page).Text = "Full gym, plates, sled, boxes, bands, rower";
		GetPhysicalLimitationsEditor(page).Text = "Limited overhead mobility";
		GetInjuryHistoryEditor(page).Text = "ACL reconstruction 2021, shoulder impingement history";
		GetCurrentPainEditor(page).Text = "Low back tightness after long sessions";

		page.OnSaveCoachProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		Assert.NotNull(api.LastCoachSave);
		Assert.Equal(7, api.LastCoachSave.TrainingDaysPerWeek);
		Assert.Equal(120, api.LastCoachSave.PreferredSessionDurationMinutes);
		Assert.Equal("Body Recomposition", api.LastCoachSave.PrimaryTrainingGoal);
		Assert.Equal("Olympic Weightlifting", api.LastCoachSave.SecondaryTrainingGoal);
		Assert.Equal("Kosher", api.LastCoachSave.DietaryPreference);
		Assert.Equal("Full gym, plates, sled, boxes, bands, rower", api.LastCoachSave.AvailableEquipment);
		Assert.Equal("Limited overhead mobility", api.LastCoachSave.PhysicalLimitations);
		Assert.Equal("ACL reconstruction 2021, shoulder impingement history", api.LastCoachSave.InjuryHistory);
		Assert.Equal("Low back tightness after long sessions", api.LastCoachSave.CurrentPainPoints);

		// Verify controls
		Assert.Equal("7", GetLabel(page, "TrainingDays").Text);
		Assert.Equal("120", GetLabel(page, "SessionDuration").Text);
		Assert.Equal("Body Recomposition", GetLabel(page, "PrimaryGoal").Text);
		Assert.Equal("Kosher", GetLabel(page, "DietaryPreference").Text);
		Assert.Equal("Low back tightness after long sessions", GetCurrentPainEditor(page).Text);
		Assert.False(page._coachVm!.IsDirty);

		// Reload
		var reloaded = await ReloadPage(api);
		Assert.Equal("7", GetLabel(reloaded, "TrainingDays").Text);
		Assert.Equal("120", GetLabel(reloaded, "SessionDuration").Text);
		Assert.Equal("Kosher", GetLabel(reloaded, "DietaryPreference").Text);
		Assert.Equal("Low back tightness after long sessions", GetCurrentPainEditor(reloaded).Text);
		Assert.Equal("ACL reconstruction 2021, shoulder impingement history", GetInjuryHistoryEditor(reloaded).Text);
	}

	// ════════════════════════════════════════════════════════════════════
	//  COACH EDITOR PUSH-AT-SAVE — real page exercises this exactly
	// ════════════════════════════════════════════════════════════════════

	[Fact]
	public async Task RealPage_CoachEditors_PushedAtSaveTime_NotBefore()
	{
		var (page, api) = await CreateAndLoadPage();

		// User types in editors — VM doesn't see these yet
		GetEquipmentEditor(page).Text = "Full gym";
		GetPhysicalLimitationsEditor(page).Text = "Bad knee";
		GetInjuryHistoryEditor(page).Text = "Torn ACL";
		GetCurrentPainEditor(page).Text = "Lower back";

		// VM still has empty (hydrated) values
		Assert.Equal("", page._coachVm!.EquipmentText);
		Assert.Equal("", page._coachVm.LimitationsText);
		Assert.Equal("", page._coachVm.InjuryHistoryText);
		Assert.Equal("", page._coachVm.PainPointsText);

		// Set a required selector so save succeeds
		page._coachVm.SelectedTrainingDays = "3";

		// The real page's OnSaveCoachProfileClicked reads Editor.Text and pushes to VM
		page.OnSaveCoachProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		// Now the VM and save request have the editor values
		Assert.Equal("Full gym", api.LastCoachSave!.AvailableEquipment);
		Assert.Equal("Bad knee", api.LastCoachSave.PhysicalLimitations);
		Assert.Equal("Torn ACL", api.LastCoachSave.InjuryHistory);
		Assert.Equal("Lower back", api.LastCoachSave.CurrentPainPoints);
	}

	// ════════════════════════════════════════════════════════════════════
	//  POSITION VISIBILITY — real page PositionContainer.IsVisible
	// ════════════════════════════════════════════════════════════════════

	[Fact]
	public async Task RealPage_PositionContainer_VisibilityMatchesSport()
	{
		var (page, _) = await CreateAndLoadPage();

		var positionContainer = GetField<VerticalStackLayout>(page, "PositionContainer");

		// No sport selected → hidden
		Assert.False(positionContainer.IsVisible);

		// Select Basketball (has positions) → position becomes visible after save sync
		SetAthleteSelectors(page, sportName: "Basketball", position: "Center");
		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		Assert.True(positionContainer.IsVisible);
		Assert.Equal("Center", GetLabel(page, "Position").Text);

		// Switch to Powerlifting (no positions) → position hidden after save sync
		SetAthleteSelectors(page, sportName: "Powerlifting");
		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		Assert.False(positionContainer.IsVisible);
	}

	// ════════════════════════════════════════════════════════════════════
	//  DOB DISPLAY → SAVE → RELOAD through real page labels
	// ════════════════════════════════════════════════════════════════════

	[Fact]
	public async Task RealPage_DateOfBirth_DisplayAndPersistence()
	{
		var (page, api) = await CreateAndLoadPage();

		var dobLabel = GetLabel(page, "DateOfBirth");
		var ageLabel = GetLabel(page, "Age");

		// Before selection: placeholder
		Assert.Equal("Select date of birth", dobLabel.Text);

		// Simulate DateSelectorPage callback
		page._athleteVm!.DateOfBirth = new DateOnly(1995, 8, 20);

		// Trigger save — page's post-save code calls SyncDateOfBirthUI
		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		// Real label updated
		Assert.Contains("1995", dobLabel.Text);
		Assert.NotEqual("Select date of birth", dobLabel.Text);
		Assert.Contains("Age:", ageLabel.Text);

		// API received the value
		Assert.Equal(new DateOnly(1995, 8, 20), api.LastAthleteSave!.DateOfBirth);

		// Reload — new page reads persisted data
		var reloaded = await ReloadPage(api);
		Assert.Contains("1995", GetLabel(reloaded, "DateOfBirth").Text);
	}

	// ════════════════════════════════════════════════════════════════════
	//  CROSS-SECTION ISOLATION through real page
	// ════════════════════════════════════════════════════════════════════

	[Fact]
	public async Task RealPage_AthleteSave_DoesNotMutateCoachControls()
	{
		var profile = new UserProfileResponse
		{
			Id = 1, FirstName = "Test", LastName = "User", Email = "t@t.com",
			TrainingDaysPerWeek = 5, PrimaryTrainingGoal = "Strength",
			AvailableEquipment = "Barbell", InjuryHistory = "ACL 2020"
		};

		var (page, api) = await CreateAndLoadPage(profile);

		// Verify coach controls were populated from profile
		Assert.Equal("5", GetLabel(page, "TrainingDays").Text);
		Assert.Equal("Strength", GetLabel(page, "PrimaryGoal").Text);
		Assert.Equal("Barbell", GetEquipmentEditor(page).Text);

		// Save athlete only
		TypeInEntry(GetWeightEntry(page), "80");
		page._athleteVm!.DateOfBirth = new DateOnly(2000, 1, 1);
		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		// Reload and verify coach fields preserved
		var reloaded = await ReloadPage(api);
		Assert.Equal("5", GetLabel(reloaded, "TrainingDays").Text);
		Assert.Equal("Strength", GetLabel(reloaded, "PrimaryGoal").Text);
		Assert.Equal("Barbell", GetEquipmentEditor(reloaded).Text);
		Assert.Equal("ACL 2020", GetInjuryHistoryEditor(reloaded).Text);
	}

	[Fact]
	public async Task RealPage_CoachSave_DoesNotMutateAthleteControls()
	{
		var profile = new UserProfileResponse
		{
			Id = 1, FirstName = "Test", LastName = "User", Email = "t@t.com",
			WeightKg = 90, SportName = "Basketball", Position = "Center",
			GymExperienceLevel = "5+ years", DateOfBirth = new DateOnly(1998, 3, 10)
		};

		var (page, api) = await CreateAndLoadPage(profile);

		// Verify athlete controls populated
		Assert.Equal("90", GetWeightEntry(page).Text);
		Assert.Equal("Basketball", GetLabel(page, "Sport").Text);

		// Save coach only
		page._coachVm!.SelectedTrainingDays = "3";
		GetEquipmentEditor(page).Text = "Dumbbells";
		page.OnSaveCoachProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		// Reload and verify athlete fields preserved
		var reloaded = await ReloadPage(api);
		Assert.Equal("90", GetWeightEntry(reloaded).Text);
		Assert.Equal("Basketball", GetLabel(reloaded, "Sport").Text);
		Assert.Equal("Center", GetLabel(reloaded, "Position").Text);
		Assert.Equal("5+ years", GetLabel(reloaded, "GymExperience").Text);
		Assert.Contains("1998", GetLabel(reloaded, "DateOfBirth").Text);
	}

	// ════════════════════════════════════════════════════════════════════
	//  FULL RELOAD — all 15 fields through real controls
	// ════════════════════════════════════════════════════════════════════

	[Fact]
	public async Task RealPage_FullReload_All15FieldsReflected()
	{
		var (page, api) = await CreateAndLoadPage();

		// Save athlete
		TypeInEntry(GetWeightEntry(page), "82.5");
		TypeInEntry(GetBodyFatEntry(page), "15.2");
		SetAthleteSelectors(page,
			dob: new DateOnly(2000, 6, 15),
			sportName: "Basketball",
			position: "Point Guard",
			gymExperience: "3-4 years");
		page.OnSaveProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		// Save coach
		SetCoachSelectors(page,
			trainingDays: "4",
			sessionDuration: "75",
			primaryGoal: "Athletic Performance",
			secondaryGoal: "Hypertrophy",
			dietaryPreference: "Mediterranean");
		GetEquipmentEditor(page).Text = "Full gym, dumbbells, pull-up bar";
		GetPhysicalLimitationsEditor(page).Text = "Mild ankle stiffness";
		GetInjuryHistoryEditor(page).Text = "Minor hamstring strain 2024";
		GetCurrentPainEditor(page).Text = "Occasional knee tightness";
		page.OnSaveCoachProfileClicked(null, EventArgs.Empty);
		await Task.Delay(50);

		// Full reload — new page instance
		var r = await ReloadPage(api);

		// 6 athlete fields via real controls
		Assert.Contains("2000", GetLabel(r, "DateOfBirth").Text);
		Assert.Equal("82.5", GetWeightEntry(r).Text);
		Assert.Equal("15.2", GetBodyFatEntry(r).Text);
		Assert.Equal("Basketball", GetLabel(r, "Sport").Text);
		Assert.Equal("Point Guard", GetLabel(r, "Position").Text);
		Assert.Equal("3-4 years", GetLabel(r, "GymExperience").Text);

		// 9 coach fields via real controls
		Assert.Equal("4", GetLabel(r, "TrainingDays").Text);
		Assert.Equal("75", GetLabel(r, "SessionDuration").Text);
		Assert.Equal("Athletic Performance", GetLabel(r, "PrimaryGoal").Text);
		Assert.Equal("Hypertrophy", GetLabel(r, "SecondaryGoal").Text);
		Assert.Equal("Mediterranean", GetLabel(r, "DietaryPreference").Text);
		Assert.Equal("Full gym, dumbbells, pull-up bar", GetEquipmentEditor(r).Text);
		Assert.Equal("Mild ankle stiffness", GetPhysicalLimitationsEditor(r).Text);
		Assert.Equal("Minor hamstring strain 2024", GetInjuryHistoryEditor(r).Text);
		Assert.Equal("Occasional knee tightness", GetCurrentPainEditor(r).Text);
	}
}
