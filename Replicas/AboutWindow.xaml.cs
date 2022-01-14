using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Replicas;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }
    
    private void PaintCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DialogResult = false;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        DateTime lastVersion = new DateTime(2022, 1, 14);
        TimeSpan versionAge = DateTime.Today - lastVersion;
        var daze = (int)versionAge.TotalDays;
        var dayString = daze == 1 ? "day" : "days";
        textblockVersion.Text = $"First .NET 6.0 version {lastVersion:dd MMM yyyy} ({daze} {dayString} old)";
        textblockTitle.Text = Jbh.AppManager.AppName;
        textblockDescription.Text = "Backup utility";
        textblockCopyright.Text ="Jonathan Hepworth 2015 - 2022";
        HistoryTextBlock.Text = "Based on a series of my previous VB and C# applications including 'Copier', 'Replicate' etc.";
        ImplementationTextBlock.Text = "Adapted January 2022 using Rider IDE for the first time in WPF / C# / .NET 6.0";
        
        // TODO Keep this information updated
    }
}