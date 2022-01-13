using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Replicas;

internal partial class FolderBrowser
{
    internal FolderBrowser()
    {
        _selectedDirectory = "C:\\";
        InitializeComponent();
    }

    internal FolderBrowser(string selectedDirectory)
    {
        _selectedDirectory = selectedDirectory;
        InitializeComponent();
    }

    private string _selectedDirectory;

    public string SelectedDirectory => _selectedDirectory;

    private void ComboDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboDrives.SelectedItem == null)
        {
            return;
        }

        var dName = ComboDrives.SelectedItem.ToString()?[..3];
        if (!string.IsNullOrEmpty(dName))
        {
            DisplayDirectory(dName);
        }
    }

    private void DisplayDirectory(string directoryPath)
    {
        ListboxChildren.Items.Clear();
        TextBlockPath.ToolTip = TextBlockPath.Text = directoryPath;
        _selectedDirectory = TextBlockPath.Text;
        try
        {
            var subs = Directory.GetDirectories(directoryPath);
            foreach (string subDir in subs)
            {
                DirectoryInfo di = new(subDir);
                bool directoryIsHidden = (di.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                bool permitHidden = false;
                if (ChkHidden.IsChecked.HasValue)
                {
                    if (ChkHidden.IsChecked.Value)
                    {
                        permitHidden = true;
                    }
                }

                if (permitHidden || !directoryIsHidden)
                {
                    string s = Path.GetFileName(subDir);
                    ListboxChildren.Items.Add(s);
                } // GetFileName gets the last element in the path even if this is a directory name
            }
        }
        catch (UnauthorizedAccessException uaException)
        {
            MessageBox.Show(uaException.Message);
        }
        catch (ArgumentNullException ex)
        {
            MessageBox.Show(ex.Message);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message);
        }
        catch (PathTooLongException ex)
        {
            MessageBox.Show(ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            MessageBox.Show(ex.Message);
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void ListboxChildren_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ListboxChildren.SelectedItem is string q)
        {
            string nextPath = Path.Combine(TextBlockPath.Text, q);
            DisplayDirectory(nextPath);
        }
    }

    private void ButtonUp_Click(object sender, RoutedEventArgs e)
    {
        if (TextBlockPath.Text.Length < 4)
        {
            return;
        }

        var kk = Path.GetDirectoryName(TextBlockPath.Text);
        if (!string.IsNullOrEmpty(kk))
        {
            DisplayDirectory(kk);
        }
    }

    private void ButtonSelect_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void ButtonCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ButtonCreate_Click(object sender, RoutedEventArgs e)
    {
        string q = NewDirectoryTextBox.Text.Trim();
        if (string.IsNullOrEmpty(q))
        {
            MessageBox.Show("Enter a name for the new directory in the text box", "Create directory"
                , MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string newDirectoryPath = Path.Combine(_selectedDirectory, q);
        Directory.CreateDirectory(newDirectoryPath);
        DisplayDirectory(_selectedDirectory);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ComboDrives.Items.Clear();
        foreach (DriveInfo drv in DriveInfo.GetDrives())
        {
            if (drv.IsReady)
            {
                ComboDrives.Items.Add(drv.Name + " [" + drv.VolumeLabel + "]");
            }
        }

        if (ComboDrives.Items.Count > 0)
        {
            ComboDrives.SelectedIndex = 0;
        }

        if (Owner != null)
        {
            Icon = Owner.Icon;
        }
    }

    private void ChkHidden_Checked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TextBlockPath.Text))
        {
            return;
        }

        DisplayDirectory(TextBlockPath.Text);
    }

    private void ChkHidden_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TextBlockPath.Text))
        {
            DisplayDirectory(TextBlockPath.Text);
        }
    }

    public void SetTitle(string titleBarText)
    {
        Title = titleBarText;
    }
}