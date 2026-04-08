using System.Text;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

public partial class CalculationsPage : ContentPage
{
	private static readonly string[] StrengthCategories =
	[
		ExerciseCatalog.Push,
		ExerciseCatalog.Pull,
		ExerciseCatalog.SquatVariation,
		ExerciseCatalog.DeadliftVariation,
		ExerciseCatalog.OlympicLifts
	];

	private readonly ApiClient _api;
	private readonly UserSession _session;
	private readonly List<SavedPrItem> _savedPrEntries = [];
	private ExerciseCatalogItem? _selectedStrengthExerciseItem;
	private ExerciseCatalogItem? _selectedPrExerciseItem;
	private int? _editingPrEntryId;
	private List<PrEntryResponse> _allPrEntries = [];
	private List<ExerciseGroup> _exerciseGroups = [];
	private ExerciseGroup? _selectedExerciseGroup;

	public CalculationsPage()
	{
		InitializeComponent();
		_api = MauiProgram.Services.GetRequiredService<ApiClient>();
		_session = MauiProgram.Services.GetRequiredService<UserSession>();
		ApplyLanguage();
		UpdateOneRmSelectionUI();
		UpdatePrSelectionUI();
		UpdateCalculationTabUI(CalcTab.OneRm);
	}

	private void ApplyLanguage()
	{
		CalcTitleLabel.Text = AppLanguage.CalcPageTitle;
		CalcSubtitleLabel.Text = AppLanguage.CalcPageSubtitle;
		NoPrsLabel.Text = AppLanguage.CalcNoPrs;
		NoPrsDescLabel.Text = AppLanguage.CalcNoPrsDesc;
		ChartTitleLabel.Text = AppLanguage.CalcPrProgress;
		ToolsSectionLabel.Text = AppLanguage.CalcStrengthTools;
		StrengthEstimateLabel.Text = AppLanguage.CalcStrengthEstimate;
		StrengthMovementLabel.Text = AppLanguage.CalcStrengthMovement;
		BrowseStrengthBtn.Text = AppLanguage.SharedBrowse;
		WeightRangeLabel.Text = AppLanguage.CalcWeightKgRange;
		RepsRangeLabel.Text = AppLanguage.CalcRepsRange;
		RirRangeLabel.Text = AppLanguage.CalcRirRange;
		ConcentricTimeLbl.Text = AppLanguage.CalcConcentricTime;
		CalculateBtn.Text = AppLanguage.CalcCalculate;
		CalcRangeLabel.Text = AppLanguage.CalcCalculatedRange;
		SavePrTitle.Text = AppLanguage.CalcSavePr;
		MovementLabel.Text = AppLanguage.CalcMovement;
		BrowsePrBtn.Text = AppLanguage.SharedBrowse;
		PrWeightLabel.Text = AppLanguage.CalcWeightKg;
		PrRepsLabel.Text = AppLanguage.CalcReps;
		PrRirLabel.Text = AppLanguage.CalcRir;
		PrConcentricTimeLbl.Text = AppLanguage.CalcConcentricTime;
		PrGctLabel.Text = AppLanguage.CalcGroundContactTime;
		SavePrButton.Text = AppLanguage.CalcSavePr;
		CancelPrEditButton.Text = AppLanguage.SharedCancel;
		SavedPrEntriesLabel.Text = AppLanguage.CalcSavedPrEntries;
		NoSavedPrLabel.Text = AppLanguage.CalcNoSavedPr;
		RsiTitleLabel.Text = AppLanguage.CalcReactiveStrength;
		RsiDescLabel.Text = AppLanguage.CalcRsiDesc;
		JumpHeightLabel.Text = AppLanguage.CalcJumpHeight;
		GctLabel.Text = AppLanguage.CalcGctS;
		CalculateRsiBtn.Text = AppLanguage.CalcCalculateRsi;
		RsiResultTitle.Text = AppLanguage.CalcResult;
		RsiResultLabel.Text = AppLanguage.CalcNoRsiYet;
		FfmiTitleLabel.Text = AppLanguage.CalcFfmiTitle;
		FfmiDescLabel.Text = AppLanguage.CalcFfmiDesc;
		FfmiMissingDataLabel.Text = AppLanguage.CalcFfmiMissingData;
		FfmiGoToProfileBtn.Text = AppLanguage.CalcFfmiGoToProfile;
		FfmiWeightLabel.Text = AppLanguage.CalcFfmiWeightLabel;
		FfmiHeightLabel.Text = AppLanguage.CalcFfmiHeightLabel;
		FfmiBodyFatLabel.Text = AppLanguage.CalcFfmiBodyFatLabel;
		CalculateFfmiBtn.Text = AppLanguage.CalcFfmiCalculate;
		FfmiResultTitle.Text = AppLanguage.CalcResult;
		FfmiNormalizedCaption.Text = AppLanguage.CalcFfmiNormalized;
		FfmiSecondaryLabel.Text = AppLanguage.CalcFfmiNoResult;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		AppLanguage.LanguageChanged -= OnLanguageChanged;
	}

	private void OnLanguageChanged() => ApplyLanguage();

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		AppLanguage.LanguageChanged += OnLanguageChanged;
		await LoadProgressDashboardAsync();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
	}

	private async Task LoadProgressDashboardAsync()
	{
		try
		{
			var prResult = await _api.GetPrEntriesAsync();
			if (prResult.Success && prResult.Data is not null && prResult.Data.Count > 0)
			{
				_allPrEntries = prResult.Data;

				// Group by ExerciseName + ExerciseCategory, order by most recent
				_exerciseGroups = _allPrEntries
					.GroupBy(p => new { p.ExerciseName, p.ExerciseCategory })
					.Select(g => new ExerciseGroup
					{
						ExerciseName = g.Key.ExerciseName,
						ExerciseCategory = g.Key.ExerciseCategory,
						Entries = g.OrderBy(e => e.CreatedAt).ToList(),
						MostRecent = g.Max(e => e.CreatedAt)
					})
					.OrderByDescending(g => g.MostRecent)
					.ToList();

				BuildExerciseChips();

				// Preselect the most recent group
				_selectedExerciseGroup = _exerciseGroups.FirstOrDefault();
				UpdateExerciseChipSelection();
				UpdateHeroAndChart();

				EmptyDashboardCard.IsVisible = false;
			}
			else
			{
				_allPrEntries = [];
				_exerciseGroups = [];
				ExerciseChipsContainer.Children.Clear();
				EmptyDashboardCard.IsVisible = true;
				HeroPrCard.IsVisible = false;
				ChartCard.IsVisible = false;
			}
		}
		catch
		{
			EmptyDashboardCard.IsVisible = true;
			HeroPrCard.IsVisible = false;
			ChartCard.IsVisible = false;
		}
	}

	private void BuildExerciseChips()
	{
		ExerciseChipsContainer.Children.Clear();

		foreach (var group in _exerciseGroups)
		{
			bool isActive = _selectedExerciseGroup is not null
				&& _selectedExerciseGroup.ExerciseName == group.ExerciseName
				&& _selectedExerciseGroup.ExerciseCategory == group.ExerciseCategory;

			var chip = CreateExerciseChip(group.ExerciseName, isActive);

			chip.GestureRecognizers.Add(new TapGestureRecognizer
			{
				Command = new Command(() =>
				{
					_selectedExerciseGroup = group;
					UpdateExerciseChipSelection();
					UpdateHeroAndChart();
				})
			});

			ExerciseChipsContainer.Children.Add(chip);
		}
	}

	private Border CreateExerciseChip(string text, bool isActive)
	{
		var accentSoft = GetDashColor("AccentSoft", "#2F2346");
		var surfaceRaised = GetDashColor("SurfaceRaised", "#1D1828");
		var accent = GetDashColor("Accent", "#8B5CF6");
		var surfaceBorder = GetDashColor("SurfaceBorder", "#342D46");

		var chip = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 14 },
			BackgroundColor = isActive ? accentSoft : surfaceRaised,
			Stroke = new SolidColorBrush(isActive ? accent : surfaceBorder),
			StrokeThickness = 1,
			Padding = new Thickness(14, 7)
		};

		chip.Content = new Label
		{
			Text = text,
			FontSize = 12,
			FontFamily = "OpenSansSemibold",
			TextColor = isActive
				? GetDashColor("AccentGlow", "#A78BFA")
				: GetDashColor("TextSecondary", "#B3B2C5")
		};

		return chip;
	}

	private void UpdateExerciseChipSelection()
	{
		int index = 0;
		foreach (var group in _exerciseGroups)
		{
			if (index >= ExerciseChipsContainer.Children.Count) break;
			bool isActive = _selectedExerciseGroup is not null
				&& _selectedExerciseGroup.ExerciseName == group.ExerciseName
				&& _selectedExerciseGroup.ExerciseCategory == group.ExerciseCategory;

			var chip = (Border)ExerciseChipsContainer.Children[index];
			var accentSoft = GetDashColor("AccentSoft", "#2F2346");
			var surfaceRaised = GetDashColor("SurfaceRaised", "#1D1828");
			var accent = GetDashColor("Accent", "#8B5CF6");
			var surfaceBorder = GetDashColor("SurfaceBorder", "#342D46");

			chip.BackgroundColor = isActive ? accentSoft : surfaceRaised;
			chip.Stroke = new SolidColorBrush(isActive ? accent : surfaceBorder);

			if (chip.Content is Label label)
			{
				label.TextColor = isActive
					? GetDashColor("AccentGlow", "#A78BFA")
					: GetDashColor("TextSecondary", "#B3B2C5");
			}

			index++;
		}
	}

	private void UpdateHeroAndChart()
	{
		if (_selectedExerciseGroup is null || _selectedExerciseGroup.Entries.Count == 0)
		{
			HeroPrCard.IsVisible = false;
			ChartCard.IsVisible = false;
			return;
		}

		var entries = _selectedExerciseGroup.Entries;
		var best = entries.First();
		bool isStrength = best.TrackingMode == "Strength";

		double SafeEstimated1Rm(PrEntryResponse e)
		{
			if (e.Weight <= 0 || e.Reps <= 0)
				return e.Weight;
			return CalculationService.CalculateOneRm(e.Weight, e.Reps, e.RIR ?? 0);
		}

		// Find best value
		if (isStrength)
		{
			best = entries.OrderByDescending(e => SafeEstimated1Rm(e)).First();
			HeroPrExerciseLabel.Text = _selectedExerciseGroup.ExerciseName.ToUpperInvariant();
			HeroPrStrengthLabel.Text = AppLanguage.CalcEstimated1Rm;
			var estimated1Rm = SafeEstimated1Rm(best);
			HeroPrValueLabel.Text = $"{estimated1Rm:0.#} kg";
		}
		else
		{
			best = entries.OrderByDescending(e => e.Metric1Value ?? 0).First();
			HeroPrExerciseLabel.Text = _selectedExerciseGroup.ExerciseName.ToUpperInvariant();

			// Resolve real metric label from exercise catalog
			var catalogItem = ExerciseCatalog.GetByNameAndCategory(
				_selectedExerciseGroup.ExerciseName, _selectedExerciseGroup.ExerciseCategory);
			string metricLabel = catalogItem is not null
				? $"{catalogItem.PrimaryLabel} ({catalogItem.PrimaryUnit})".ToUpperInvariant()
				: !string.IsNullOrWhiteSpace(best.Metric1Unit)
					? $"BEST ({best.Metric1Unit.ToUpperInvariant()})"
					: AppLanguage.CalcBestValue;
			HeroPrStrengthLabel.Text = metricLabel;
			HeroPrValueLabel.Text = $"{best.Metric1Value:0.##} {best.Metric1Unit}";
		}

		HeroPrDateLabel.Text = AppLanguage.FormatBestPr(best.CreatedAt);
		HeroPrCard.IsVisible = true;

		// Chart: up to last 6 points, require at least 2 for a line
		var chartPoints = entries.TakeLast(6).ToList();
		var points = chartPoints.Select(e => new ChartPoint
		{
			Date = e.CreatedAt,
			Value = isStrength ? SafeEstimated1Rm(e) : (e.Metric1Value ?? 0)
		}).ToList();

		if (points.Count >= 2)
		{
			var span = points.Last().Date - points.First().Date;
			bool useMonthLabels = span.TotalDays > 45;

			PrChartView.Drawable = new PrLineChartDrawable(points, useMonthLabels);
			PrChartView.Invalidate();
			ChartTitleLabel.Text = AppLanguage.FormatProgress(_selectedExerciseGroup.ExerciseName);
			ChartCard.IsVisible = true;
		}
		else
		{
			ChartCard.IsVisible = false;
		}
	}

	private static Color GetDashColor(string key, string fallback)
	{
		if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
			return color;
		return Color.FromArgb(fallback);
	}

	private sealed class ExerciseGroup
	{
		public string ExerciseName { get; set; } = "";
		public string ExerciseCategory { get; set; } = "";
		public List<PrEntryResponse> Entries { get; set; } = [];
		public DateTime MostRecent { get; set; }
	}

	private sealed class ChartPoint
	{
		public DateTime Date { get; set; }
		public double Value { get; set; }
	}

	private sealed class PrLineChartDrawable : IDrawable
	{
		private readonly List<ChartPoint> _points;
		private readonly bool _useMonthLabels;

		public PrLineChartDrawable(List<ChartPoint> points, bool useMonthLabels)
		{
			_points = points;
			_useMonthLabels = useMonthLabels;
		}

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			if (_points.Count < 2) return;

			float w = dirtyRect.Width;
			float h = dirtyRect.Height;
			float padLeft = 44;
			float padRight = 14;
			float padTop = 14;
			float padBottom = 32;
			float chartW = w - padLeft - padRight;
			float chartH = h - padTop - padBottom;

			double minVal = _points.Min(p => p.Value);
			double maxVal = _points.Max(p => p.Value);
			double margin = (maxVal - minVal) * 0.1;
			if (margin < 1) margin = 1;
			minVal -= margin;
			maxVal += margin;

			var pts = new PointF[_points.Count];
			for (int i = 0; i < _points.Count; i++)
			{
				float x = padLeft + (chartW * i / (_points.Count - 1));
				float y = padTop + chartH - (float)((((_points[i].Value - minVal) / (maxVal - minVal))) * chartH);
				pts[i] = new PointF(x, y);
			}

			// Draw horizontal grid lines + Y-axis labels
			canvas.FontSize = 9;
			canvas.FontColor = Color.FromArgb("#5A5474");
			canvas.StrokeColor = Color.FromArgb("#2A2540");
			canvas.StrokeSize = 1;
			for (int g = 0; g <= 3; g++)
			{
				float ratio = g / 3f;
				float gy = padTop + chartH - (ratio * chartH);
				double val = minVal + ratio * (maxVal - minVal);
				canvas.DrawLine(padLeft, gy, padLeft + chartW, gy);
				canvas.DrawString($"{val:0.#}", 0, gy - 8, padLeft - 4, 16, HorizontalAlignment.Right, VerticalAlignment.Center);
			}

			// Draw area fill
			var areaPath = new PathF();
			areaPath.MoveTo(pts[0].X, padTop + chartH);
			foreach (var pt in pts)
				areaPath.LineTo(pt.X, pt.Y);
			areaPath.LineTo(pts[^1].X, padTop + chartH);
			areaPath.Close();

			canvas.SetFillPaint(new LinearGradientPaint(
				[
					new PaintGradientStop(0f, Color.FromArgb("#8B5CF6").WithAlpha(0.25f)),
					new PaintGradientStop(1f, Color.FromArgb("#8B5CF6").WithAlpha(0.02f))
				],
				new Point(0, 0), new Point(0, 1)), dirtyRect);
			canvas.FillPath(areaPath);

			// Draw line
			canvas.StrokeColor = Color.FromArgb("#A78BFA");
			canvas.StrokeSize = 2.5f;
			canvas.StrokeLineCap = LineCap.Round;
			canvas.StrokeLineJoin = LineJoin.Round;

			var linePath = new PathF();
			linePath.MoveTo(pts[0].X, pts[0].Y);
			for (int i = 1; i < pts.Length; i++)
				linePath.LineTo(pts[i].X, pts[i].Y);
			canvas.DrawPath(linePath);

			// Draw dots with outer glow ring
			foreach (var pt in pts)
			{
				canvas.FillColor = Color.FromArgb("#8B5CF6").WithAlpha(0.2f);
				canvas.FillCircle(pt.X, pt.Y, 8);
				canvas.FillColor = Color.FromArgb("#A78BFA");
				canvas.FillCircle(pt.X, pt.Y, 4);
				canvas.FillColor = Colors.White;
				canvas.FillCircle(pt.X, pt.Y, 1.5f);
			}

			// Draw X-axis labels
			canvas.FontSize = 10;
			canvas.FontColor = Color.FromArgb("#B3B2C5");
			for (int i = 0; i < _points.Count; i++)
			{
				string label = _useMonthLabels
					? _points[i].Date.ToString("MMM")
					: _points[i].Date.ToString("MMM d");
				canvas.DrawString(label, pts[i].X - 22, padTop + chartH + 8, 44, 20, HorizontalAlignment.Center, VerticalAlignment.Top);
			}
		}
	}

	private enum CalcTab { OneRm, Rsi, Ffmi }

	private void OnOneRmTabClicked(object? sender, EventArgs e)
	{
		UpdateCalculationTabUI(CalcTab.OneRm);
	}

	private void OnRsiTabClicked(object? sender, EventArgs e)
	{
		UpdateCalculationTabUI(CalcTab.Rsi);
	}

	private void OnFfmiTabClicked(object? sender, EventArgs e)
	{
		UpdateCalculationTabUI(CalcTab.Ffmi);
		_ = LoadFfmiProfileDataAsync();
	}

	private void UpdateCalculationTabUI(CalcTab activeTab)
	{
		OneRmSection.IsVisible = activeTab == CalcTab.OneRm;
		RsiSection.IsVisible = activeTab == CalcTab.Rsi;
		FfmiSection.IsVisible = activeTab == CalcTab.Ffmi;

		var active = Color.FromArgb("#7C4DFF");
		var inactive = Color.FromArgb("#161322");
		var activeText = Colors.White;
		var inactiveText = Color.FromArgb("#C9C3DA");

		OneRmTabButton.BackgroundColor = activeTab == CalcTab.OneRm ? active : inactive;
		OneRmTabButton.TextColor = activeTab == CalcTab.OneRm ? activeText : inactiveText;
		RsiTabButton.BackgroundColor = activeTab == CalcTab.Rsi ? active : inactive;
		RsiTabButton.TextColor = activeTab == CalcTab.Rsi ? activeText : inactiveText;
		FfmiTabButton.BackgroundColor = activeTab == CalcTab.Ffmi ? active : inactive;
		FfmiTabButton.TextColor = activeTab == CalcTab.Ffmi ? activeText : inactiveText;
	}

	private async void OnChooseStrengthExerciseClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				AppLanguage.CalcChooseStrength,
				StrengthCategories,
				OnStrengthExerciseSelected),
			true);
	}

	private void OnStrengthExerciseSelected(ExerciseCatalogItem item)
	{
		_selectedStrengthExerciseItem = item;
		UpdateOneRmSelectionUI();
	}

	private void UpdateOneRmSelectionUI()
	{
		if (_selectedStrengthExerciseItem is null)
		{
			SelectedStrengthExerciseLabel.Text = AppLanguage.CalcNoStrengthSelected;
			SelectedStrengthExerciseHintLabel.Text = AppLanguage.CalcStrengthHint;
			return;
		}

		SelectedStrengthExerciseLabel.Text = _selectedStrengthExerciseItem.Name;
		SelectedStrengthExerciseHintLabel.Text = _selectedStrengthExerciseItem.SelectionHintText;
	}

	private void OnCalculateOneRmClicked(object? sender, EventArgs e)
	{
		ResultsLabel.Text = string.Empty;
		ClearLabel(OneRmStatusLabel);

		OneRmInputParseResult inputResult = CalculationsPageLogic.ParseOneRmInputs(
			WeightEntry.Text,
			RepsEntry.Text,
			RirEntry.Text,
			ConcentricTimeEntry.Text);

		if (!inputResult.IsValid)
		{
			ShowError(OneRmStatusLabel, inputResult.ErrorMessage);
			return;
		}

		double oneRm = CalculationService.CalculateOneRm(inputResult.WeightKg, inputResult.Reps, inputResult.Rir);
		var output = new StringBuilder();

		if (_selectedStrengthExerciseItem is not null)
		{
			output.AppendLine($"Movement: {_selectedStrengthExerciseItem.Name}");
		}

		IReadOnlyList<double> rmValues = CalculationService.BuildRmTable(oneRm, 8);
		for (int rm = 1; rm <= rmValues.Count; rm++)
		{
			double rmWeight = rmValues[rm - 1];
			output.AppendLine($"{rm}RM: {Math.Round(rmWeight, 1)} kg");
		}

		if (inputResult.ConcentricTimeSeconds.HasValue)
		{
			output.AppendLine();
			output.AppendLine($"Concentric Time: {inputResult.ConcentricTimeSeconds.Value:0.##} s");
		}

		ResultsLabel.Text = output.ToString().TrimEnd();
	}

	private async void OnChoosePrExerciseClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				AppLanguage.CalcChoosePr,
				ExerciseCatalog.Categories,
				OnPrExerciseSelected),
			true);
	}

	private void OnPrExerciseSelected(ExerciseCatalogItem item)
	{
		_selectedPrExerciseItem = item;
		UpdatePrSelectionUI();
	}

	private void UpdatePrSelectionUI()
	{
		if (_selectedPrExerciseItem is null)
		{
			SelectedPrExerciseLabel.Text = AppLanguage.CalcNoPrSelected;
			SelectedPrExerciseHintLabel.Text = AppLanguage.CalcPrHint;
			PrStrengthInputsSection.IsVisible = false;
			PrCustomInputsSection.IsVisible = false;
			PrMetric2Container.IsVisible = false;
			PrGctContainer.IsVisible = false;
			return;
		}

		SelectedPrExerciseLabel.Text = _selectedPrExerciseItem.Name;
		SelectedPrExerciseHintLabel.Text = _selectedPrExerciseItem.SelectionHintText;

		bool isStrength = _selectedPrExerciseItem.TrackingMode == ExerciseTrackingMode.Strength;
		PrStrengthInputsSection.IsVisible = isStrength;
		PrCustomInputsSection.IsVisible = !isStrength;

		if (!isStrength)
		{
			PrMetric1Label.Text = $"{_selectedPrExerciseItem.PrimaryLabel} ({_selectedPrExerciseItem.PrimaryUnit})";
			PrMetric1Entry.Placeholder = $"Enter {_selectedPrExerciseItem.PrimaryLabel.ToLowerInvariant()}";
			PrMetric2Container.IsVisible = _selectedPrExerciseItem.HasSecondaryMetric;
			PrMetric2Label.Text = $"{_selectedPrExerciseItem.SecondaryLabel} ({_selectedPrExerciseItem.SecondaryUnit})";
			PrMetric2Entry.Placeholder = $"Enter {_selectedPrExerciseItem.SecondaryLabel.ToLowerInvariant()}";
			PrGctContainer.IsVisible = _selectedPrExerciseItem.SupportsGroundContactTime;
		}
		else
		{
			PrMetric2Container.IsVisible = false;
			PrGctContainer.IsVisible = false;
		}
	}

	private async void OnSavePrClicked(object? sender, EventArgs e)
	{
		ClearLabel(PrStatusLabel);

		if (!_session.IsLoggedIn())
		{
			ShowError(PrStatusLabel, AppLanguage.SharedPleaseLogin);
			return;
		}

		if (_selectedPrExerciseItem is null)
		{
			ShowError(PrStatusLabel, AppLanguage.SharedChooseMovement);
			return;
		}

		// Use existing validation logic (userId=0 is fine, API uses JWT token)
		PrEntryBuildResult buildResult = BuildPrEntry(0);
		if (!buildResult.IsValid || buildResult.Entry is null)
		{
			ShowError(PrStatusLabel, buildResult.ErrorMessage);
			return;
		}

		var entry = buildResult.Entry;
		var data = new
		{
			exerciseName = entry.ExerciseName,
			exerciseCategory = entry.ExerciseCategory,
			trackingMode = entry.TrackingMode,
			weight = entry.Weight,
			reps = entry.Reps,
			rir = entry.RIR,
			metric1Value = entry.Metric1Value,
			metric1Unit = entry.Metric1Unit,
			metric2Value = entry.Metric2Value,
			metric2Unit = entry.Metric2Unit,
			groundContactTimeMs = entry.GroundContactTimeMs,
			concentricTimeSeconds = entry.ConcentricTimeSeconds
		};

		if (_editingPrEntryId.HasValue)
		{
			var result = await _api.UpdatePrEntryAsync(_editingPrEntryId.Value, data);
			if (!result.Success)
			{
				ShowError(PrStatusLabel, result.Error ?? AppLanguage.CalcPrFailedUpdate);
				return;
			}
			ShowSuccess(PrStatusLabel, AppLanguage.CalcPrUpdated);
		}
		else
		{
			var result = await _api.CreatePrEntryAsync(data);
			if (!result.Success)
			{
				ShowError(PrStatusLabel, result.Error ?? AppLanguage.CalcPrFailedSave);
				return;
			}
			ShowSuccess(PrStatusLabel, AppLanguage.CalcPrSaved);
		}

		ResetPrSaveMode();
		await LoadProgressDashboardAsync();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
	}

	private PrEntryBuildResult BuildPrEntry(int userId)
	{
		if (_selectedPrExerciseItem is null)
		{
			return PrEntryBuildResult.Failure(AppLanguage.SharedChooseMovement);
		}

		return CalculationsPageLogic.BuildPrEntry(new PrEntryBuildRequest
		{
			UserId = userId,
			ExerciseName = _selectedPrExerciseItem.Name,
			ExerciseCategory = _selectedPrExerciseItem.Category,
			TrackingMode = _selectedPrExerciseItem.TrackingMode,
			PrimaryLabel = _selectedPrExerciseItem.PrimaryLabel,
			PrimaryUnit = _selectedPrExerciseItem.PrimaryUnit,
			SecondaryLabel = _selectedPrExerciseItem.SecondaryLabel,
			SecondaryUnit = _selectedPrExerciseItem.SecondaryUnit,
			HasSecondaryMetric = _selectedPrExerciseItem.HasSecondaryMetric,
			SupportsGroundContactTime = _selectedPrExerciseItem.SupportsGroundContactTime,
			WeightText = PrWeightEntry.Text,
			RepsText = PrRepsEntry.Text,
			RirText = PrRirEntry.Text,
			ConcentricTimeText = PrConcentricTimeEntry.Text,
			Metric1Text = PrMetric1Entry.Text,
			Metric2Text = PrMetric2Entry.Text,
			GroundContactTimeText = PrGroundContactTimeEntry.Text
		});
	}

	private async Task LoadSavedPrEntriesAsync()
	{
		_savedPrEntries.Clear();

		if (!_session.IsLoggedIn())
		{
			return;
		}

		var result = await _api.GetPrEntriesAsync();
		if (!result.Success || result.Data is null)
		{
			return;
		}

		_savedPrEntries.AddRange(result.Data.Select(entry => new SavedPrItem
		{
			Id = entry.Id,
			ExerciseName = entry.ExerciseName,
			ExerciseCategory = entry.ExerciseCategory,
			TrackingMode = entry.TrackingMode,
			Weight = entry.Weight,
			Reps = entry.Reps,
			Rir = entry.RIR,
			Metric1Value = entry.Metric1Value,
			Metric1Unit = entry.Metric1Unit,
			Metric2Value = entry.Metric2Value,
			Metric2Unit = entry.Metric2Unit,
			GroundContactTimeMs = entry.GroundContactTimeMs,
			ConcentricTimeSeconds = entry.ConcentricTimeSeconds,
			Text = FormatSavedPr(entry)
		}));
	}

	private void RefreshSavedPrList()
	{
		SavedPrView.ItemsSource = _savedPrEntries.ToList();
	}

	private static string FormatSavedPr(PrEntryResponse entry)
	{
		if (entry.TrackingMode == nameof(ExerciseTrackingMode.Custom))
		{
			ExerciseCatalogItem? item = ExerciseCatalog.GetByNameAndCategory(entry.ExerciseName, entry.ExerciseCategory);
			string primaryLabel = item?.PrimaryLabel ?? "Value";
			string text = $"{entry.ExerciseName}: {primaryLabel} {entry.Metric1Value:0.##} {entry.Metric1Unit}";

			if (item?.HasSecondaryMetric == true && entry.Metric2Value.HasValue)
			{
				text += $" | {item.SecondaryLabel} {entry.Metric2Value:0.##} {entry.Metric2Unit}";
			}

			if (entry.GroundContactTimeMs.HasValue)
			{
				text += $" | GCT {MetricInput.FormatSecondsFromMilliseconds(entry.GroundContactTimeMs.Value)}";
			}

			return text;
		}

		string strengthText = entry.RIR.HasValue
			? $"{entry.ExerciseName}: {entry.Weight} x {entry.Reps} RIR{entry.RIR.Value}"
			: $"{entry.ExerciseName}: {entry.Weight} x {entry.Reps}";

		if (entry.ConcentricTimeSeconds.HasValue)
		{
			strengthText += $" | Concentric {entry.ConcentricTimeSeconds.Value:0.##} s";
		}

		return strengthText;
	}

	private async void OnDeleteSavedPrInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<SavedPrItem>(sender) is not SavedPrItem item)
		{
			return;
		}

		bool confirmed = await ConfirmDialogPage.ShowAsync(
			Navigation,
			AppLanguage.CalcDeletePrTitle,
			AppLanguage.FormatDeleteConfirm(item.Text),
			AppLanguage.SharedDelete,
			AppLanguage.SharedCancel);
		if (!confirmed)
		{
			return;
		}

		await _api.DeletePrEntryAsync(item.Id);
		if (_editingPrEntryId == item.Id)
		{
			ResetPrSaveMode();
		}

		await LoadProgressDashboardAsync();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
		ShowSuccess(PrStatusLabel, AppLanguage.CalcPrDeleted);
	}

	private void OnEditSavedPrInvoked(object? sender, EventArgs e)
	{
		if (GetBindingContext<SavedPrItem>(sender) is not SavedPrItem item)
		{
			return;
		}

		_editingPrEntryId = item.Id;
		_selectedPrExerciseItem = ExerciseCatalog.GetByNameAndCategory(item.ExerciseName, item.ExerciseCategory);
		UpdatePrSelectionUI();

		if (item.TrackingMode == nameof(ExerciseTrackingMode.Custom))
		{
			PrMetric1Entry.Text = item.Metric1Value?.ToString("0.##") ?? string.Empty;
			PrMetric2Entry.Text = item.Metric2Value?.ToString("0.##") ?? string.Empty;
			PrGroundContactTimeEntry.Text = item.GroundContactTimeMs.HasValue
				? MetricInput.MillisecondsToSeconds(item.GroundContactTimeMs.Value).ToString("0.##")
				: string.Empty;
		}
		else
		{
			PrWeightEntry.Text = item.Weight.ToString();
			PrRepsEntry.Text = item.Reps.ToString();
			PrRirEntry.Text = item.Rir?.ToString() ?? string.Empty;
			PrConcentricTimeEntry.Text = item.ConcentricTimeSeconds?.ToString("0.##") ?? string.Empty;
		}

		SavePrButton.Text = AppLanguage.CalcUpdatePr;
		CancelPrEditButton.IsVisible = true;
		ShowSuccess(PrStatusLabel, AppLanguage.FormatEditing(item.Text));
	}

	private void OnCancelPrEditClicked(object? sender, EventArgs e)
	{
		ResetPrSaveMode();
		ClearLabel(PrStatusLabel);
	}

	private void ResetPrSaveMode()
	{
		_editingPrEntryId = null;
		_selectedPrExerciseItem = null;
		UpdatePrSelectionUI();
		PrWeightEntry.Text = string.Empty;
		PrRepsEntry.Text = string.Empty;
		PrRirEntry.Text = string.Empty;
		PrConcentricTimeEntry.Text = string.Empty;
		PrMetric1Entry.Text = string.Empty;
		PrMetric2Entry.Text = string.Empty;
		PrGroundContactTimeEntry.Text = string.Empty;
		SavePrButton.Text = AppLanguage.CalcSavePr;
		CancelPrEditButton.IsVisible = false;
	}

	private void OnCalculateRsiClicked(object? sender, EventArgs e)
	{
		ClearLabel(RsiStatusLabel);

		if (!MetricInput.TryParseFlexibleDouble(RsiJumpHeightEntry.Text, out double jumpHeightCm) || jumpHeightCm <= 0)
		{
			ShowError(RsiStatusLabel, AppLanguage.CalcJumpHeightError);
			return;
		}

		if (!MetricInput.TryParseFlexibleDouble(RsiGroundContactTimeEntry.Text, out double gctSeconds) || gctSeconds <= 0)
		{
			ShowError(RsiStatusLabel, AppLanguage.CalcGctError);
			return;
		}

		double rsi = CalculationService.CalculateRsi(jumpHeightCm, gctSeconds);
		RsiResultLabel.Text = $"RSI: {rsi:0.00} | Height {jumpHeightCm:0.##} cm | GCT {gctSeconds:0.##} s";
	}

	private async Task LoadFfmiProfileDataAsync()
	{
		if (!_session.IsLoggedIn()) return;

		var result = await _api.GetProfileAsync();
		if (!result.Success || result.Data is null) return;

		var profile = result.Data;
		bool hasFfmiData = profile.WeightKg.HasValue
						&& profile.HeightCm.HasValue
						&& profile.BodyFatPercentage.HasValue;

		FfmiMissingDataCard.IsVisible = !hasFfmiData;
		FfmiInputCard.IsVisible = hasFfmiData;
		FfmiResultCard.IsVisible = hasFfmiData;

		if (hasFfmiData)
		{
			FfmiWeightEntry.Text = profile.WeightKg!.Value.ToString("0.#");
			FfmiHeightEntry.Text = profile.HeightCm!.Value.ToString("0.#");
			FfmiBodyFatEntry.Text = profile.BodyFatPercentage!.Value.ToString("0.#");
		}
	}

	private async void OnFfmiGoToProfileClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new ProfilePage(), true);
	}

	private void OnCalculateFfmiClicked(object? sender, EventArgs e)
	{
		ClearLabel(FfmiStatusLabel);

		if (!MetricInput.TryParseFlexibleDouble(FfmiWeightEntry.Text, out double weightKg) || weightKg <= 0)
		{
			ShowError(FfmiStatusLabel, AppLanguage.CalcFfmiWeightError);
			return;
		}

		if (!MetricInput.TryParseFlexibleDouble(FfmiHeightEntry.Text, out double heightCm) || heightCm <= 0)
		{
			ShowError(FfmiStatusLabel, AppLanguage.CalcFfmiHeightError);
			return;
		}

		if (!MetricInput.TryParseFlexibleDouble(FfmiBodyFatEntry.Text, out double bodyFat) || bodyFat <= 0 || bodyFat >= 100)
		{
			ShowError(FfmiStatusLabel, AppLanguage.CalcFfmiBodyFatError);
			return;
		}

		var (lbm, rawFfmi, normalizedFfmi) = CalculationService.CalculateFfmi(weightKg, heightCm, bodyFat);

		FfmiNormalizedLabel.Text = normalizedFfmi.ToString("0.0");
		FfmiNormalizedCaption.Text = AppLanguage.CalcFfmiNormalized;
		FfmiSecondaryLabel.Text = $"{AppLanguage.CalcFfmiRaw}: {rawFfmi:0.0}  |  {AppLanguage.CalcFfmiLbm}: {lbm:0.1} kg";
	}

	private static void ShowError(Label label, string message)
	{
		label.Text = message;
		label.TextColor = Colors.Red;
		label.IsVisible = true;
	}

	private static void ShowSuccess(Label label, string message)
	{
		label.Text = message;
		label.TextColor = Colors.LightGreen;
		label.IsVisible = true;
	}

	private static void ClearLabel(Label label)
	{
		label.Text = string.Empty;
		label.IsVisible = false;
	}

	private static TItem? GetBindingContext<TItem>(object? sender) where TItem : class
	{
		return sender switch
		{
			BindableObject bindable when bindable.BindingContext is TItem item => item,
			_ => null
		};
	}

	private sealed class SavedPrItem
	{
		public int Id { get; set; }
		public string ExerciseName { get; set; } = string.Empty;
		public string ExerciseCategory { get; set; } = string.Empty;
		public string TrackingMode { get; set; } = nameof(ExerciseTrackingMode.Strength);
		public int Weight { get; set; }
		public int Reps { get; set; }
		public int? Rir { get; set; }
		public double? Metric1Value { get; set; }
		public string Metric1Unit { get; set; } = string.Empty;
		public double? Metric2Value { get; set; }
		public string Metric2Unit { get; set; } = string.Empty;
		public double? GroundContactTimeMs { get; set; }
		public double? ConcentricTimeSeconds { get; set; }
		public string Text { get; set; } = string.Empty;
	}
}
