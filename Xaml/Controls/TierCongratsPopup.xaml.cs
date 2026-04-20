using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using FreakLete.Helpers;
using FreakLete.Models;
using FreakLete.Services;

namespace FreakLete.Xaml.Controls;

public partial class TierCongratsPopup : ContentView
{
    private readonly Page _page;

    private TierCongratsPopup(Page page, TierResult tier)
    {
        _page = page;
        InitializeComponent();
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
                CanBeDismissedByTappingOutsideOfPopup = true
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
                $"{AppLanguage.TierNextMilestonePrefix(tier.NextLevel)} — {TierDisplayFormatter.FormatDelta(tier.TrackingMode, tier.NextDelta.Value)}";

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
        try { await _page.ClosePopupAsync(); }
        catch { /* already dismissed */ }
    }
}
