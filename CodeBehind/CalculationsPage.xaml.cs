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
		UpdateOneRmSelectionUI();
		UpdatePrSelectionUI();
		UpdateCalculationTabUI(showOneRm: true);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
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

		// Find best value
		if (isStrength)
		{
			best = entries.OrderByDescending(e => e.Weight).First();
			HeroPrExerciseLabel.Text = _selectedExerciseGroup.ExerciseName.ToUpperInvariant();
			HeroPrStrengthLabel.Text = "MAXIMUM LOAD (KG)";
			HeroPrValueLabel.Text = $"{best.Weight} kg";
		}
		else
		{
			best = entries.OrderByDescending(e => e.Metric1Value ?? 0).First();
			HeroPrExerciseLabel.Text = _selectedExerciseGroup.ExerciseName.ToUpperInvariant();
			HeroPrStrengthLabel.Text = $"{best.Metric1Unit?.ToUpperInvariant() ?? "VALUE"}";
			HeroPrValueLabel.Text = $"{best.Metric1Value:0.##} {best.Metric1Unit}";
		}

		HeroPrDateLabel.Text = $"Best PR: {best.CreatedAt:MMM dd, yyyy}";
		HeroPrCard.IsVisible = true;

		// Chart: up to last 6 points
		var chartPoints = entries.TakeLast(6).ToList();
		if (chartPoints.Count >= 2)
		{
			var points = chartPoints.Select(e => new ChartPoint
			{
				Date = e.CreatedAt,
				Value = isStrength ? e.Weight : (e.Metric1Value ?? 0)
			}).ToList();

			var span = points.Last().Date - points.First().Date;
			bool useMonthLabels = span.TotalDays > 45;

			var drawable = new PrLineChartDrawable(points, useMonthLabels);
			PrChartView.Drawable = drawable;
			PrChartView.Invalidate();
			ChartTitleLabel.Text = $"{_selectedExerciseGroup.ExerciseName} Progress";
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
			float padLeft = 10;
			float padRight = 10;
			float padTop = 10;
			float padBottom = 30;
			float chartW = w - padLeft - padRight;
			float chartH = h - padTop - padBottom;

			double minVal = _points.Min(p => p.Value);
			double maxVal = _points.Max(p => p.Value);
			if (Math.Abs(maxVal - minVal) < 0.01) { minVal -= 1; maxVal += 1; }

			var pts = new PointF[_points.Count];
			for (int i = 0; i < _points.Count; i++)
			{
				float x = padLeft + (chartW * i / (_points.Count - 1));
				float y = padTop + chartH - (float)(((_points[i].Value - minVal) / (maxVal - minVal)) * chartH);
				pts[i] = new PointF(x, y);
			}

			// Draw area fill
			var areaPath = new PathF();
			areaPath.MoveTo(pts[0].X, padTop + chartH);
			foreach (var pt in pts)
				areaPath.LineTo(pt.X, pt.Y);
			areaPath.LineTo(pts[^1].X, padTop + chartH);
			areaPath.Close();

			canvas.SetFillPaint(new LinearGradientPaint(
				new[] {
					new PaintGradientStop(0f, Color.FromArgb("#8B5CF6").WithAlpha(0.3f)),
					new PaintGradientStop(1f, Color.FromArgb("#8B5CF6").WithAlpha(0.02f))
				},
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

			// Draw dots
			canvas.FillColor = Color.FromArgb("#A78BFA");
			foreach (var pt in pts)
				canvas.FillCircle(pt.X, pt.Y, 4);

			// Draw labels
			canvas.FontSize = 10;
			canvas.FontColor = Color.FromArgb("#B3B2C5");
			for (int i = 0; i < _points.Count; i++)
			{
				string label = _useMonthLabels
					? _points[i].Date.ToString("MMM")
					: _points[i].Date.ToString("MMM d");
				canvas.DrawString(label, pts[i].X - 20, padTop + chartH + 6, 40, 20, HorizontalAlignment.Center, VerticalAlignment.Top);
			}
		}
	}

	private void OnOneRmTabClicked(object? sender, EventArgs e)
	{
		UpdateCalculationTabUI(showOneRm: true);
	}

	private void OnRsiTabClicked(object? sender, EventArgs e)
	{
		UpdateCalculationTabUI(showOneRm: false);
	}

	private void UpdateCalculationTabUI(bool showOneRm)
	{
		OneRmSection.IsVisible = showOneRm;
		RsiSection.IsVisible = !showOneRm;

		OneRmTabButton.BackgroundColor = showOneRm ? Color.FromArgb("#7C4DFF") : Color.FromArgb("#161322");
		OneRmTabButton.TextColor = showOneRm ? Colors.White : Color.FromArgb("#C9C3DA");
		RsiTabButton.BackgroundColor = showOneRm ? Color.FromArgb("#161322") : Color.FromArgb("#7C4DFF");
		RsiTabButton.TextColor = showOneRm ? Color.FromArgb("#C9C3DA") : Colors.White;
	}

	private async void OnChooseStrengthExerciseClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(
			new ExercisePickerPage(
				"Choose Strength Exercise",
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
			SelectedStrengthExerciseLabel.Text = "No strength movement selected";
			SelectedStrengthExerciseHintLabel.Text = "Browse weighted movements for the 1RM estimate.";
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
				"Choose PR Movement",
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
			SelectedPrExerciseLabel.Text = "No PR movement selected";
			SelectedPrExerciseHintLabel.Text = "Browse gym and athletic movements before saving a PR.";
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
			ShowError(PrStatusLabel, "Please log in again.");
			return;
		}

		if (_selectedPrExerciseItem is null)
		{
			ShowError(PrStatusLabel, "Choose a movement before saving.");
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
				ShowError(PrStatusLabel, result.Error ?? "Failed to update PR.");
				return;
			}
			ShowSuccess(PrStatusLabel, "Saved PR updated.");
		}
		else
		{
			var result = await _api.CreatePrEntryAsync(data);
			if (!result.Success)
			{
				ShowError(PrStatusLabel, result.Error ?? "Failed to save PR.");
				return;
			}
			ShowSuccess(PrStatusLabel, "Saved PR added.");
		}

		ResetPrSaveMode();
		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
	}

	private PrEntryBuildResult BuildPrEntry(int userId)
	{
		if (_selectedPrExerciseItem is null)
		{
			return PrEntryBuildResult.Failure("Choose a movement before saving.");
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
			"Delete PR",
			$"Delete '{item.Text}'?",
			"Delete",
			"Cancel");
		if (!confirmed)
		{
			return;
		}

		await _api.DeletePrEntryAsync(item.Id);
		if (_editingPrEntryId == item.Id)
		{
			ResetPrSaveMode();
		}

		await LoadSavedPrEntriesAsync();
		RefreshSavedPrList();
		ShowSuccess(PrStatusLabel, "Saved PR deleted.");
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

		SavePrButton.Text = "Update PR";
		CancelPrEditButton.IsVisible = true;
		ShowSuccess(PrStatusLabel, $"Editing: {item.Text}");
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
		SavePrButton.Text = "Save PR";
		CancelPrEditButton.IsVisible = false;
	}

	private void OnCalculateRsiClicked(object? sender, EventArgs e)
	{
		ClearLabel(RsiStatusLabel);

		if (!MetricInput.TryParseFlexibleDouble(RsiJumpHeightEntry.Text, out double jumpHeightCm) || jumpHeightCm <= 0)
		{
			ShowError(RsiStatusLabel, "Jump height must be a positive number.");
			return;
		}

		if (!MetricInput.TryParseFlexibleDouble(RsiGroundContactTimeEntry.Text, out double gctSeconds) || gctSeconds <= 0)
		{
			ShowError(RsiStatusLabel, "GCT must be a positive number.");
			return;
		}

		double rsi = CalculationService.CalculateRsi(jumpHeightCm, gctSeconds);
		RsiResultLabel.Text = $"RSI: {rsi:0.00} | Height {jumpHeightCm:0.##} cm | GCT {gctSeconds:0.##} s";
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
