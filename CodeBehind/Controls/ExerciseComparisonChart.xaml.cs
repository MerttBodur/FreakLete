using FreakLete.Services;

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

        bool hasData = (data1 != null && data1.Count > 0) ||
                       (data2 != null && data2.Count > 0);

        EmptyStateLabel.IsVisible = !hasData;
        ComparisonChartView.IsVisible = hasData;
        LegendRow.IsVisible = hasData;

        if (!hasData) return;

        ComparisonChartView.Drawable = new DualLineChartDrawable(data1, data2, labels);
        ComparisonChartView.Invalidate();
    }

    // ── Custom dual straight-line chart drawable ──────────────────────────────

    private sealed class DualLineChartDrawable : IDrawable
    {
        private readonly List<float>?  _series1;
        private readonly List<float>?  _series2;
        private readonly List<string>? _labels;

        public DualLineChartDrawable(List<float>? s1, List<float>? s2, List<string>? labels)
        {
            _series1 = s1;
            _series2 = s2;
            _labels  = labels;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float w = dirtyRect.Width;
            float h = dirtyRect.Height;
            const float padLeft   = 42f;
            const float padRight  = 12f;
            const float padTop    = 14f;
            const float padBottom = 30f;
            float chartW = w - padLeft - padRight;
            float chartH = h - padTop - padBottom;

            // Determine shared y scale across both series
            float allMax = 0f;
            if (_series1 is { Count: > 0 }) allMax = Math.Max(allMax, _series1.Max());
            if (_series2 is { Count: > 0 }) allMax = Math.Max(allMax, _series2.Max());
            if (allMax <= 0) return;
            float yScale = allMax * 1.1f; // 10% headroom

            // Grid lines + Y-axis labels
            canvas.FontSize   = 9;
            canvas.FontColor  = Color.FromArgb("#5A5474");
            canvas.StrokeColor = Color.FromArgb("#2A2540");
            canvas.StrokeSize  = 1;
            for (int g = 0; g <= 3; g++)
            {
                float ratio = g / 3f;
                float gy = padTop + chartH - (ratio * chartH);
                double val = ratio * yScale;
                canvas.DrawLine(padLeft, gy, padLeft + chartW, gy);
                canvas.DrawString($"{val:0.#}", 0, gy - 8, padLeft - 4, 16,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            float labelY = padTop + chartH + 8;

            // Draw series 1 (accent)
            DrawSeries(canvas, _series1, _labels, padLeft, padTop, chartW, chartH, yScale, labelY,
                Color.FromArgb("#8B5CF6"), Color.FromArgb("#A78BFA"), drawLabels: true);

            // Draw series 2 (success green) — no x-axis labels (already drawn by series1)
            DrawSeries(canvas, _series2, null, padLeft, padTop, chartW, chartH, yScale, labelY,
                Color.FromArgb("#22C55E"), Color.FromArgb("#4ADE80"), drawLabels: false);
        }

        private static void DrawSeries(
            ICanvas canvas,
            List<float>? series,
            List<string>? labels,
            float padLeft, float padTop, float chartW, float chartH, float yScale, float labelY,
            Color lineColor, Color dotColor, bool drawLabels)
        {
            if (series is null || series.Count == 0) return;

            int n = series.Count;
            float[] xs = new float[n];
            float[] ys = new float[n];

            for (int i = 0; i < n; i++)
            {
                xs[i] = n == 1
                    ? padLeft + chartW / 2f
                    : padLeft + (chartW * i / (n - 1));
                float ratio = yScale > 0 ? series[i] / yScale : 0f;
                ys[i] = padTop + chartH - (ratio * chartH);
            }

            // Area fill (subtle gradient)
            if (n >= 2)
            {
                var areaPath = new PathF();
                areaPath.MoveTo(xs[0], padTop + chartH);
                for (int i = 0; i < n; i++)
                    areaPath.LineTo(xs[i], ys[i]);
                areaPath.LineTo(xs[n - 1], padTop + chartH);
                areaPath.Close();

                canvas.SetFillPaint(new LinearGradientPaint(
                [
                    new PaintGradientStop(0f, lineColor.WithAlpha(0.18f)),
                    new PaintGradientStop(1f, lineColor.WithAlpha(0.01f))
                ],
                new Point(0, 0), new Point(0, 1)), new RectF(padLeft, padTop, chartW, chartH));
                canvas.FillPath(areaPath);
            }

            // Straight line segments
            if (n >= 2)
            {
                canvas.StrokeColor    = lineColor;
                canvas.StrokeSize     = 2.5f;
                canvas.StrokeLineCap  = LineCap.Round;
                canvas.StrokeLineJoin = LineJoin.Round;
                var linePath = new PathF();
                linePath.MoveTo(xs[0], ys[0]);
                for (int i = 1; i < n; i++)
                    linePath.LineTo(xs[i], ys[i]);
                canvas.DrawPath(linePath);
            }

            // Dots with glow ring
            for (int i = 0; i < n; i++)
            {
                canvas.FillColor = lineColor.WithAlpha(0.2f);
                canvas.FillCircle(xs[i], ys[i], 8);
                canvas.FillColor = dotColor;
                canvas.FillCircle(xs[i], ys[i], 4);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(xs[i], ys[i], 1.5f);
            }

            // X-axis labels (series1 only)
            if (drawLabels && labels is not null)
            {
                canvas.FontSize  = 10;
                canvas.FontColor = Color.FromArgb("#B3B2C5");
                for (int i = 0; i < n; i++)
                {
                    string lbl = i < labels.Count ? labels[i] : "";
                    canvas.DrawString(lbl, xs[i] - 22, labelY, 44, 20,
                        HorizontalAlignment.Center, VerticalAlignment.Top);
                }
            }
        }
    }
}
