using System.Media;
using System.Windows;
using System.Windows.Media;

namespace Ddw.Agent;

public partial class PopupWindow : Window
{
    private readonly NotificationDto _note;
    private readonly UserConfig _config;

    public PopupWindow(NotificationDto note, UserConfig config)
    {
        InitializeComponent();
        _note = note;
        _config = config;

        TitleText.Text = note.Title;
        BodyText.Text = note.Body;
        MetaText.Text = $"{note.Category}  •  {note.PublishAt.ToLocalTime():dd MMM yyyy, HH:mm}";
        ActionBtn.Content = note.RequiresAck ? "Acknowledge" : "Got it";

        var (color, label) = note.Priority switch
        {
            Priorities.Critical => ("#C00000", $"⚠ CRITICAL — {note.Category.ToUpper()}"),
            Priorities.High     => ("#ED7D31", $"{note.Category.ToUpper()} — HIGH"),
            Priorities.Low      => ("#6B7280", note.Category.ToUpper()),
            _                   => ("#1F4E79", note.Category.ToUpper()),
        };
        HeaderBar.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(color)!;
        PriorityLabel.Text = label;

        if (note.Priority >= Priorities.High) SystemSounds.Exclamation.Play();

        if (note.Priority == Priorities.Critical)
        {
            // Full-screen, dimmed, must acknowledge.
            SizeToContent = SizeToContent.Manual;
            WindowState = WindowState.Maximized;
            RootGrid.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString("#D91F2937")!;
            Card.Width = 480;
            DismissBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            Loaded += PositionBottomRight;
        }
    }

    private void PositionBottomRight(object? sender, RoutedEventArgs e)
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - ActualWidth - 16;
        Top = wa.Bottom - ActualHeight - 16;
    }

    private async void Action_Click(object sender, RoutedEventArgs e)
    {
        if (_note.RequiresAck) await ApiClient.AckAsync(_note.Id, _config);
        else await ApiClient.ReadAsync(_note.Id, _config);
        Close();
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e) => Close(); // not read -> reappears next poll
}
