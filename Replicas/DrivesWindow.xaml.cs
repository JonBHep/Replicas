using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Replicas;

internal partial class DrivesWindow : Window
{
    public DrivesWindow()
    {
        InitializeComponent();
    }
    private string currentDriveLettersOfKnownDrives;
        private bool loaded;

        private ContextMenu ItemContextMenu;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ItemContextMenu = new ContextMenu();
            MenuItem i = new MenuItem() { Header = "Edit the description of the selected drive" };
            i.Click += EditDriveDescriptionMenuItem_Click;
            ItemContextMenu.Items.Add(i);
            i = new MenuItem() { Header = "Forget the selected drive" };
            i.Click += ForgetDriveMenuItem_Click;
            ItemContextMenu.Items.Add(i);

            Kernel.Instance.KnownDrives.RefreshDriveList();
            RefreshListOfKnownDrives();
            RefreshListOfFoundDrives();
            loaded = true;
        }

        private void RefreshListOfKnownDrives()
        {
            Kernel.Instance.KnownDrives.GetJobReferences();
            double[] colwidth = { 300, 120, 40, 80, 128, 60, 60 };

            currentDriveLettersOfKnownDrives = string.Empty;
            RecognisedDrivesListBox.Items.Clear();
            ListOneHeadingsStackPanel.Margin = new Thickness(12, 0, 0, 3);
            ListOneHeadingsStackPanel.Children.Clear();
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Description", FontWeight = FontWeights.SemiBold, Foreground = Brushes.DarkGreen, MinWidth = colwidth[0] });
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Label", FontWeight = FontWeights.Bold, MinWidth = colwidth[1] });
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Disk", FontWeight = FontWeights.Bold, Foreground = Brushes.DarkGreen, MinWidth = colwidth[2], TextAlignment = TextAlignment.Center });
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Capacity", FontWeight = FontWeights.SemiBold, Foreground = Brushes.SaddleBrown, MinWidth = colwidth[3], TextAlignment = TextAlignment.Center });
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Capacity", FontWeight = FontWeights.SemiBold, Foreground = Brushes.DarkMagenta, MinWidth = colwidth[4], TextAlignment = TextAlignment.Center });
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Used", FontWeight = FontWeights.SemiBold, Foreground = Brushes.IndianRed, MinWidth = colwidth[5], TextAlignment = TextAlignment.Center });
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Jobs", FontWeight = FontWeights.SemiBold, Foreground = Brushes.Crimson, MinWidth = colwidth[6], TextAlignment = TextAlignment.Center });
            ListOneHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Connected", FontWeight = FontWeights.SemiBold, Foreground = Brushes.DarkGreen });

            // find maxiumum drive capacity so that others can be compared
            long maxsize = 0;
            for (int a = 0; a < Kernel.Instance.KnownDrives.RecognisedDrives.Count; a++)
            {
                string dlet = Kernel.Instance.KnownDrives.CurrentDriveLetter(a);
                if (!string.IsNullOrEmpty(dlet))
                {
                    long siz = Kernel.Instance.DrivesCurrentlyFound.DriveVolumeSize(dlet[0]);
                    if ((siz > 0) && (siz != Kernel.Instance.KnownDrives.RecognisedDrives[a].Capacity)) { Kernel.Instance.KnownDrives.RecognisedDrives[a].Capacity = siz; }
                }
                long sz = Kernel.Instance.KnownDrives.RecognisedDrives[a].Capacity;
                maxsize = Math.Max(maxsize, sz);
            }

            for (int a = 0; a < Kernel.Instance.KnownDrives.RecognisedDrives.Count; a++)
            {
                TextBlock DescriptionBloc = new TextBlock() { Text = Kernel.Instance.KnownDrives.RecognisedDrives[a].MyDescription, FontWeight = FontWeights.SemiBold, Foreground = Brushes.DarkGreen, MinWidth = colwidth[0] };
                TextBlock LabelBloc = new TextBlock() { Text = Kernel.Instance.KnownDrives.RecognisedDrives[a].VolumeLabel, FontWeight = FontWeights.Bold, MinWidth = colwidth[1] };
                string dlet = Kernel.Instance.KnownDrives.CurrentDriveLetter(a);
                TextBlock LetterBloc = new TextBlock() { Text = dlet, FontWeight = FontWeights.Black, Foreground = Brushes.DarkGreen, MinWidth = colwidth[2], TextAlignment = TextAlignment.Center };
                if (string.IsNullOrEmpty(dlet))
                {
                    LetterBloc.Text = string.Empty;
                }
                else
                {
                    LetterBloc.Text = dlet;
                    currentDriveLettersOfKnownDrives += dlet;
                    long siz = Kernel.Instance.DrivesCurrentlyFound.DriveVolumeSize(dlet[0]);
                    if ((siz > 0) && (siz != Kernel.Instance.KnownDrives.RecognisedDrives[a].Capacity)) { Kernel.Instance.KnownDrives.RecognisedDrives[a].Capacity= siz; }
                    int usd = Kernel.Instance.DrivesCurrentlyFound.DriveVolumeUsedPercent(dlet[0]);
                    if ((usd > 0) && (usd != Kernel.Instance.KnownDrives.RecognisedDrives[a].PercentUsed)) { Kernel.Instance.KnownDrives.RecognisedDrives[a].PercentUsed= usd; }
                    Kernel.Instance.KnownDrives.RecognisedDrives[a].LastConnected= DateTime.Now.ToBinary();
                }
                
                long sz = Kernel.Instance.KnownDrives.RecognisedDrives[a].Capacity;
                string szr = (sz == 0) ? string.Empty : Kernel.SizeReport(sz);
                TextBlock CapacityBloc = new TextBlock() { Text = szr, FontWeight = FontWeights.SemiBold, MinWidth = colwidth[3], Foreground = Brushes.SaddleBrown, TextAlignment = TextAlignment.Center };
                double rwidth = colwidth[4] * sz /(double) maxsize;
                Rectangle capacityRectangle = new Rectangle() { Height = 4, Width = rwidth, Fill = Brushes.DarkMagenta };
                Rectangle fillerRectangle = new Rectangle() { Height = 4, Width = colwidth[4] - rwidth, Fill = Brushes.Transparent };
                int pc = Kernel.Instance.KnownDrives.RecognisedDrives[a].PercentUsed;
                string pcr = (pc == 0) ? string.Empty : $"{pc} %";
                TextBlock PercentBloc = new TextBlock() { Text = pcr, FontWeight = FontWeights.SemiBold, MinWidth = colwidth[5], Foreground = Brushes.IndianRed, TextAlignment = TextAlignment.Center };
                TextBlock JobsBloc = new TextBlock() { Text = Kernel.Instance.KnownDrives.RecognisedDrives[a].JobReferences.ToString(CultureInfo.CurrentCulture), FontWeight = FontWeights.SemiBold, MinWidth = colwidth[6], Foreground = Brushes.Crimson, TextAlignment = TextAlignment.Center };
                TextBlock ConnectedBloc = new TextBlock() { Text = Kernel.Instance.KnownDrives.LastConnected(a), Foreground = Brushes.DarkGreen };

                StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
                sp.Children.Add(DescriptionBloc);
                sp.Children.Add(LabelBloc);
                sp.Children.Add(LetterBloc);
                sp.Children.Add(CapacityBloc);
                sp.Children.Add(capacityRectangle);
                sp.Children.Add(fillerRectangle);
                sp.Children.Add(PercentBloc);
                sp.Children.Add(JobsBloc);
                sp.Children.Add(ConnectedBloc);
                ListBoxItem item = new ListBoxItem() { Content = sp, Tag = a, ContextMenu = ItemContextMenu };
                RecognisedDrivesListBox.Items.Add(item);

                if (ShowJobsCheckBox.IsChecked.Value)
                {
                    foreach (string clef in Kernel.Instance.JobProfiles.Jobs.Keys)
                    {
                        if ((Kernel.Instance.JobProfiles.Jobs[clef].SourceVolume == Kernel.Instance.KnownDrives.RecognisedDrives[a].VolumeLabel) || (Kernel.Instance.JobProfiles.Jobs[clef].DestinationVolume == Kernel.Instance.KnownDrives.RecognisedDrives[a].VolumeLabel))
                        {
                            RecognisedDrivesListBox.Items.Add(new ListBoxItem() { IsHitTestVisible = false, Content = new TextBlock() { Text = Kernel.Instance.JobProfiles.Jobs[clef].JobTitle, Foreground = Brushes.Crimson, FontWeight = FontWeights.Medium, Margin = new Thickness(40, 0, 0, 0) } });
                        }
                    }
                }
            }
        } 
       
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ShowJobsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (loaded)
            {
                RefreshListOfKnownDrives();
            }
        }

        private void RefreshListOfFoundDrives()
        {
            double[] colwidth = { 120, 40, 80, 60 };
            DetectedDrives dd = Kernel.Instance.DrivesCurrentlyFound;
            string driveLetters = dd.DriveLetters;
            UnrecognisedDrivesListBox.Items.Clear();
            ListTwoHeadingsStackPanel.Margin = new Thickness(12, 0, 0, 3);
            ListTwoHeadingsStackPanel.Children.Clear();
            ListTwoHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Label", FontWeight = FontWeights.Bold, MinWidth = colwidth[0] });
            ListTwoHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Disk", FontWeight = FontWeights.Bold, Foreground = Brushes.DarkGreen, MinWidth = colwidth[1], TextAlignment = TextAlignment.Center });
            ListTwoHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Capacity", FontWeight = FontWeights.SemiBold, Foreground = Brushes.SaddleBrown, MinWidth = colwidth[2], TextAlignment = TextAlignment.Center });
            ListTwoHeadingsStackPanel.Children.Add(new TextBlock() { Text = "Used", FontWeight = FontWeights.SemiBold, Foreground = Brushes.IndianRed, MinWidth = colwidth[3], TextAlignment = TextAlignment.Center });
            for (int a = 0; a < driveLetters.Length; a++)
            {
                char letr = driveLetters[a];
                if (currentDriveLettersOfKnownDrives.IndexOf(letr) < 0)
                {
                    TextBlock LabelBloc = new TextBlock() { Text = dd.DriveVolumeLabel(letr), FontWeight = FontWeights.Bold, MinWidth = colwidth[0] };
                    TextBlock LetterBloc = new TextBlock() { Text = letr.ToString(), FontWeight = FontWeights.Black, Foreground = Brushes.DarkGreen, MinWidth = colwidth[1], TextAlignment = TextAlignment.Center };
                    long sz = dd.DriveVolumeSize(letr);
                    string szr = (sz == 0) ? string.Empty : Kernel.SizeReport(sz);
                    TextBlock CapacityBloc = new TextBlock() { Text = szr, FontWeight = FontWeights.SemiBold, MinWidth = colwidth[2], Foreground = Brushes.SaddleBrown, TextAlignment = TextAlignment.Center };
                    int pc = dd.DriveVolumeUsedPercent(letr);
                    string pcr = (pc == 0) ? string.Empty : $"{pc} %";
                    TextBlock PercentBloc = new TextBlock() { Text = pcr, FontWeight = FontWeights.SemiBold, MinWidth = colwidth[3], Foreground = Brushes.IndianRed, TextAlignment = TextAlignment.Center };

                    StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
                    sp.Children.Add(LabelBloc);
                    sp.Children.Add(LetterBloc);
                    sp.Children.Add(CapacityBloc);
                    sp.Children.Add(PercentBloc);
                    ListBoxItem item = new ListBoxItem() { Content = sp };
                    UnrecognisedDrivesListBox.Items.Add(item);
                }
            }
        }

        private void EditDriveDescriptionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (RecognisedDrivesListBox.SelectedIndex == -1) { return; }
            if (RecognisedDrivesListBox.SelectedItem is ListBoxItem im)
            {
                if (im.Tag is int index)
                {
                    string lbl = Kernel.Instance.KnownDrives.RecognisedDrives[index].VolumeLabel;
                    string cap = Kernel.Instance.KnownDrives.RecognisedDrives[index].MyDescription;
                    Jbh.InputBox ib = new Jbh.InputBox(BoxTitle: "Drive description", PromptText: lbl, DefaultResponse: cap) { Owner = this };
                    if (ib.ShowDialog() == true)
                    {
                        Kernel.Instance.KnownDrives.SpecifyVolumeDescription(lbl, ib.ResponseText);
                        RefreshListOfKnownDrives();
                    }
                }
            }
        }

        private void ForgetDriveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (RecognisedDrivesListBox.SelectedIndex == -1) { return; }
            if (RecognisedDrivesListBox.SelectedItem is ListBoxItem im)
            {
                if (im.Tag is int index)
                {
                    string lbl = Kernel.Instance.KnownDrives.RecognisedDrives[index].VolumeLabel;
                    MessageBoxResult answer = MessageBox.Show("Forget details of the selected drive?\n\n" + lbl + "\n\nIf the drive reappears in the list it is because it is referenced in a backup job.", "Forget drive details", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (answer == MessageBoxResult.Yes)
                    {
                        Kernel.Instance.KnownDrives.KillVolume(lbl);
                        RefreshListOfKnownDrives();
                    }
                }
            }
        }
}