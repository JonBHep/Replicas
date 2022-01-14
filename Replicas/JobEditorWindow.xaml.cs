using System.Linq;
using System.Windows;

namespace Replicas;

internal partial class JobEditorWindow
{
    internal JobEditorWindow()
    {
        InitializeComponent();
    }
    internal string? AaTitle;
        internal string? AaSourcePath;
        internal string? AaSourceVolume;
        internal string? AaDestinationPath;
        internal string? AaDestinationVolume;
        internal bool AaIncludeHiddenFiles;
        internal bool AaDangerous;
        internal bool AaIsJbhInfoBackup;
        internal bool AaIsJbhBusinessBackup;

        internal bool Inaccessible;

        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OkayButton_Click(object sender, RoutedEventArgs e)
        {
            AaIsJbhInfoBackup = JbhInfoRadioButton.IsChecked.HasValue && JbhInfoRadioButton.IsChecked.Value;
            AaIsJbhBusinessBackup = JbhBusinessRadioButton.IsChecked.HasValue && JbhBusinessRadioButton.IsChecked.Value;
            AaDestinationPath = LblDestinationPath.Text;
            AaDestinationVolume = LblDestinationVolume.Text;
            AaIncludeHiddenFiles =ChkHidden.IsChecked.HasValue && ChkHidden.IsChecked.Value;
            AaDangerous =DangerousCheckBox.IsChecked.HasValue && DangerousCheckBox.IsChecked.Value;
            AaSourcePath = LblSourcePath.Text;
            AaSourceVolume = LblSourceVolume.Text;
            AaTitle = LblTitle.Text;
            DialogResult = true;
        }

        private bool OkToEnd
        {
            get
            {
                bool b = LblTitle.Text != "?";

                if (LblDestinationPath.Text == "?") { b = false; }
                if (LblDestinationVolume.Text == "?") { b = false; }
                if (LblSourcePath.Text == "?") { b = false; }
                if (LblSourceVolume.Text == "?") { b = false; }
                if (string.IsNullOrWhiteSpace(LblTitle.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(LblDestinationPath.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(LblDestinationVolume.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(LblSourcePath.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(LblSourceVolume.Text)) { b = false; }
                return b;
            }
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            InputBox ib = new InputBox("Job title", "Give this backup job a title", LblTitle.Text) { Owner = this };
            if (ib.ShowDialog() == true)
            {
                LblTitle.Text = ib.ResponseText;
                BtnOkay.IsEnabled = OkToEnd;
            }
        }

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowser fb = new FolderBrowser();
            if (fb.ShowDialog() == true)
            {
                LblSourcePath.Text = fb.SelectedDirectory;
                string dlabel = Kernel.Instance.DrivesCurrentlyFound.DriveVolumeLabel(LblSourcePath.Text.ElementAt(0));
                LblSourceVolume.Text = dlabel;
                LblSourceVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(dlabel);
                BtnOkay.IsEnabled = OkToEnd;
            }
        }

        private void DestinationButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowser fb = new FolderBrowser();
            if (fb.ShowDialog() == true)
            {
                LblDestinationPath.Text = fb.SelectedDirectory;
                string dlabel = Kernel.Instance.DrivesCurrentlyFound.DriveVolumeLabel(LblDestinationPath.Text.ElementAt(0));
                LblDestinationVolume.Text = dlabel;
                LblDestinationVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(dlabel);
                BtnOkay.IsEnabled = OkToEnd;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AaTitle == "?")
            {
                LblDestinationPath.Text = "?";
                LblDestinationVolume.Text = "?";
                LblSourcePath.Text = "?";
                LblSourceVolume.Text = "?";
                LblSpace.Text = "";
                LblTitle.Text = "?";
                ChkHidden.IsChecked = true;
                JbhInfoRadioButton.IsChecked = false;
                JbhBusinessRadioButton.IsChecked = false;
                OtherRadioButton.IsChecked = true;
                Title = "New backup job";
            }
            else
            {
                LblDestinationPath.Text = AaDestinationPath;
                LblDestinationVolume.Text = AaDestinationVolume;
                LblDestinationVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(AaDestinationVolume);
                LblSourcePath.Text = AaSourcePath;
                LblSourceVolume.Text = AaSourceVolume;
                LblSourceVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(AaSourceVolume);
                LblSpace.Text = "";
                LblTitle.Text = AaTitle;
                ChkHidden.IsChecked = AaIncludeHiddenFiles;
                DangerousCheckBox.IsChecked = AaDangerous;
                OtherRadioButton.IsChecked = true;
                JbhInfoRadioButton.IsChecked = AaIsJbhInfoBackup;
                JbhBusinessRadioButton.IsChecked = AaIsJbhBusinessBackup;
                Title = "Backup job details";
                if (Inaccessible)
                {
                    LblSpace.Text = "Free space on destination drive: inaccessible";
                }
                else
                {
                    LblSpace.Text = "Free space on destination drive: " + DriveFreeReport;
                }
            }
        }

        private string DriveFreeReport
        {
            get
            {
                long rv;
                char? dlet = Kernel.Instance.DrivesCurrentlyFound.DriveLetter(AaDestinationVolume);
                if (dlet is char)
                {
                    string slet = dlet.ToString();
                    System.IO.DriveInfo diobj = new System.IO.DriveInfo(slet);
                    if (diobj.IsReady)
                    {
                        rv = diobj.AvailableFreeSpace;
                        return Kernel.SizeReport(rv);
                    }
                    else
                    { return string.Empty; }
                }
                else
                { return string.Empty; }
            }
        }

        private void DatabankCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            BtnOkay.IsEnabled = OkToEnd;
        }
        
        private void DangerousCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            BtnOkay.IsEnabled = OkToEnd;
        }
}