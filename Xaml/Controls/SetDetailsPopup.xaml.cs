using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete.Xaml.Controls;

public partial class SetDetailsPopup : ContentView
{
    private readonly Page _page;
    private readonly List<Entry> _weightEntries = new();
    private readonly List<Entry> _repsEntries = new();
    private readonly TaskCompletionSource<List<SetDetail>?> _result = new();
    private bool _isClosing;

    private SetDetailsPopup(Page page, int setCount, int? defaultReps)
    {
        _page = page;
        InitializeComponent();
        ApplyLanguage();
        BuildRows(setCount, defaultReps);
    }

    public static async Task<List<SetDetail>?> ShowAsync(Page page, int setCount, int? defaultReps)
    {
        var popup = new SetDetailsPopup(page, setCount, defaultReps);
        _ = page.ShowPopupAsync(popup, new PopupOptions
        {
            CanBeDismissedByTappingOutsideOfPopup = false,
            PageOverlayColor = Color.FromArgb("#AA000000"),
            Shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(24),
                Fill = new SolidColorBrush(Colors.Transparent),
                Stroke = new SolidColorBrush(Colors.Transparent),
                StrokeThickness = 0
            }
        });
        return await popup._result.Task;
    }

    private void ApplyLanguage()
    {
        TitleLabel.Text = AppLanguage.NewWorkoutSetDetailsTitle;
        SubtitleLabel.Text = AppLanguage.NewWorkoutSetDetailsSubtitle;
        SetHeaderLabel.Text = AppLanguage.NewWorkoutSetColumnSet;
        WeightHeaderLabel.Text = AppLanguage.NewWorkoutSetColumnWeight;
        RepsHeaderLabel.Text = AppLanguage.NewWorkoutSetColumnReps;
        CancelButton.Text = AppLanguage.NewWorkoutSetDetailsCancel;
        SaveButton.Text = AppLanguage.NewWorkoutSetDetailsSave;
    }

    private void BuildRows(int setCount, int? defaultReps)
    {
        for (int i = 1; i <= setCount; i++)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8
            };

            var setLabel = new Label
            {
                Text = i.ToString(),
                FontSize = 13,
                FontFamily = "OpenSansSemibold",
                TextColor = ColorResources.GetColor("AccentGlow", "#A78BFA"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(setLabel, 0);
            grid.Children.Add(setLabel);

            var weightEntry = BuildNumericEntry("0");
            Grid.SetColumn(weightEntry, 1);
            grid.Children.Add(weightEntry);
            _weightEntries.Add(weightEntry);

            var repsEntry = BuildNumericEntry("0");
            if (defaultReps is > 0)
                repsEntry.Text = defaultReps.Value.ToString();
            Grid.SetColumn(repsEntry, 2);
            grid.Children.Add(repsEntry);
            _repsEntries.Add(repsEntry);

            RowsContainer.Children.Add(grid);
        }
    }

    private static Entry BuildNumericEntry(string placeholder)
    {
        return new Entry
        {
            Placeholder = placeholder,
            Keyboard = Keyboard.Numeric,
            FontSize = 14,
            FontFamily = "OpenSansSemibold",
            TextColor = ColorResources.GetColor("TextPrimary", "#F7F7FB"),
            PlaceholderColor = ColorResources.GetColor("TextMuted", "#6B6780"),
            BackgroundColor = ColorResources.GetColor("Surface", "#13101C"),
            HorizontalTextAlignment = TextAlignment.Center
        };
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isClosing) return;

        var collected = new List<SetDetail>();
        for (int i = 0; i < _repsEntries.Count; i++)
        {
            if (!int.TryParse(_repsEntries[i].Text, out int reps) || reps <= 0)
            {
                ErrorLabel.Text = AppLanguage.NewWorkoutSetDetailsRepsRequired;
                ErrorLabel.IsVisible = true;
                return;
            }

            double? weight = null;
            if (!string.IsNullOrWhiteSpace(_weightEntries[i].Text) &&
                MetricInput.TryParseFlexibleDouble(_weightEntries[i].Text, out double w) && w > 0)
            {
                weight = w;
            }

            collected.Add(new SetDetail { SetNumber = i + 1, Reps = reps, Weight = weight });
        }

        _isClosing = true;
        _result.TrySetResult(collected);
        await _page.ClosePopupAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _result.TrySetResult(null);
        await _page.ClosePopupAsync();
    }
}
