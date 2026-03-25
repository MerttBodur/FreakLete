using FreakLete.Services;
using FreakLete.ViewModels;

namespace FreakLete.Core.Tests;

public class CoachProfileViewModelTests
{
    private static CoachProfileViewModel CreateVm(
        CoachProfileViewModel.SaveDelegate? save = null)
    {
        save ??= _ => Task.FromResult(ApiResult<UserProfileResponse>.Fail("not wired"));
        return new CoachProfileViewModel(save);
    }

    private static UserProfileResponse MakeProfile(
        int? trainingDays = null, int? sessionDuration = null,
        string primaryGoal = "", string secondaryGoal = "",
        string dietary = "", string equipment = "",
        string limitations = "", string injury = "", string pain = "")
    {
        return new UserProfileResponse
        {
            Id = 1, FirstName = "Test", LastName = "User", Email = "t@t.com",
            TrainingDaysPerWeek = trainingDays,
            PreferredSessionDurationMinutes = sessionDuration,
            PrimaryTrainingGoal = primaryGoal,
            SecondaryTrainingGoal = secondaryGoal,
            DietaryPreference = dietary,
            AvailableEquipment = equipment,
            PhysicalLimitations = limitations,
            InjuryHistory = injury,
            CurrentPainPoints = pain
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  HYDRATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void HydrateFromProfile_SetsAllFields()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(
            trainingDays: 5, sessionDuration: 90,
            primaryGoal: "Strength", secondaryGoal: "Hypertrophy",
            dietary: "High Protein", equipment: "Full gym",
            limitations: "None", injury: "ACL 2020", pain: "Knee"));

        Assert.Equal("5", vm.SelectedTrainingDays);
        Assert.Equal("90", vm.SelectedSessionDuration);
        Assert.Equal("Strength", vm.SelectedPrimaryGoal);
        Assert.Equal("Hypertrophy", vm.SelectedSecondaryGoal);
        Assert.Equal("High Protein", vm.SelectedDietaryPreference);
        Assert.Equal("Full gym", vm.EquipmentText);
        Assert.Equal("None", vm.LimitationsText);
        Assert.Equal("ACL 2020", vm.InjuryHistoryText);
        Assert.Equal("Knee", vm.PainPointsText);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void HydrateFromProfile_NullFields_ClearsState()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile()); // all defaults/null

        Assert.Null(vm.SelectedTrainingDays);
        Assert.Null(vm.SelectedSessionDuration);
        Assert.Null(vm.SelectedPrimaryGoal);
        Assert.Null(vm.SelectedSecondaryGoal);
        Assert.Null(vm.SelectedDietaryPreference);
        Assert.Equal("", vm.EquipmentText);
        Assert.Equal("", vm.LimitationsText);
        Assert.Equal("", vm.InjuryHistoryText);
        Assert.Equal("", vm.PainPointsText);
        Assert.False(vm.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════
    //  DIRTY STATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FreshHydration_IsNotDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(trainingDays: 5));
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void ChangingTrainingDays_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(trainingDays: 5));
        vm.SelectedTrainingDays = "4";
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void ChangingTrainingDaysBack_ClearsDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(trainingDays: 5));
        vm.SelectedTrainingDays = "4";
        Assert.True(vm.IsDirty);
        vm.SelectedTrainingDays = "5";
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void ChangingPrimaryGoal_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(primaryGoal: "Strength"));
        vm.SelectedPrimaryGoal = "Hypertrophy";
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void ChangingEquipmentText_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(equipment: "Barbell"));
        vm.EquipmentText = "Barbell, Dumbbells";
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void ChangingInjuryHistory_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(injury: "ACL 2020"));
        vm.InjuryHistoryText = "ACL 2020, Shoulder 2022";
        Assert.True(vm.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════
    //  SAVE — successful save refreshes from returned profile
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAsync_Success_RehydratesFromResponse()
    {
        var serverProfile = MakeProfile(
            trainingDays: 6, sessionDuration: 60,
            primaryGoal: "Hypertrophy", dietary: "High Protein",
            equipment: "Full gym", injury: "None");

        var vm = CreateVm(save: _ =>
            Task.FromResult(ApiResult<UserProfileResponse>.Ok(serverProfile)));

        vm.HydrateFromProfile(MakeProfile(trainingDays: 5));
        vm.SelectedTrainingDays = "6";

        var result = await vm.SaveAsync();

        Assert.True(result);
        Assert.True(vm.SaveSucceeded);
        Assert.Null(vm.SaveError);
        Assert.Equal("6", vm.SelectedTrainingDays);
        Assert.Equal("60", vm.SelectedSessionDuration);
        Assert.Equal("Hypertrophy", vm.SelectedPrimaryGoal);
        Assert.Equal("High Protein", vm.SelectedDietaryPreference);
        Assert.False(vm.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════
    //  SAVE — failed save does not fake success
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAsync_Failure_DoesNotFakeSuccess()
    {
        var vm = CreateVm(save: _ =>
            Task.FromResult(ApiResult<UserProfileResponse>.Fail("Server error", 500)));

        vm.HydrateFromProfile(MakeProfile(trainingDays: 5));
        vm.SelectedTrainingDays = "6";

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.False(vm.SaveSucceeded);
        Assert.Equal("Server error", vm.SaveError);
        // Draft state preserved (not rehydrated)
        Assert.Equal("6", vm.SelectedTrainingDays);
        Assert.True(vm.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════
    //  PROPERTY CHANGED NOTIFICATIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SettingProperty_RaisesPropertyChanged()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile());

        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.SelectedTrainingDays = "3";

        Assert.Contains("SelectedTrainingDays", changed);
        Assert.Contains("IsDirty", changed);
    }
}
