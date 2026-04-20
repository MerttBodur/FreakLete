using Microsoft.Maui.Controls.Shapes;

namespace FreakLete;

[ContentProperty(nameof(InputContent))]
public sealed class InputShell : ContentView
{
	public static readonly BindableProperty InputContentProperty =
		BindableProperty.Create(
			nameof(InputContent),
			typeof(View),
			typeof(InputShell),
			propertyChanged: OnInputContentChanged);

	public static readonly BindableProperty StateProperty =
		BindableProperty.Create(
			nameof(State),
			typeof(InputShellState),
			typeof(InputShell),
			InputShellState.Normal,
			propertyChanged: OnStateChanged);

	private readonly Border _border;
	private View? _attachedView;
	private bool _isFocused;

	public InputShell()
	{
		_border = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
			StrokeThickness = 1,
			Padding = new Thickness(12, 0),
			MinimumHeightRequest = 52,
			HorizontalOptions = LayoutOptions.Fill
		};

		base.Content = _border;
		UpdateVisualState();
	}

	public View? InputContent
	{
		get => (View?)GetValue(InputContentProperty);
		set => SetValue(InputContentProperty, value);
	}

	public InputShellState State
	{
		get => (InputShellState)GetValue(StateProperty);
		set => SetValue(StateProperty, value);
	}

	private static void OnInputContentChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var shell = (InputShell)bindable;
		if (oldValue is View oldView)
		{
			shell.Detach(oldView);
		}

		if (newValue is View newView)
		{
			shell.Attach(newView);
			shell._border.Content = newView;
		}
		else
		{
			shell._border.Content = null;
		}

		shell.UpdateVisualState();
	}

	private static void OnStateChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((InputShell)bindable).UpdateVisualState();
	}

	private void Attach(View view)
	{
		_attachedView = view;
		view.Focused += OnFocused;
		view.Unfocused += OnUnfocused;
		view.PropertyChanged += OnInputPropertyChanged;
		NormalizeInputChrome(view);
	}

	private void Detach(View view)
	{
		view.Focused -= OnFocused;
		view.Unfocused -= OnUnfocused;
		view.PropertyChanged -= OnInputPropertyChanged;
		if (ReferenceEquals(_attachedView, view))
		{
			_attachedView = null;
		}
	}

	private void OnFocused(object? sender, FocusEventArgs e)
	{
		_isFocused = true;
		UpdateVisualState();
	}

	private void OnUnfocused(object? sender, FocusEventArgs e)
	{
		_isFocused = false;
		UpdateVisualState();
	}

	private void OnInputPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IsEnabled))
		{
			UpdateVisualState();
		}
	}

	private void NormalizeInputChrome(View view)
	{
		view.BackgroundColor = Colors.Transparent;
		view.MinimumHeightRequest = Math.Max(view.MinimumHeightRequest, 52);
		view.HorizontalOptions = LayoutOptions.Fill;

		switch (view)
		{
			case Entry entry:
				entry.TextColor = ResourceColor("TextPrimary");
				entry.PlaceholderColor = ResourceColor("TextMuted");
				entry.FontFamily = "OpenSansSemibold";
				entry.FontSize = 15;
				break;
			case Editor editor:
				editor.TextColor = ResourceColor("TextPrimary");
				editor.PlaceholderColor = ResourceColor("TextMuted");
				editor.FontFamily = "OpenSansRegular";
				editor.FontSize = 14;
				break;
			case SearchBar searchBar:
				searchBar.TextColor = ResourceColor("TextPrimary");
				searchBar.PlaceholderColor = ResourceColor("TextMuted");
				searchBar.CancelButtonColor = ResourceColor("TextMuted");
				searchBar.FontFamily = "OpenSansRegular";
				searchBar.FontSize = 14;
				break;
			case Picker picker:
				picker.TextColor = ResourceColor("TextPrimary");
				picker.TitleColor = ResourceColor("TextMuted");
				picker.FontFamily = "OpenSansSemibold";
				picker.FontSize = 15;
				break;
			case DatePicker datePicker:
				datePicker.TextColor = ResourceColor("TextPrimary");
				datePicker.FontFamily = "OpenSansSemibold";
				datePicker.FontSize = 15;
				break;
		}
	}

	private void UpdateVisualState()
	{
		var isDisabled = _attachedView is { IsEnabled: false } || !IsEnabled;
		var state = isDisabled ? InputShellState.Disabled : State;

		Color stroke = state switch
		{
			InputShellState.Error => ResourceColor("Danger"),
			InputShellState.Focused => ResourceColor("Accent"),
			_ when _isFocused => ResourceColor("Accent"),
			_ => ResourceColor("SurfaceBorder")
		};

		_border.BackgroundColor = ResourceColor("SurfaceRaised");
		_border.Stroke = new SolidColorBrush(stroke);
		Opacity = state == InputShellState.Disabled ? 0.45 : 1;
	}

	private static Color ResourceColor(string key)
	{
		if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
		{
			return color;
		}

		return key switch
		{
			"Accent" => Color.FromArgb("#8B5CF6"),
			"Danger" => Color.FromArgb("#DC2626"),
			"SurfaceBorder" => Color.FromArgb("#342D46"),
			"SurfaceRaised" => Color.FromArgb("#1D1828"),
			"TextMuted" => Color.FromArgb("#8A889B"),
			_ => Color.FromArgb("#F7F7FB")
		};
	}
}

public enum InputShellState
{
	Normal,
	Focused,
	Error,
	Disabled
}
