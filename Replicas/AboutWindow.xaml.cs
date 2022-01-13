using System.Windows;

namespace Replicas;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }
// TODO Design this window
    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}