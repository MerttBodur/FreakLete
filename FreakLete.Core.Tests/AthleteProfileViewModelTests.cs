using FreakLete.Services;
using FreakLete.ViewModels;

namespace FreakLete.Core.Tests;

public class AthleteProfileViewModelTests
{
    private static readonly List<SportDefinitionResponse> TestCatalog =
    [
        new() { Id = "soccer", Name = "Soccer", Category = "Team Sports", HasPositions = true,
            Positions = ["Goalkeeper", "Center Back", "Striker"] },
        new() { Id = "tennis", Name = "Tennis", Category = "Racket Sports", HasPositions = false },
        new() { Id = "basketball", Name = "Basketball", Category = "Team Sports", HasPositions = true,
            Positions = ["Point Guard", "Center"] },
    ];

    private static AthleteProfileViewModel CreateVm(
        AthleteProfileViewModel.SaveDelegate? save = null)
    {
        save ??= _ => Task.FromResult(ApiResult<UserProfileResponse>.Fail("not wired"));
        return new AthleteProfileViewModel(save, TestCatalog);
    }

    private static UserProfileResponse MakeProfile(
        DateOnly? dob = null, double? weight = null, double? bodyFat = null,
        string sport = "", string position = "", string gym = "")
    {
        return new UserProfileResponse
        {
            Id = 1, FirstName = "Test", LastName = "User", Email = "t@t.com",
            DateOfBirth = dob, WeightKg = weight, BodyFatPercentage = bodyFat,
            SportName = sport, Position = position, GymExperienceLevel = gym
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
            dob: new DateOnly(2000, 6, 15),
            weight: 82.5,
            bodyFat: 14.2,
            sport: "Soccer",
            position: "Goalkeeper",
            gym: "Intermediate"));

        Assert.Equal(new DateOnly(2000, 6, 15), vm.DateOfBirth);
        Assert.Equal(82.5.ToString("0.##"), vm.WeightText);
        Assert.Equal(14.2.ToString("0.##"), vm.BodyFatText);
        Assert.Equal("Soccer", vm.SelectedSport?.Name);
        Assert.Equal("Goalkeeper", vm.SelectedPosition);
        Assert.Equal("Intermediate", vm.SelectedGymExperience);
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void HydrateFromProfile_NullFields_ClearsState()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile()); // all defaults/null

        Assert.Null(vm.DateOfBirth);
        Assert.Equal("", vm.WeightText);
        Assert.Equal("", vm.BodyFatText);
        Assert.Null(vm.SelectedSport);
        Assert.Null(vm.SelectedPosition);
        Assert.Null(vm.SelectedGymExperience);
        Assert.False(vm.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════
    //  DOB PLACEHOLDER / AGE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DateOfBirthDisplay_NoDob_ShowsPlaceholder()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile());
        Assert.Equal("Select date of birth", vm.DateOfBirthDisplay);
    }

    [Fact]
    public void DateOfBirthDisplay_WithDob_ShowsFormattedDate()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(dob: new DateOnly(2000, 6, 15)));
        Assert.Contains("2000", vm.DateOfBirthDisplay);
        Assert.DoesNotContain("Select", vm.DateOfBirthDisplay);
    }

    [Fact]
    public void AgeDisplay_NoDob_ShowsDash()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile());
        Assert.Equal("Age: -", vm.AgeDisplay);
    }

    [Fact]
    public void AgeDisplay_WithDob_ShowsAge()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(dob: new DateOnly(2000, 1, 1)));
        Assert.StartsWith("Age: ", vm.AgeDisplay);
        Assert.DoesNotContain("-", vm.AgeDisplay.Substring(5));
    }

    // ════════════════════════════════════════════════════════════════
    //  SPORT → POSITION COHERENCE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SportWithPositions_ShowsPositionSelector()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(sport: "Soccer", position: "Goalkeeper"));
        Assert.True(vm.ShowPositionSelector);
    }

    [Fact]
    public void SportWithoutPositions_HidesPositionSelector()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(sport: "Tennis"));
        Assert.False(vm.ShowPositionSelector);
        Assert.Null(vm.SelectedPosition);
    }

    [Fact]
    public void ChangingSport_ClearsIncompatiblePosition()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(sport: "Soccer", position: "Goalkeeper"));
        Assert.Equal("Goalkeeper", vm.SelectedPosition);

        // Switch to Basketball — Goalkeeper is not a basketball position
        vm.SelectedSport = TestCatalog.First(s => s.Name == "Basketball");
        Assert.Null(vm.SelectedPosition);
        Assert.True(vm.ShowPositionSelector);
    }

    [Fact]
    public void ChangingSportToNoPositions_ClearsPosition()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(sport: "Soccer", position: "Goalkeeper"));

        vm.SelectedSport = TestCatalog.First(s => s.Name == "Tennis");
        Assert.Null(vm.SelectedPosition);
        Assert.False(vm.ShowPositionSelector);
    }

    [Fact]
    public void ChangingSport_KeepsCompatiblePosition()
    {
        // "Center" exists in Basketball
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(sport: "Basketball", position: "Center"));

        // Re-set same sport — position should survive
        vm.SelectedSport = TestCatalog.First(s => s.Name == "Basketball");
        Assert.Equal("Center", vm.SelectedPosition);
    }

    [Fact]
    public void ClearingSport_ClearsPosition()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(sport: "Soccer", position: "Goalkeeper"));

        vm.SelectedSport = null;
        Assert.Null(vm.SelectedPosition);
        Assert.False(vm.ShowPositionSelector);
    }

    // ════════════════════════════════════════════════════════════════
    //  DIRTY STATE
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FreshHydration_IsNotDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(weight: 80));
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void ChangingWeight_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(weight: 80));
        vm.WeightText = "85";
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void ChangingWeightBack_ClearsDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(weight: 80));
        vm.WeightText = "85";
        Assert.True(vm.IsDirty);
        vm.WeightText = "80";
        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void ChangingDob_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(dob: new DateOnly(2000, 1, 1)));
        vm.DateOfBirth = new DateOnly(1999, 6, 15);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void ChangingSport_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(sport: "Soccer"));
        vm.SelectedSport = TestCatalog.First(s => s.Name == "Tennis");
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void ChangingGymExperience_MakesDirty()
    {
        var vm = CreateVm();
        vm.HydrateFromProfile(MakeProfile(gym: "Beginner"));
        vm.SelectedGymExperience = "Advanced";
        Assert.True(vm.IsDirty);
    }

    // ════════════════════════════════════════════════════════════════
    //  VALIDATION
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Validate_ValidFields_IsValid()
    {
        var vm = CreateVm();
        vm.WeightText = "80";
        vm.BodyFatText = "15";
        var (valid, error) = vm.Validate();
        Assert.True(valid);
        Assert.Null(error);
    }

    [Fact]
    public void Validate_InvalidWeight_ReturnsError()
    {
        var vm = CreateVm();
        vm.WeightText = "500";
        vm.BodyFatText = "15";
        var (valid, error) = vm.Validate();
        Assert.False(valid);
        Assert.NotNull(error);
    }

    // ════════════════════════════════════════════════════════════════
    //  SAVE — successful save refreshes from returned profile
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAsync_Success_RehydratesFromResponse()
    {
        var serverProfile = MakeProfile(
            weight: 85, bodyFat: 16, sport: "Soccer", position: "Striker", gym: "Advanced");

        var vm = CreateVm(save: _ =>
            Task.FromResult(ApiResult<UserProfileResponse>.Ok(serverProfile)));

        vm.HydrateFromProfile(MakeProfile(weight: 80));
        vm.WeightText = "85";

        var result = await vm.SaveAsync();

        Assert.True(result);
        Assert.True(vm.SaveSucceeded);
        Assert.Null(vm.SaveError);
        // Rehydrated from server response
        Assert.Equal(85.0.ToString("0.##"), vm.WeightText);
        Assert.Equal(16.0.ToString("0.##"), vm.BodyFatText);
        Assert.Equal("Soccer", vm.SelectedSport?.Name);
        Assert.False(vm.IsDirty); // rehydrated = not dirty
    }

    // ════════════════════════════════════════════════════════════════
    //  SAVE — failed save does not fake success
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveAsync_Failure_DoesNotFakeSuccess()
    {
        var vm = CreateVm(save: _ =>
            Task.FromResult(ApiResult<UserProfileResponse>.Fail("Server error", 500)));

        vm.HydrateFromProfile(MakeProfile(weight: 80));
        vm.WeightText = "85";

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.False(vm.SaveSucceeded);
        Assert.Equal("Server error", vm.SaveError);
        // Draft state preserved (not rehydrated)
        Assert.Equal("85", vm.WeightText);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public async Task SaveAsync_ValidationFailure_DoesNotCallApi()
    {
        bool apiCalled = false;
        var vm = CreateVm(save: _ =>
        {
            apiCalled = true;
            return Task.FromResult(ApiResult<UserProfileResponse>.Ok(MakeProfile()));
        });

        vm.WeightText = "999"; // invalid
        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.False(apiCalled);
        Assert.NotNull(vm.SaveError);
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

        vm.WeightText = "90";

        Assert.Contains("WeightText", changed);
        Assert.Contains("IsDirty", changed);
    }
}
