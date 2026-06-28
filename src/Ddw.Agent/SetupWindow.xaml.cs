using System.Windows;

namespace Ddw.Agent;

public partial class SetupWindow : Window
{
    public UserConfig Result { get; private set; }

    public SetupWindow(UserConfig existing)
    {
        InitializeComponent();
        Result = existing;
        EmailBox.Text = existing.Upn;
        LocationBox.Text = existing.Location;
        DeptBox.Text = existing.Department;
        ProjectBox.Text = existing.Project;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            ErrorText.Text = "Please enter a valid company email.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        Result = new UserConfig
        {
            Upn = email,
            Location = LocationBox.Text.Trim(),
            Department = DeptBox.Text.Trim(),
            Project = ProjectBox.Text.Trim(),
            DeviceName = Environment.MachineName
        };
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
