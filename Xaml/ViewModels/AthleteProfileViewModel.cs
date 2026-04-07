using System.ComponentModel;
using System.Runtime.CompilerServices;
using FreakLete.Services;

namespace FreakLete.ViewModels;

/// <summary>
/// Draft state + save logic for the athlete profile section.
/// All athlete fields live here; ProfilePage binds/reads from this VM.
/// Save flow: hydrate from server → edit draft → save typed request → rehydrate from response.
/// </summary>
public class AthleteProfileViewModel : INotifyPropertyChanged
{
    /// <summary>Delegate that sends the save request and returns the API result.</summary>
    public delegate Task<ApiResult<UserProfileResponse>> SaveDelegate(SaveAthleteProfileRequest request);

    private readonly SaveDelegate _save;
    private readonly List<SportDefinitionResponse> _sportCatalog;

    // ── Draft state ──────────────────────────────────────────────

    private DateOnly? _dateOfBirth;
    private string _weightText = "";
    private string _bodyFatText = "";
    private string _heightText = "";
    private SportDefinitionResponse? _selectedSport;
    private string? _selectedPosition;
    private string? _selectedGymExperience;
    private string? _selectedSex;
    private bool _isSaving;
    private bool _isDirty;
    private string? _saveError;
    private bool _saveSucceeded;

    // Snapshot of server-confirmed values for dirty tracking
    private DateOnly? _serverDateOfBirth;
    private string _serverWeightText = "";
    private string _serverBodyFatText = "";
    private string _serverHeightText = "";
    private string? _serverSportName;
    private string? _serverPosition;
    private string? _serverGymExperience;
    private string? _serverSex;

    public AthleteProfileViewModel(SaveDelegate save, List<SportDefinitionResponse> sportCatalog)
    {
        _save = save;
        _sportCatalog = sportCatalog;
    }

    // ── Public properties ────────────────────────────────────────

    public DateOnly? DateOfBirth
    {
        get => _dateOfBirth;
        set { if (SetField(ref _dateOfBirth, value)) UpdateDirty(); }
    }

    public string WeightText
    {
        get => _weightText;
        set { if (SetField(ref _weightText, value ?? "")) UpdateDirty(); }
    }

    public string BodyFatText
    {
        get => _bodyFatText;
        set { if (SetField(ref _bodyFatText, value ?? "")) UpdateDirty(); }
    }

    public string HeightText
    {
        get => _heightText;
        set { if (SetField(ref _heightText, value ?? "")) UpdateDirty(); }
    }

    public SportDefinitionResponse? SelectedSport
    {
        get => _selectedSport;
        set
        {
            if (SetField(ref _selectedSport, value))
            {
                // Sport change → resolve position coherence
                ResolvePositionForSport(currentPosition: _selectedPosition);
                OnPropertyChanged(nameof(ShowPositionSelector));
                UpdateDirty();
            }
        }
    }

    public string? SelectedPosition
    {
        get => _selectedPosition;
        set { if (SetField(ref _selectedPosition, value)) UpdateDirty(); }
    }

    public string? SelectedGymExperience
    {
        get => _selectedGymExperience;
        set { if (SetField(ref _selectedGymExperience, value)) UpdateDirty(); }
    }

    public string? SelectedSex
    {
        get => _selectedSex;
        set { if (SetField(ref _selectedSex, value)) UpdateDirty(); }
    }

    public bool ShowPositionSelector =>
        _selectedSport is not null && _selectedSport.HasPositions && _selectedSport.Positions.Count > 0;

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

    public string DateOfBirthDisplay =>
        _dateOfBirth.HasValue
            ? ProfileStateManager.FormatDateOfBirth(_dateOfBirth)!
            : "Select date of birth";

    public string AgeDisplay
    {
        get
        {
            var age = ProfileStateManager.CalculateAge(_dateOfBirth, DateOnly.FromDateTime(DateTime.Today));
            return age.HasValue ? $"Age: {age}" : "Age: -";
        }
    }

    // ── Hydrate from server profile ──────────────────────────────

    public void HydrateFromProfile(UserProfileResponse profile)
    {
        _dateOfBirth = profile.DateOfBirth;
        _weightText = profile.WeightKg?.ToString("0.##") ?? "";
        _bodyFatText = profile.BodyFatPercentage?.ToString("0.##") ?? "";
        _heightText = profile.HeightCm?.ToString("0.##") ?? "";
        _selectedGymExperience = string.IsNullOrWhiteSpace(profile.GymExperienceLevel) ? null : profile.GymExperienceLevel;
        _selectedSex = string.IsNullOrWhiteSpace(profile.Sex) ? null : profile.Sex;

        // Resolve sport from catalog
        _selectedSport = string.IsNullOrWhiteSpace(profile.SportName)
            ? null
            : _sportCatalog.FirstOrDefault(s =>
                string.Equals(s.Name, profile.SportName, StringComparison.OrdinalIgnoreCase));

        // Resolve position
        if (_selectedSport is not null && _selectedSport.HasPositions && _selectedSport.Positions.Count > 0)
        {
            _selectedPosition = string.IsNullOrWhiteSpace(profile.Position)
                ? null
                : _selectedSport.Positions.FirstOrDefault(p =>
                    string.Equals(p, profile.Position, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            _selectedPosition = null;
        }

        // Snapshot server state
        _serverDateOfBirth = _dateOfBirth;
        _serverWeightText = _weightText;
        _serverBodyFatText = _bodyFatText;
        _serverHeightText = _heightText;
        _serverSportName = _selectedSport?.Name;
        _serverPosition = _selectedPosition;
        _serverGymExperience = _selectedGymExperience;
        _serverSex = _selectedSex;

        // Reset status
        _isDirty = false;
        _saveError = null;
        _saveSucceeded = false;
        _isSaving = false;

        // Notify all properties changed
        OnPropertyChanged("");
    }

    // ── Validation ───────────────────────────────────────────────

    public (bool IsValid, string? Error) Validate()
    {
        return ProfileStateManager.ValidateAthleteFields(_weightText, _bodyFatText, _heightText);
    }

    // ── Save ─────────────────────────────────────────────────────

    public async Task<bool> SaveAsync()
    {
        SaveError = null;
        SaveSucceeded = false;

        var (isValid, error) = Validate();
        if (!isValid)
        {
            SaveError = error;
            return false;
        }

        IsSaving = true;

        var request = new SaveAthleteProfileRequest
        {
            DateOfBirth = _dateOfBirth,
            WeightKg = ProfileStateManager.ParseNullableDouble(_weightText),
            BodyFatPercentage = ProfileStateManager.ParseNullableDouble(_bodyFatText),
            HeightCm = ProfileStateManager.ParseNullableDouble(_heightText),
            Sex = _selectedSex,
            SportName = _selectedSport?.Name,
            Position = ShowPositionSelector ? _selectedPosition : null,
            GymExperienceLevel = _selectedGymExperience
        };

        var result = await _save(request);
        IsSaving = false;

        if (result.Success && result.Data is not null)
        {
            // Rehydrate from server-confirmed response — single source of truth
            HydrateFromProfile(result.Data);
            SaveSucceeded = true;
            return true;
        }

        SaveError = result.Error ?? "Failed to save athlete profile.";
        return false;
    }

    // ── Sport/position coherence ─────────────────────────────────

    public void ResolvePositionForSport(string? currentPosition)
    {
        if (_selectedSport is null ||
            !_selectedSport.HasPositions ||
            _selectedSport.Positions.Count == 0)
        {
            _selectedPosition = null;
        }
        else if (!string.IsNullOrWhiteSpace(currentPosition))
        {
            // Keep position if it matches new sport's positions
            _selectedPosition = _selectedSport.Positions.FirstOrDefault(p =>
                string.Equals(p, currentPosition, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            _selectedPosition = null;
        }

        OnPropertyChanged(nameof(SelectedPosition));
    }

    // ── Dirty tracking ───────────────────────────────────────────

    private void UpdateDirty()
    {
        IsDirty = _dateOfBirth != _serverDateOfBirth
            || _weightText != _serverWeightText
            || _bodyFatText != _serverBodyFatText
            || _heightText != _serverHeightText
            || _selectedSport?.Name != _serverSportName
            || _selectedPosition != _serverPosition
            || _selectedGymExperience != _serverGymExperience
            || _selectedSex != _serverSex;
    }

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
