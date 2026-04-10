using FreakLete.Services;
using Microcharts;
using SkiaSharp;

namespace FreakLete;

public partial class ExerciseComparisonChart : ContentView
{
    // ── Bindable properties ──────────────────────────────────────────────────

    public static readonly BindableProperty Exercise1NameProperty =
        BindableProperty.Create(nameof(Exercise1Name), typeof(string), typeof(ExerciseComparisonChart), "Bench Press", propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty Exercise2NameProperty =
        BindableProperty.Create(nameof(Exercise2Name), typeof(string), typeof(ExerciseComparisonChart), "Squat", propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty Exercise1DataProperty =
        BindableProperty.Create(nameof(Exercise1Data), typeof(List<float>), typeof(ExerciseComparisonChart), null, propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty Exercise2DataProperty =
        BindableProperty.Create(nameof(Exercise2Data), typeof(List<float>), typeof(ExerciseComparisonChart), null, propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty Exercise1DeltaProperty =
        BindableProperty.Create(nameof(Exercise1Delta), typeof(string), typeof(ExerciseComparisonChart), "-", propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty Exercise2DeltaProperty =
        BindableProperty.Create(nameof(Exercise2Delta), typeof(string), typeof(ExerciseComparisonChart), "-", propertyChanged: OnPropertyChanged);

    /// <summary>Generic axis labels — replaces the old DayLabels.</summary>
    public static readonly BindableProperty AxisLabelsProperty =
        BindableProperty.Create(nameof(AxisLabels), typeof(List<string>), typeof(ExerciseComparisonChart), null, propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty Exercise1UnitProperty =
        BindableProperty.Create(nameof(Exercise1Unit), typeof(string), typeof(ExerciseComparisonChart), "kg", propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty Exercise2UnitProperty =
        BindableProperty.Create(nameof(Exercise2Unit), typeof(string), typeof(ExerciseComparisonChart), "kg", propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty SelectedRangeProperty =
        BindableProperty.Create(nameof(SelectedRange), typeof(ChartDataHelper.ChartRange), typeof(ExerciseComparisonChart),
            ChartDataHelper.ChartRange.Days14, propertyChanged: OnRangePropertyChanged);

    // ── Properties ───────────────────────────────────────────────────────────

    public string Exercise1Name
    {
        get => (string)GetValue(Exercise1NameProperty);
        set => SetValue(Exercise1NameProperty, value);
    }

    public string Exercise2Name
    {
        get => (string)GetValue(Exercise2NameProperty);
        set => SetValue(Exercise2NameProperty, value);
    }

    public List<float> Exercise1Data
    {
        get => (List<float>)GetValue(Exercise1DataProperty);
        set => SetValue(Exercise1DataProperty, value);
    }

    public List<float> Exercise2Data
    {
        get => (List<float>)GetValue(Exercise2DataProperty);
        set => SetValue(Exercise2DataProperty, value);
    }

    public string Exercise1Delta
    {
        get => (string)GetValue(Exercise1DeltaProperty);
        set => SetValue(Exercise1DeltaProperty, value);
    }

    public string Exercise2Delta
    {
        get => (string)GetValue(Exercise2DeltaProperty);
        set => SetValue(Exercise2DeltaProperty, value);
    }

    public List<string> AxisLabels
    {
        get => (List<string>)GetValue(AxisLabelsProperty);
        set => SetValue(AxisLabelsProperty, value);
    }

    public string Exercise1Unit
    {
        get => (string)GetValue(Exercise1UnitProperty);
        set => SetValue(Exercise1UnitProperty, value);
    }

    public string Exercise2Unit
    {
        get => (string)GetValue(Exercise2UnitProperty);
        set => SetValue(Exercise2UnitProperty, value);
    }

    public ChartDataHelper.ChartRange SelectedRange
    {
        get => (ChartDataHelper.ChartRange)GetValue(SelectedRangeProperty);
        set => SetValue(SelectedRangeProperty, value);
    }

    // ── Legacy alias kept for callers still using DayLabels ──────────────────
    [Obsolete("Use AxisLabels instead")]
    public List<string> DayLabels
    {
        get => AxisLabels;
        set => AxisLabels = value;
    }

    // ── Events ───────────────────────────────────────────────────────────────

    public event EventHandler? ChangeExercisesClicked;
    public event EventHandler<ChartDataHelper.ChartRange>? RangeChanged;

    // ── Constructor ──────────────────────────────────────────────────────────

    public ExerciseComparisonChart()
    {
        InitializeComponent();
        ApplyLanguage();
        Loaded += (_, _) => AppLanguage.LanguageChanged += OnLanguageChanged;
        Unloaded += (_, _) => AppLanguage.LanguageChanged -= OnLanguageChanged;
    }

    // ── Language ─────────────────────────────────────────────────────────────

    private void OnLanguageChanged()
    {
        ApplyLanguage();
        ApplyRangeButtonStyles();
        ApplyState();
    }

    private void ApplyLanguage()
    {
        ChangeButtonLabel.Text = AppLanguage.ChartChange;
        EmptyStateLabel.Text = AppLanguage.ChartNoData;
        RangeLabel14.Text = AppLanguage.ChartRange14Days;
        RangeLabel1M.Text = AppLanguage.ChartRange1Month;
        RangeLabel3M.Text = AppLanguage.ChartRange3Months;
        RangeLabel6M.Text = AppLanguage.ChartRange6Months;
        ApplyRangeSubtitle();
    }

    private void ApplyRangeSubtitle()
    {
        SubtitleLabel.Text = SelectedRange switch
        {
            ChartDataHelper.ChartRange.Days14  => AppLanguage.ChartSubtitleDays14,
            ChartDataHelper.ChartRange.Month1  => AppLanguage.ChartSubtitleMonth1,
            ChartDataHelper.ChartRange.Months3 => AppLanguage.ChartSubtitleMonths3,
            ChartDataHelper.ChartRange.Months6 => AppLanguage.ChartSubtitleMonths6,
            _                                  => AppLanguage.ChartSubtitleDays14
        };
    }

    // ── Property changed callbacks ────────────────────────────────────────────

    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ExerciseComparisonChart chart)
            chart.ApplyState();
    }

    private static void OnRangePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ExerciseComparisonChart chart)
        {
            chart.ApplyRangeSubtitle();
            chart.ApplyRangeButtonStyles();
        }
    }

    // ── Range button taps ─────────────────────────────────────────────────────

    private void OnRange14Tapped(object? sender, EventArgs e) => SelectRange(ChartDataHelper.ChartRange.Days14);
    private void OnRange1MTapped(object? sender, EventArgs e) => SelectRange(ChartDataHelper.ChartRange.Month1);
    private void OnRange3MTapped(object? sender, EventArgs e) => SelectRange(ChartDataHelper.ChartRange.Months3);
    private void OnRange6MTapped(object? sender, EventArgs e) => SelectRange(ChartDataHelper.ChartRange.Months6);

    private void SelectRange(ChartDataHelper.ChartRange range)
    {
        if (SelectedRange == range) return;
        SelectedRange = range;
        RangeChanged?.Invoke(this, range);
    }

    private void ApplyRangeButtonStyles()
    {
        var activeColor   = Color.FromArgb("#8B5CF6"); // Accent
        var inactiveColor = Color.FromArgb("#2F2346"); // AccentSoft
        var activeFg      = Color.FromArgb("#F7F7FB"); // TextPrimary
        var inactiveFg    = Color.FromArgb("#A78BFA"); // AccentGlow

        SetRangeButton(RangeBtn14, RangeLabel14, SelectedRange == ChartDataHelper.ChartRange.Days14,  activeColor, inactiveColor, activeFg, inactiveFg);
        SetRangeButton(RangeBtn1M, RangeLabel1M, SelectedRange == ChartDataHelper.ChartRange.Month1,  activeColor, inactiveColor, activeFg, inactiveFg);
        SetRangeButton(RangeBtn3M, RangeLabel3M, SelectedRange == ChartDataHelper.ChartRange.Months3, activeColor, inactiveColor, activeFg, inactiveFg);
        SetRangeButton(RangeBtn6M, RangeLabel6M, SelectedRange == ChartDataHelper.ChartRange.Months6, activeColor, inactiveColor, activeFg, inactiveFg);
    }

    private static void SetRangeButton(Border btn, Label lbl, bool active,
        Color activeBg, Color inactiveBg, Color activeFg, Color inactiveFg)
    {
        btn.BackgroundColor = active ? activeBg : inactiveBg;
        lbl.TextColor = active ? activeFg : inactiveFg;
    }

    // ── Change button tap ─────────────────────────────────────────────────────

    private void OnChangeButtonTapped(object? sender, EventArgs e)
    {
        ChangeExercisesClicked?.Invoke(this, EventArgs.Empty);
    }

    // ── Main state render ─────────────────────────────────────────────────────

    private void ApplyState()
    {
        TitleLabel.Text = $"{Exercise1Name} vs {Exercise2Name}";
        Badge1Label.Text = $"{Exercise1Name} {Exercise1Delta}";
        Badge2Label.Text = $"{Exercise2Name} {Exercise2Delta}";
        Legend1Label.Text = $"{Exercise1Name} ({Exercise1Unit})";
        Legend2Label.Text = $"{Exercise2Name} ({Exercise2Unit})";

        ApplyRangeButtonStyles();
        ApplyRangeSubtitle();

        var data1  = Exercise1Data;
        var data2  = Exercise2Data;
        var labels = AxisLabels;

        bool hasData = (data1 != null && data1.Any(v => v > 0)) ||
                       (data2 != null && data2.Any(v => v > 0));

        EmptyStateLabel.IsVisible = !hasData;
        ChartView1.IsVisible = hasData;
        ChartView2.IsVisible = hasData;
        LegendRow.IsVisible = hasData;

        if (!hasData) return;

        float allMax = 0;
        if (data1 != null) allMax = Math.Max(allMax, data1.Max());
        if (data2 != null) allMax = Math.Max(allMax, data2.Max());
        if (allMax == 0) allMax = 1;

        var accentColor      = SKColor.Parse("#8B5CF6");
        var successColor     = SKColor.Parse("#22C55E");
        var transparentBg    = SKColor.Parse("#00000000");
        var labelColor       = SKColor.Parse("#B3B2C5");

        if (data1 != null && data1.Count > 0)
        {
            var entries1 = new List<ChartEntry>();
            for (int i = 0; i < data1.Count; i++)
            {
                entries1.Add(new ChartEntry(data1[i])
                {
                    Color      = accentColor,
                    Label      = labels != null && i < labels.Count ? labels[i] : "",
                    ValueLabel = data1[i] > 0 ? data1[i].ToString("0") : ""
                });
            }

            ChartView1.Chart = new LineChart
            {
                Entries                  = entries1,
                BackgroundColor          = transparentBg,
                LineSize                 = 3,
                PointSize                = 6,
                LabelColor               = labelColor,
                LabelTextSize            = 24,
                ValueLabelTextSize       = 20,
                MinValue                 = 0,
                MaxValue                 = allMax,
                LineMode                 = LineMode.Spline,
                PointMode                = PointMode.Circle,
                ValueLabelOrientation    = Orientation.Horizontal,
                LabelOrientation         = Orientation.Horizontal
            };
        }

        if (data2 != null && data2.Count > 0)
        {
            var entries2 = new List<ChartEntry>();
            for (int i = 0; i < data2.Count; i++)
            {
                entries2.Add(new ChartEntry(data2[i])
                {
                    Color      = successColor,
                    Label      = "",
                    ValueLabel = data2[i] > 0 ? data2[i].ToString("0") : ""
                });
            }

            ChartView2.Chart = new LineChart
            {
                Entries               = entries2,
                BackgroundColor       = transparentBg,
                LineSize              = 3,
                PointSize             = 6,
                LabelColor            = transparentBg,
                ValueLabelTextSize    = 20,
                MinValue              = 0,
                MaxValue              = allMax,
                LineMode              = LineMode.Spline,
                PointMode             = PointMode.Circle,
                ValueLabelOrientation = Orientation.Horizontal,
                LabelOrientation      = Orientation.Horizontal
            };
        }
    }
}
