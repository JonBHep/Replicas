using System.Linq;
using System.Windows;

namespace Replicas;

internal partial class JobEditorWindow : Window
{
    internal JobEditorWindow()
    {
        InitializeComponent();
    }
    internal string AaTitle;
        internal string AaSourcePath;
        internal string AaSourceVolume;
        internal string AaDestinationPath;
        internal string AaDestinationVolume;
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
            AaDestinationPath = lblDestinationPath.Text;
            AaDestinationVolume = lblDestinationVolume.Text;
            AaIncludeHiddenFiles = chkHidden.IsChecked.Value;
            AaDangerous = DangerousCheckBox.IsChecked.Value;
            AaSourcePath = lblSourcePath.Text;
            AaSourceVolume = lblSourceVolume.Text;
            AaTitle = lblTitle.Text;
            DialogResult = true;
        }

        private bool OkToEnd
        {
            get
            {
                bool b = !(lblTitle.Text == "?");

                if (lblDestinationPath.Text == "?") { b = false; }
                if (lblDestinationVolume.Text == "?") { b = false; }
                if (lblSourcePath.Text == "?") { b = false; }
                if (lblSourceVolume.Text == "?") { b = false; }
                if (string.IsNullOrWhiteSpace(lblTitle.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(lblDestinationPath.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(lblDestinationVolume.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(lblSourcePath.Text)) { b = false; }
                if (string.IsNullOrWhiteSpace(lblSourceVolume.Text)) { b = false; }
                return b;
            }
        }

        private void TitleButton_Click(object sender, RoutedEventArgs e)
        {
            InputBox ib = new InputBox("Job title", "Give this backup job a title", lblTitle.Text) { Owner = this };
            if (ib.ShowDialog() == true)
            {
                lblTitle.Text = ib.ResponseText;
                btnOkay.IsEnabled = OkToEnd;
            }
        }

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowser fb = new FolderBrowser();
            if (fb.ShowDialog() == true)
            {
                lblSourcePath.Text = fb.SelectedDirectory;
                string dlabel = Kernel.Instance.DrivesCurrentlyFound.DriveVolumeLabel(lblSourcePath.Text.ElementAt(0));
                lblSourceVolume.Text = dlabel;
                lblSourceVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(dlabel);
                btnOkay.IsEnabled = OkToEnd;
            }
        }

        private void DestinationButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowser fb = new FolderBrowser();
            if (fb.ShowDialog() == true)
            {
                lblDestinationPath.Text = fb.SelectedDirectory;
                string dlabel = Kernel.Instance.DrivesCurrentlyFound.DriveVolumeLabel(lblDestinationPath.Text.ElementAt(0));
                lblDestinationVolume.Text = dlabel;
                lblDestinationVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(dlabel);
                btnOkay.IsEnabled = OkToEnd;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AaTitle == "?")
            {
                lblDestinationPath.Text = "?";
                lblDestinationVolume.Text = "?";
                lblSourcePath.Text = "?";
                lblSourceVolume.Text = "?";
                lblSpace.Text = "";
                lblTitle.Text = "?";
                chkHidden.IsChecked = true;
                JbhInfoRadioButton.IsChecked = false;
                JbhBusinessRadioButton.IsChecked = false;
                OtherRadioButton.IsChecked = true;
                Title = "New backup job";
            }
            else
            {
                lblDestinationPath.Text = AaDestinationPath;
                lblDestinationVolume.Text = AaDestinationVolume;
                lblDestinationVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(AaDestinationVolume);
                lblSourcePath.Text = AaSourcePath;
                lblSourceVolume.Text = AaSourceVolume;
                lblSourceVolumeDescription.Text = Kernel.Instance.KnownDrives.VolumeDescription(AaSourceVolume);
                lblSpace.Text = "";
                lblTitle.Text = AaTitle;
                chkHidden.IsChecked = AaIncludeHiddenFiles;
                DangerousCheckBox.IsChecked = AaDangerous;
                OtherRadioButton.IsChecked = true;
                JbhInfoRadioButton.IsChecked = AaIsJbhInfoBackup;
                JbhBusinessRadioButton.IsChecked = AaIsJbhBusinessBackup;
                Title = "Backup job details";
                if (Inaccessible)
                {
                    lblSpace.Text = "Free space on destination drive: inaccessible";
                }
                else
                {
                    lblSpace.Text = "Free space on destination drive: " + DriveFreeReport;
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
            btnOkay.IsEnabled = OkToEnd;
        }
        
        private void DangerousCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            btnOkay.IsEnabled = OkToEnd;
        }
}