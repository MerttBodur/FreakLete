using FreakLete.Services;
using FreakLete.ViewModels;

namespace FreakLete.Core.Tests;

/// <summary>
/// ViewModel-level profile flow tests. These test the save/load/roundtrip
/// logic through AthleteProfileViewModel and CoachProfileViewModel directly,
/// without instantiating ProfilePage or any MAUI controls.
///
/// This is NOT a page test. It verifies:
///   - VM hydration from profile response
///   - VM save → API request mapping (all 15 fields × 3 datasets)
///   - Coach editor push-at-save-time behavior (mirrors OnSaveCoachProfileClicked)
///   - Cross-section isolation (athlete save ≠ coach mutation)
///   - Position visibility toggling
///   - DOB display + save roundtrip
///   - Reload (re-hydrate from persisted state)
///
/// Real ProfilePage + MAUI control verification happens on Android emulator (manual smoke testing).
/// </summary>
public class ProfileViewModelFlowTests
{
    // ── Sport catalog ─────────────────────────────────────────────────

    private static readonly List<SportDefinitionResponse> SportCatalog =
    [
        new() { Id = "powerlifting", Name = "Powerlifting", Category = "Strength", HasPositions = false, Positions = [] },
        new() { Id = "basketball", Name = "Basketball", Category = "Team", HasPositions = true, Positions = ["Point Guard", "Shooting Guard", "Small Forward", "Power Forward", "Center"] },
        new() { Id = "soccer", Name = "Soccer", Category = "Team", HasPositions = true, Positions = ["Goalkeeper", "Defender", "Midfielder", "Striker"] },
    ];

    // ── In-memory profile store ───────────────────────────────────────

    /// <summary>
    /// Simulates the server-side profile store. Athlete save writes only athlete
    /// fields; coach save writes only coach fields. Mirrors real endpoint behavior.
    /// </summary>
    private class FakeProfileStore
    {
        public UserProfileResponse Profile { get; private set; }

        public SaveAthleteProfileRequest? LastAthleteSave { get; private set; }
        public SaveCoachProfileRequest? LastCoachSave { get; private set; }

        public FakeProfileStore(UserProfileResponse? initial = null)
        {
            Profile = initial ?? new UserProfileResponse
            {
                Id = 1, FirstName = "Test", LastName = "User", Email = "test@test.com"
            };
        }

        public Task<ApiResult<UserProfileResponse>> SaveAthlete(SaveAthleteProfileRequest req)
        {
            LastAthleteSave = req;
            Profile.DateOfBirth = req.DateOfBirth;
            Profile.WeightKg = req.WeightKg;
            Profile.BodyFatPercentage = req.BodyFatPercentage;
            Profile.SportName = req.SportName ?? "";
            Profile.Position = req.Position ?? "";
            Profile.GymExperienceLevel = req.GymExperienceLevel ?? "";
            return Task.FromResult(ApiResult<UserProfileResponse>.Ok(Profile));
        }

        public Task<ApiResult<UserProfileResponse>> SaveCoach(SaveCoachProfileRequest req)
        {
            LastCoachSave = req;
            Profile.TrainingDaysPerWeek = req.TrainingDaysPerWeek;
            Profile.PreferredSessionDurationMinutes = req.PreferredSessionDurationMinutes;
            Profile.PrimaryTrainingGoal = req.PrimaryTrainingGoal ?? "";
            Profile.SecondaryTrainingGoal = req.SecondaryTrainingGoal ?? "";
            Profile.DietaryPreference = req.DietaryPreference ?? "";
            Profile.AvailableEquipment = req.AvailableEquipment ?? "";
            Profile.PhysicalLimitations = req.PhysicalLimitations ?? "";
            Profile.InjuryHistory = req.InjuryHistory ?? "";
            Profile.CurrentPainPoints = req.CurrentPainPoints ?? "";
            return Task.FromResult(ApiResult<UserProfileResponse>.Ok(Profile));
        }
    }

    // ── Helpers that mirror ProfilePage's own wiring ──────────────────

    /// <summary>Same as ProfilePage.LoadProfileAsync: creates VMs, hydrates from profile.</summary>
    private static (AthleteProfileViewModel athlete, CoachProfileViewModel coach) LoadPage(FakeProfileStore store)
    {
        var athlete = new AthleteProfileViewModel(store.SaveAthlete, SportCatalog);
        athlete.HydrateFromProfile(store.Profile);
        var coach = new CoachProfileViewModel(store.SaveCoach);
        coach.HydrateFromProfile(store.Profile);
        return (athlete, coach);
    }

    /// <summary>
    /// Mirrors OnSaveCoachProfileClicked: pushes editor text values into
    /// the coach VM before saving. This is the exact page behavior — editors
    /// are NOT live-synced, they are pushed at save time.
    /// </summary>
    private static async Task<bool> CoachSaveWithEditorPush(
        CoachProfileViewModel coach,
        string equipmentText, string limitationsText,
        string injuryHistoryText, string painPointsText)
    {
        // Same as ProfilePage.OnSaveCoachProfileClicked lines 445-448
        coach.EquipmentText = equipmentText.Trim();
        coach.LimitationsText = limitationsText.Trim();
        coach.InjuryHistoryText = injuryHistoryText.Trim();
        coach.PainPointsText = painPointsText.Trim();
        return await coach.SaveAsync();
    }

    // ════════════════════════════════════════════════════════════════════
    //  ATHLETE PROFILE — 3 DATASETS
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AthleteSave_MinimumValid_RoundtripsCorrectly()
    {
        var store = new FakeProfileStore();
        var (athlete, _) = LoadPage(store);

        // Simulate user edits (same as page control interactions)
        athlete.DateOfBirth = new DateOnly(1900, 1, 1);
        athlete.WeightText = "20";
        athlete.BodyFatText = "0";
        athlete.SelectedSport = SportCatalog.First(s => s.Name == "Powerlifting");
        athlete.SelectedGymExperience = "< 1 year";

        var success = await athlete.SaveAsync();
        Assert.True(success);

        // Verify request sent
        Assert.Equal(new DateOnly(1900, 1, 1), store.LastAthleteSave!.DateOfBirth);
        Assert.Equal(20.0, store.LastAthleteSave.WeightKg);
        Assert.Equal(0.0, store.LastAthleteSave.BodyFatPercentage);
        Assert.Equal("Powerlifting", store.LastAthleteSave.SportName);
        Assert.Null(store.LastAthleteSave.Position); // Powerlifting has no positions
        Assert.Equal("< 1 year", store.LastAthleteSave.GymExperienceLevel);

        // Verify post-save VM state (matches page UI sync)
        Assert.False(athlete.IsDirty);
        Assert.True(athlete.SaveSucceeded);
        Assert.Equal("20", athlete.WeightText);

        // Reload (same as page OnAppearing re-load)
        var (reloaded, _) = LoadPage(store);
        Assert.Equal(new DateOnly(1900, 1, 1), reloaded.DateOfBirth);
        Assert.Equal("20", reloaded.WeightText);
        Assert.Equal("0", reloaded.BodyFatText);
        Assert.Equal("Powerlifting", reloaded.SelectedSport?.Name);
        Assert.Null(reloaded.SelectedPosition);
        Assert.Equal("< 1 year", reloaded.SelectedGymExperience);
        Assert.False(reloaded.IsDirty);
    }

    [Fact]
    public async Task AthleteSave_AverageValid_RoundtripsCorrectly()
    {
        var store = new FakeProfileStore();
        var (athlete, _) = LoadPage(store);

        athlete.DateOfBirth = new DateOnly(2000, 6, 15);
        athlete.WeightText = "82.5";
        athlete.BodyFatText = "15.2";
        athlete.SelectedSport = SportCatalog.First(s => s.Name == "Basketball");
        athlete.SelectedPosition = "Point Guard";
        athlete.SelectedGymExperience = "3-4 years";

        var success = await athlete.SaveAsync();
        Assert.True(success);

        Assert.Equal(new DateOnly(2000, 6, 15), store.LastAthleteSave!.DateOfBirth);
        Assert.Equal(82.5, store.LastAthleteSave.WeightKg);
        Assert.Equal(15.2, store.LastAthleteSave.BodyFatPercentage);
        Assert.Equal("Basketball", store.LastAthleteSave.SportName);
        Assert.Equal("Point Guard", store.LastAthleteSave.Position);
        Assert.Equal("3-4 years", store.LastAthleteSave.GymExperienceLevel);

        Assert.False(athlete.IsDirty);

        // Reload
        var (reloaded, _) = LoadPage(store);
        Assert.Equal("Basketball", reloaded.SelectedSport?.Name);
        Assert.Equal("Point Guard", reloaded.SelectedPosition);
        Assert.Equal(82.5, double.Parse(reloaded.WeightText));
        Assert.Equal(15.2, double.Parse(reloaded.BodyFatText));
    }

    [Fact]
    public async Task AthleteSave_MaximumValid_RoundtripsCorrectly()
    {
        var store = new FakeProfileStore();
        var (athlete, _) = LoadPage(store);

        athlete.DateOfBirth = new DateOnly(2026, 3, 26);
        athlete.WeightText = "400";
        athlete.BodyFatText = "100";
        athlete.SelectedSport = SportCatalog.First(s => s.Name == "Soccer");
        athlete.SelectedPosition = "Striker";
        athlete.SelectedGymExperience = "5+ years";

        var success = await athlete.SaveAsync();
        Assert.True(success);

        Assert.Equal(new DateOnly(2026, 3, 26), store.LastAthleteSave!.DateOfBirth);
        Assert.Equal(400.0, store.LastAthleteSave.WeightKg);
        Assert.Equal(100.0, store.LastAthleteSave.BodyFatPercentage);
        Assert.Equal("Soccer", store.LastAthleteSave.SportName);
        Assert.Equal("Striker", store.LastAthleteSave.Position);
        Assert.Equal("5+ years", store.LastAthleteSave.GymExperienceLevel);

        Assert.False(athlete.IsDirty);

        // Reload
        var (reloaded, _) = LoadPage(store);
        Assert.Equal("Soccer", reloaded.SelectedSport?.Name);
        Assert.Equal("Striker", reloaded.SelectedPosition);
        Assert.Equal("400", reloaded.WeightText);
        Assert.Equal("100", reloaded.BodyFatText);
        Assert.Equal("5+ years", reloaded.SelectedGymExperience);
    }

    // ════════════════════════════════════════════════════════════════════
    //  COACH PROFILE — 3 DATASETS (with editor push-at-save behavior)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CoachSave_MinimumValid_RoundtripsCorrectly()
    {
        var store = new FakeProfileStore();
        var (_, coach) = LoadPage(store);

        // Selector fields (set via OptionPickerPage callbacks on real page)
        coach.SelectedTrainingDays = "1";
        coach.SelectedSessionDuration = "30";
        coach.SelectedPrimaryGoal = "Strength";
        coach.SelectedSecondaryGoal = null;
        coach.SelectedDietaryPreference = "No preference";

        // Editor fields pushed at save time (same as page OnSaveCoachProfileClicked)
        var success = await CoachSaveWithEditorPush(coach,
            equipmentText: "Bands",
            limitationsText: "None",
            injuryHistoryText: "None",
            painPointsText: "None");

        Assert.True(success);

        Assert.Equal(1, store.LastCoachSave!.TrainingDaysPerWeek);
        Assert.Equal(30, store.LastCoachSave.PreferredSessionDurationMinutes);
        Assert.Equal("Strength", store.LastCoachSave.PrimaryTrainingGoal);
        Assert.Null(store.LastCoachSave.SecondaryTrainingGoal);
        Assert.Equal("No preference", store.LastCoachSave.DietaryPreference);
        Assert.Equal("Bands", store.LastCoachSave.AvailableEquipment);
        Assert.Equal("None", store.LastCoachSave.PhysicalLimitations);
        Assert.Equal("None", store.LastCoachSave.InjuryHistory);
        Assert.Equal("None", store.LastCoachSave.CurrentPainPoints);

        Assert.False(coach.IsDirty);
        Assert.True(coach.SaveSucceeded);

        // Reload
        var (_, reloaded) = LoadPage(store);
        Assert.Equal("1", reloaded.SelectedTrainingDays);
        Assert.Equal("30", reloaded.SelectedSessionDuration);
        Assert.Equal("Strength", reloaded.SelectedPrimaryGoal);
        Assert.Null(reloaded.SelectedSecondaryGoal);
        Assert.Equal("No preference", reloaded.SelectedDietaryPreference);
        Assert.Equal("Bands", reloaded.EquipmentText);
        Assert.Equal("None", reloaded.LimitationsText);
        Assert.Equal("None", reloaded.InjuryHistoryText);
        Assert.Equal("None", reloaded.PainPointsText);
        Assert.False(reloaded.IsDirty);
    }

    [Fact]
    public async Task CoachSave_AverageValid_RoundtripsCorrectly()
    {
        var store = new FakeProfileStore();
        var (_, coach) = LoadPage(store);

        coach.SelectedTrainingDays = "4";
        coach.SelectedSessionDuration = "75";
        coach.SelectedPrimaryGoal = "Athletic Performance";
        coach.SelectedSecondaryGoal = "Hypertrophy";
        coach.SelectedDietaryPreference = "Mediterranean";

        var success = await CoachSaveWithEditorPush(coach,
            equipmentText: "Full gym, dumbbells, pull-up bar",
            limitationsText: "Mild ankle stiffness",
            injuryHistoryText: "Minor hamstring strain 2024",
            painPointsText: "Occasional knee tightness");

        Assert.True(success);

        Assert.Equal(4, store.LastCoachSave!.TrainingDaysPerWeek);
        Assert.Equal(75, store.LastCoachSave.PreferredSessionDurationMinutes);
        Assert.Equal("Athletic Performance", store.LastCoachSave.PrimaryTrainingGoal);
        Assert.Equal("Hypertrophy", store.LastCoachSave.SecondaryTrainingGoal);
        Assert.Equal("Mediterranean", store.LastCoachSave.DietaryPreference);
        Assert.Equal("Full gym, dumbbells, pull-up bar", store.LastCoachSave.AvailableEquipment);
        Assert.Equal("Mild ankle stiffness", store.LastCoachSave.PhysicalLimitations);
        Assert.Equal("Minor hamstring strain 2024", store.LastCoachSave.InjuryHistory);
        Assert.Equal("Occasional knee tightness", store.LastCoachSave.CurrentPainPoints);

        Assert.False(coach.IsDirty);

        // Reload
        var (_, reloaded) = LoadPage(store);
        Assert.Equal("Athletic Performance", reloaded.SelectedPrimaryGoal);
        Assert.Equal("Hypertrophy", reloaded.SelectedSecondaryGoal);
        Assert.Equal("Occasional knee tightness", reloaded.PainPointsText);
        Assert.Equal("Mild ankle stiffness", reloaded.LimitationsText);
    }

    [Fact]
    public async Task CoachSave_MaximumValid_RoundtripsCorrectly()
    {
        var store = new FakeProfileStore();
        var (_, coach) = LoadPage(store);

        coach.SelectedTrainingDays = "7";
        coach.SelectedSessionDuration = "120";
        coach.SelectedPrimaryGoal = "Body Recomposition";
        coach.SelectedSecondaryGoal = "Olympic Weightlifting";
        coach.SelectedDietaryPreference = "Kosher";

        var success = await CoachSaveWithEditorPush(coach,
            equipmentText: "Full gym, plates, sled, boxes, bands, rower",
            limitationsText: "Limited overhead mobility",
            injuryHistoryText: "ACL reconstruction 2021, shoulder impingement history",
            painPointsText: "Low back tightness after long sessions");

        Assert.True(success);

        Assert.Equal(7, store.LastCoachSave!.TrainingDaysPerWeek);
        Assert.Equal(120, store.LastCoachSave.PreferredSessionDurationMinutes);
        Assert.Equal("Body Recomposition", store.LastCoachSave.PrimaryTrainingGoal);
        Assert.Equal("Olympic Weightlifting", store.LastCoachSave.SecondaryTrainingGoal);
        Assert.Equal("Kosher", store.LastCoachSave.DietaryPreference);
        Assert.Equal("Full gym, plates, sled, boxes, bands, rower", store.LastCoachSave.AvailableEquipment);
        Assert.Equal("Limited overhead mobility", store.LastCoachSave.PhysicalLimitations);
        Assert.Equal("ACL reconstruction 2021, shoulder impingement history", store.LastCoachSave.InjuryHistory);
        Assert.Equal("Low back tightness after long sessions", store.LastCoachSave.CurrentPainPoints);

        Assert.False(coach.IsDirty);

        // Reload
        var (_, reloaded) = LoadPage(store);
        Assert.Equal("7", reloaded.SelectedTrainingDays);
        Assert.Equal("120", reloaded.SelectedSessionDuration);
        Assert.Equal("Kosher", reloaded.SelectedDietaryPreference);
        Assert.Equal("Low back tightness after long sessions", reloaded.PainPointsText);
        Assert.Equal("ACL reconstruction 2021, shoulder impingement history", reloaded.InjuryHistoryText);
    }

    // ════════════════════════════════════════════════════════════════════
    //  SAVE → VISIBLE STATE (VM + display properties)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AthleteSave_DisplayPropertiesUpdateCorrectly()
    {
        var store = new FakeProfileStore();
        var (athlete, _) = LoadPage(store);

        athlete.DateOfBirth = new DateOnly(2000, 6, 15);
        athlete.WeightText = "82.5";
        athlete.BodyFatText = "15.2";
        athlete.SelectedSport = SportCatalog.First(s => s.Name == "Basketball");
        athlete.SelectedPosition = "Point Guard";
        athlete.SelectedGymExperience = "3-4 years";

        await athlete.SaveAsync();

        // These are the same properties ProfilePage reads to update labels
        // Use culture-independent check: verify it contains the year and is not the placeholder
        Assert.Contains("2000", athlete.DateOfBirthDisplay);
        Assert.NotEqual("Select date of birth", athlete.DateOfBirthDisplay);
        Assert.Equal("Basketball", athlete.SelectedSport?.Name);
        Assert.Equal("Point Guard", athlete.SelectedPosition);
        Assert.True(athlete.ShowPositionSelector);
        Assert.Equal("3-4 years", athlete.SelectedGymExperience);
        Assert.False(athlete.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════════
    //  RELOAD SHOWS PERSISTED VALUES (full profile)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Reload_ShowsAllPersistedAthleteAndCoachValues()
    {
        var store = new FakeProfileStore();
        var (athlete, coach) = LoadPage(store);

        // Save athlete
        athlete.DateOfBirth = new DateOnly(2000, 6, 15);
        athlete.WeightText = "82.5";
        athlete.BodyFatText = "15.2";
        athlete.SelectedSport = SportCatalog.First(s => s.Name == "Basketball");
        athlete.SelectedPosition = "Point Guard";
        athlete.SelectedGymExperience = "3-4 years";
        await athlete.SaveAsync();

        // Save coach
        coach.SelectedTrainingDays = "4";
        coach.SelectedSessionDuration = "75";
        coach.SelectedPrimaryGoal = "Athletic Performance";
        coach.SelectedSecondaryGoal = "Hypertrophy";
        coach.SelectedDietaryPreference = "Mediterranean";
        await CoachSaveWithEditorPush(coach,
            "Full gym, dumbbells, pull-up bar",
            "Mild ankle stiffness",
            "Minor hamstring strain 2024",
            "Occasional knee tightness");

        // Full reload (same as navigating away and back)
        var (ra, rc) = LoadPage(store);

        // All 6 athlete fields
        Assert.Equal(new DateOnly(2000, 6, 15), ra.DateOfBirth);
        Assert.Equal(82.5, double.Parse(ra.WeightText));
        Assert.Equal(15.2, double.Parse(ra.BodyFatText));
        Assert.Equal("Basketball", ra.SelectedSport?.Name);
        Assert.Equal("Point Guard", ra.SelectedPosition);
        Assert.Equal("3-4 years", ra.SelectedGymExperience);

        // All 9 coach fields
        Assert.Equal("4", rc.SelectedTrainingDays);
        Assert.Equal("75", rc.SelectedSessionDuration);
        Assert.Equal("Athletic Performance", rc.SelectedPrimaryGoal);
        Assert.Equal("Hypertrophy", rc.SelectedSecondaryGoal);
        Assert.Equal("Mediterranean", rc.SelectedDietaryPreference);
        Assert.Equal("Full gym, dumbbells, pull-up bar", rc.EquipmentText);
        Assert.Equal("Mild ankle stiffness", rc.LimitationsText);
        Assert.Equal("Minor hamstring strain 2024", rc.InjuryHistoryText);
        Assert.Equal("Occasional knee tightness", rc.PainPointsText);

        // Not dirty
        Assert.False(ra.IsDirty);
        Assert.False(rc.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════════
    //  POSITION VISIBILITY
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PositionSelector_VisibleForSportsWithPositions_HiddenOtherwise()
    {
        var store = new FakeProfileStore();
        var (athlete, _) = LoadPage(store);

        // No sport → position hidden
        Assert.False(athlete.ShowPositionSelector);

        // Basketball → has positions → visible
        athlete.SelectedSport = SportCatalog.First(s => s.Name == "Basketball");
        Assert.True(athlete.ShowPositionSelector);

        // Set and save a position
        athlete.SelectedPosition = "Center";
        await athlete.SaveAsync();

        // Switch to Powerlifting → no positions → position cleared + hidden
        athlete.SelectedSport = SportCatalog.First(s => s.Name == "Powerlifting");
        Assert.False(athlete.ShowPositionSelector);
        Assert.Null(athlete.SelectedPosition);

        // Save and verify position is null in request
        await athlete.SaveAsync();
        Assert.Null(store.LastAthleteSave!.Position);

        // Reload → position still null
        var (reloaded, _) = LoadPage(store);
        Assert.Null(reloaded.SelectedPosition);
        Assert.False(reloaded.ShowPositionSelector);
    }

    // ════════════════════════════════════════════════════════════════════
    //  DOB THROUGH SAVE FLOW
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DateOfBirth_SelectionPersistsThroughSaveAndReload()
    {
        var store = new FakeProfileStore();
        var (athlete, _) = LoadPage(store);

        // Simulate DOB selection (same as DateSelectorPage callback)
        athlete.DateOfBirth = new DateOnly(1995, 8, 20);

        Assert.True(athlete.IsDirty);
        Assert.Contains("1995", athlete.DateOfBirthDisplay);
        Assert.NotEqual("Select date of birth", athlete.DateOfBirthDisplay);

        // Age calculation
        var expectedAge = ProfileStateManager.CalculateAge(new DateOnly(1995, 8, 20), DateOnly.FromDateTime(DateTime.Today));
        Assert.Contains(expectedAge.ToString()!, athlete.AgeDisplay);

        // Save
        await athlete.SaveAsync();
        Assert.Equal(new DateOnly(1995, 8, 20), store.LastAthleteSave!.DateOfBirth);
        Assert.False(athlete.IsDirty);

        // Reload
        var (reloaded, _) = LoadPage(store);
        Assert.Equal(new DateOnly(1995, 8, 20), reloaded.DateOfBirth);
        Assert.Contains("1995", reloaded.DateOfBirthDisplay);
        Assert.False(reloaded.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════════
    //  CROSS-SECTION ISOLATION
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AthleteSave_DoesNotMutateCoachFields()
    {
        var initial = new UserProfileResponse
        {
            Id = 1, FirstName = "Test", LastName = "User", Email = "t@t.com",
            TrainingDaysPerWeek = 5, PrimaryTrainingGoal = "Strength",
            AvailableEquipment = "Barbell", InjuryHistory = "ACL 2020"
        };
        var store = new FakeProfileStore(initial);
        var (athlete, _) = LoadPage(store);

        // Save athlete data only
        athlete.DateOfBirth = new DateOnly(2000, 1, 1);
        athlete.WeightText = "80";
        await athlete.SaveAsync();

        // Coach fields preserved on reload
        var (_, coachReloaded) = LoadPage(store);
        Assert.Equal("5", coachReloaded.SelectedTrainingDays);
        Assert.Equal("Strength", coachReloaded.SelectedPrimaryGoal);
        Assert.Equal("Barbell", coachReloaded.EquipmentText);
        Assert.Equal("ACL 2020", coachReloaded.InjuryHistoryText);
    }

    [Fact]
    public async Task CoachSave_DoesNotMutateAthleteFields()
    {
        var initial = new UserProfileResponse
        {
            Id = 1, FirstName = "Test", LastName = "User", Email = "t@t.com",
            WeightKg = 90, SportName = "Basketball", Position = "Center",
            GymExperienceLevel = "5+ years", DateOfBirth = new DateOnly(1998, 3, 10)
        };
        var store = new FakeProfileStore(initial);
        var (_, coach) = LoadPage(store);

        // Save coach data only
        coach.SelectedTrainingDays = "3";
        await CoachSaveWithEditorPush(coach, "Dumbbells", "", "", "");

        // Athlete fields preserved on reload
        var (athleteReloaded, _) = LoadPage(store);
        Assert.Equal("90", athleteReloaded.WeightText);
        Assert.Equal("Basketball", athleteReloaded.SelectedSport?.Name);
        Assert.Equal("Center", athleteReloaded.SelectedPosition);
        Assert.Equal("5+ years", athleteReloaded.SelectedGymExperience);
        Assert.Equal(new DateOnly(1998, 3, 10), athleteReloaded.DateOfBirth);
    }

    // ════════════════════════════════════════════════════════════════════
    //  COACH EDITOR PUSH-AT-SAVE-TIME BEHAVIOR
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CoachEditors_NotSyncedUntilSaveTime()
    {
        var store = new FakeProfileStore();
        var (_, coach) = LoadPage(store);

        // Simulate: user types in equipment editor but hasn't saved yet
        // On the real page, Editor.Text changes do NOT auto-sync to the VM
        // The VM's EquipmentText stays at its hydrated value until save
        Assert.Equal("", coach.EquipmentText);

        // Only after explicit push (which happens in OnSaveCoachProfileClicked)
        // do the values flow into the VM and become part of the save request
        coach.SelectedTrainingDays = "3";
        var success = await CoachSaveWithEditorPush(coach,
            equipmentText: "Full gym",
            limitationsText: "Bad knee",
            injuryHistoryText: "Torn ACL",
            painPointsText: "Lower back");

        Assert.True(success);
        Assert.Equal("Full gym", store.LastCoachSave!.AvailableEquipment);
        Assert.Equal("Bad knee", store.LastCoachSave.PhysicalLimitations);
        Assert.Equal("Torn ACL", store.LastCoachSave.InjuryHistory);
        Assert.Equal("Lower back", store.LastCoachSave.CurrentPainPoints);
    }
}
