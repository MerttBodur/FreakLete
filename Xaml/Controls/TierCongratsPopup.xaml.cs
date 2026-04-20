using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;
using Microsoft.Maui.Controls.Shapes;

namespace FreakLete.Xaml.Controls;

public partial class TierCongratsPopup : ContentView
{
    private readonly Page _page;
    private bool _hasAnimatedIn;
    private bool _isClosing;

    private TierCongratsPopup(Page page, TierResult tier)
    {
        _page = page;
        InitializeComponent();
        Opacity = 0;
        Scale = 0.92;
        Loaded += OnLoaded;
        ApplyLanguage();
        Bind(tier);
    }

    public static Task ShowAsync(Page page, TierResult tier)
    {
        try
        {
            var popup = new TierCongratsPopup(page, tier);
            return page.ShowPopupAsync(popup, new PopupOptions
            {
                CanBeDismissedByTappingOutsideOfPopup = true,
                PageOverlayColor = Colors.Transparent,
                Shape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(24),
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = new SolidColorBrush(Colors.Transparent),
                    StrokeThickness = 0
                },
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Transparent),
                    Opacity = 0,
                    Radius = 0,
                    Offset = new Point(0, 0)
                }
            });
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[TierCongratsPopup] Display failed: {ex}");
#endif
            return Task.CompletedTask;
        }
    }

    private void ApplyLanguage()
    {
        TitleLabel.Text = AppLanguage.TierCongratsTitle;
        CloseButton.Text = AppLanguage.TierCloseButton;
        MaxTierLabel.Text = AppLanguage.TierMaxTierText;
    }

    private void Bind(TierResult tier)
    {
        SubtitleLabel.Text = tier.PreviousTierLevel is null
            ? AppLanguage.TierFirstTierText(tier.TierLevel)
            : AppLanguage.TierLevelUpText(tier.TierLevel);

        TierBadge.BackgroundColor = GetTierBadgeColor(tier.TierLevel);

        if (string.IsNullOrEmpty(tier.NextLevel) || tier.NextDelta is null)
        {
            NextMilestoneStack.IsVisible = false;
            MaxTierLabel.IsVisible = true;
        }
        else
        {
            NextMilestoneStack.IsVisible = true;
            MaxTierLabel.IsVisible = false;
            NextMilestoneLabel.Text =
                $"{AppLanguage.TierNextMilestonePrefix(tier.NextLevel)} - {TierDisplayFormatter.FormatDelta(tier.TrackingMode, tier.NextDelta.Value)}";

            EventHandler? sizeHandler = null;
            sizeHandler = (_, _) =>
            {
                var container = NextMilestoneStack.Width;
                if (container > 0)
                {
                    ProgressBar.WidthRequest = container * Math.Clamp(tier.ProgressPercent, 0, 100) / 100;
                    NextMilestoneStack.SizeChanged -= sizeHandler;
                }
            };
            NextMilestoneStack.SizeChanged += sizeHandler;
        }
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_hasAnimatedIn)
        {
            return;
        }

        _hasAnimatedIn = true;

        try
        {
            await AnimateOpenAsync();
        }
        catch
        {
            // The popup may already be closing or gone.
        }
    }

    private Task AnimateOpenAsync()
        => AnimatePopupAsync(1, 1, 200, Easing.CubicOut);

    private Task AnimateCloseAsync()
        => AnimatePopupAsync(0, 0.92, 180, Easing.CubicIn);

    private Task AnimatePopupAsync(double opacity, double scale, uint duration, Easing easing)
        => Task.WhenAll(this.FadeToAsync(opacity, duration, easing), this.ScaleToAsync(scale, duration, easing));

    private static Color GetTierBadgeColor(string tierLevel)
    {
        var res = Application.Current?.Resources;
        if (res is null) return Colors.Transparent;
        string key = tierLevel switch
        {
            "NeedImprovement" => "SurfaceStrong",
            "Beginner"        => "Info",
            "Intermediate"    => "Success",
            "Advanced"        => "AccentGlow",
            "Elite"           => "Accent",
            "Freak"           => "Accent",
            _                 => "SurfaceStrong"
        };
        return res.TryGetValue(key, out var v) && v is Color c ? c : Colors.Transparent;
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;

        try
        {
            await AnimateCloseAsync();
            await _page.ClosePopupAsync();
        }
        catch
        {
            // already dismissed
        }
    }
}
