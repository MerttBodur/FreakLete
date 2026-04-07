using System.ComponentModel;
using System.Runtime.CompilerServices;
using FreakLete.Services;

namespace FreakLete.ViewModels;

/// <summary>
/// Draft state + save logic for the coach profile section.
/// Same pattern as AthleteProfileViewModel: hydrate → edit → save → rehydrate from response.
/// </summary>
public class CoachProfileViewModel : INotifyPropertyChanged
{
    public delegate Task<ApiResult<UserProfileResponse>> SaveDelegate(SaveCoachProfileRequest request);

    private readonly SaveDelegate _save;

    // ── Draft state ──────────────────────────────────────────────

    private string? _selectedTrainingDays;
    private string? _selectedSessionDuration;
    private string? _selectedPrimaryGoal;
    private string? _selectedSecondaryGoal;
    private string? _selectedDietaryPreference;
    private string _equipmentText = "";
    private string _limitationsText = "";
    private string _injuryHistoryText = "";
    private string _painPointsText = "";
    private bool _isSaving;
    private bool _isDirty;
    private string? _saveError;
    private bool _saveSucceeded;

    // Snapshot of server-confirmed values for dirty tracking
    private string? _serverTrainingDays;
    private string? _serverSessionDuration;
    private string? _serverPrimaryGoal;
    private string? _serverSecondaryGoal;
    private string? _serverDietaryPreference;
    private string _serverEquipmentText = "";
    private string _serverLimitationsText = "";
    private string _serverInjuryHistoryText = "";
    private string _serverPainPointsText = "";

    public CoachProfileViewModel(SaveDelegate save)
    {
        _save = save;
    }

    // ── Public properties ────────────────────────────────────────

    public string? SelectedTrainingDays
    {
        get => _selectedTrainingDays;
        set { if (SetField(ref _selectedTrainingDays, value)) UpdateDirty(); }
    }

    public string? SelectedSessionDuration
    {
        get => _selectedSessionDuration;
        set { if (SetField(ref _selectedSessionDuration, value)) UpdateDirty(); }
    }

    public string? SelectedPrimaryGoal
    {
        get => _selectedPrimaryGoal;
        set { if (SetField(ref _selectedPrimaryGoal, value)) UpdateDirty(); }
    }

    public string? SelectedSecondaryGoal
    {
        get => _selectedSecondaryGoal;
        set { if (SetField(ref _selectedSecondaryGoal, value)) UpdateDirty(); }
    }

    public string? SelectedDietaryPreference
    {
        get => _selectedDietaryPreference;
        set { if (SetField(ref _selectedDietaryPreference, value)) UpdateDirty(); }
    }

    public string EquipmentText
    {
        get => _equipmentText;
        set { if (SetField(ref _equipmentText, value ?? "")) UpdateDirty(); }
    }

    public string LimitationsText
    {
        get => _limitationsText;
        set { if (SetField(ref _limitationsText, value ?? "")) UpdateDirty(); }
    }

    public string InjuryHistoryText
    {
        get => _injuryHistoryText;
        set { if (SetField(ref _injuryHistoryText, value ?? "")) UpdateDirty(); }
    }

    public string PainPointsText
    {
        get => _painPointsText;
        set { if (SetField(ref _painPointsText, value ?? "")) UpdateDirty(); }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetField(ref _isSaving, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set => SetField(ref _isDirty, value);
    }

    public string? SaveError
    {
        get => _saveError;
        private set => SetField(ref _saveError, value);
    }

    public bool SaveSucceeded
    {
        get => _saveSucceeded;
        private set => SetField(ref _saveSucceeded, value);
    }

    // ── Hydrate from server profile ──────────────────────────────

    public void HydrateFromProfile(UserProfileResponse profile)
    {
        _selectedTrainingDays = profile.TrainingDaysPerWeek?.ToString();
        _selectedSessionDuration = profile.PreferredSessionDurationMinutes?.ToString();
        _selectedPrimaryGoal = NormalizeEmpty(profile.PrimaryTrainingGoal);
        _selectedSecondaryGoal = NormalizeEmpty(profile.SecondaryTrainingGoal);
        _selectedDietaryPreference = NormalizeEmpty(profile.DietaryPreference);
        _equipmentText = profile.AvailableEquipment ?? "";
        _limitationsText = profile.PhysicalLimitations ?? "";
        _injuryHistoryText = profile.InjuryHistory ?? "";
        _painPointsText = profile.CurrentPainPoints ?? "";

        // Snapshot server state
        _serverTrainingDays = _selectedTrainingDays;
        _serverSessionDuration = _selectedSessionDuration;
        _serverPrimaryGoal = _selectedPrimaryGoal;
        _serverSecondaryGoal = _selectedSecondaryGoal;
        _serverDietaryPreference = _selectedDietaryPreference;
        _serverEquipmentText = _equipmentText;
        _serverLimitationsText = _limitationsText;
        _serverInjuryHistoryText = _injuryHistoryText;
        _serverPainPointsText = _painPointsText;

        // Reset status
        _isDirty = false;
        _saveError = null;
        _saveSucceeded = false;
        _isSaving = false;

        // Notify all properties changed
        OnPropertyChanged("");
    }

    // ── Save ─────────────────────────────────────────────────────

    public async Task<bool> SaveAsync()
    {
        SaveError = null;
        SaveSucceeded = false;

        IsSaving = true;

        var request = new SaveCoachProfileRequest
        {
            TrainingDaysPerWeek = _selectedTrainingDays is not null ? int.Parse(_selectedTrainingDays) : null,
            PreferredSessionDurationMinutes = _selectedSessionDuration is not null ? int.Parse(_selectedSessionDuration) : null,
            PrimaryTrainingGoal = _selectedPrimaryGoal,
            SecondaryTrainingGoal = _selectedSecondaryGoal,
            DietaryPreference = _selectedDietaryPreference,
            AvailableEquipment = NormalizeEmpty(_equipmentText),
            PhysicalLimitations = NormalizeEmpty(_limitationsText),
            InjuryHistory = NormalizeEmpty(_injuryHistoryText),
            CurrentPainPoints = NormalizeEmpty(_painPointsText)
        };

        var result = await _save(request);
        IsSaving = false;

        if (result.Success && result.Data is not null)
        {
            HydrateFromProfile(result.Data);
            SaveSucceeded = true;
            return true;
        }

        SaveError = result.Error ?? "Failed to save coach profile.";
        return false;
    }

    // ── Dirty tracking ───────────────────────────────────────────

    private void UpdateDirty()
    {
        IsDirty = _selectedTrainingDays != _serverTrainingDays
            || _selectedSessionDuration != _serverSessionDuration
            || _selectedPrimaryGoal != _serverPrimaryGoal
            || _selectedSecondaryGoal != _serverSecondaryGoal
            || _selectedDietaryPreference != _serverDietaryPreference
            || _equipmentText != _serverEquipmentText
            || _limitationsText != _serverLimitationsText
            || _injuryHistoryText != _serverInjuryHistoryText
            || _painPointsText != _serverPainPointsText;
    }

    private static string? NormalizeEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    // ── INotifyPropertyChanged ───────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
