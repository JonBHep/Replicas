using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Replicas;

internal partial class FolderBrowser : Window
{
    public FolderBrowser()
    {
        InitializeComponent();
    }
    private string _selectedDirectory;
        //private readonly string _naString = "Not accessible";

        public string SelectedDirectory
        {
            get
            {
                return _selectedDirectory;
            }
        }

        private void ComboDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboDrives.SelectedItem == null) { return; };
            string dName = comboDrives.SelectedItem.ToString().Substring(0, 3);
            DisplayDirectory(dName);
        }

        private void DisplayDirectory(string directoryPath)
        {
            listboxChildren.Items.Clear();
            textblockPath.ToolTip = textblockPath.Text = directoryPath;
            _selectedDirectory = textblockPath.Text;
            string[] subs;
            try
            {
                subs = Directory.GetDirectories(directoryPath);
                foreach (string subdir in subs)
                {
                    DirectoryInfo di = new DirectoryInfo(subdir);
                    bool isHidden = (di.Attributes & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden;
                    if (!isHidden || (bool)chkHidden.IsChecked)
                    {
                        listboxChildren.Items.Add(System.IO.Path.GetFileName(subdir));
                    } // GetFileName gets the last element in the path even if this is a directory name
                }
            }
            catch(UnauthorizedAccessException uaex)
            {
                MessageBox.Show(uaex.Message);
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
            //{ listboxChildren.Items.Add(_naString); }
        }

        private void ListboxChildren_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listboxChildren.SelectedItems.Count < 1) { return; };
            //if (listboxChildren.SelectedItems[0].ToString() == _naString) { return; };
            string nextPath = System.IO.Path.Combine(textblockPath.Text, listboxChildren.SelectedItems[0].ToString());
            DisplayDirectory(nextPath);
        }

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            if (textblockPath.Text.Length < 4) { return; };
            string nextPath = System.IO.Path.GetDirectoryName(textblockPath.Text);
            DisplayDirectory(nextPath);
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
                MessageBox.Show("Enter a name for the new directory in the text box", "Create directory", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string newDirectoryPath = System.IO.Path.Combine(_selectedDirectory, q);
            System.IO.Directory.CreateDirectory(newDirectoryPath);
            DisplayDirectory(_selectedDirectory);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            comboDrives.Items.Clear();
            foreach (System.IO.DriveInfo drv in System.IO.DriveInfo.GetDrives())
            {
                if (drv.IsReady)
                { comboDrives.Items.Add(drv.Name + " [" + drv.VolumeLabel + "]"); }
            }
            if (comboDrives.Items.Count > 0) { comboDrives.SelectedIndex = 0; }
            if (this.Owner != null) { Icon = this.Owner.Icon; }
        }

        private void ChkHidden_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textblockPath.Text)) { return; };
            DisplayDirectory(textblockPath.Text);
        }

        private void ChkHidden_Unchecked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textblockPath.Text)) { return; };
            DisplayDirectory(textblockPath.Text);
        }

        public void SetTitle(string titleBarText)
        {
            this.Title = titleBarText;
        }
}