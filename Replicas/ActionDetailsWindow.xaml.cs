using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Replicas;

internal partial class ActionDetailsWindow
{
    public ActionDetailsWindow()
    {
        InitializeComponent();
    }

    private readonly int _destinationRootLength;
    private readonly ReplicaJobTasks _tasks=new ();
    private readonly System.Collections.ObjectModel.ObservableCollection<ReplicaAction> _listedChanges=new ();

    internal ActionDetailsWindow(ReplicaJobTasks j, int destinationRootLength)
    {
        InitializeComponent();
        _tasks = j;
        _destinationRootLength = destinationRootLength;
        _listedChanges = new System.Collections.ObjectModel.ObservableCollection<ReplicaAction>();
        FontFamily ff = new FontFamily("Verdana");
        CboPreviewCategory.Items.Clear();
        int y = j.TaskCount("ER");
        if (y > 0)
        {
            ComboBoxItem cbi = new ComboBoxItem();
            TextBlock tbk = new TextBlock()
            {
                Text = string.Format(CultureInfo.CurrentCulture, "Tasks which encountered errors ({0})", y)
                , Foreground = Brushes.Red, FontFamily = ff, FontWeight = FontWeights.Medium
            };
            cbi.Tag = "ER";
            cbi.Content = tbk;
            CboPreviewCategory.Items.Add(cbi);
        }

        ComboBoxItem cbiz = new ComboBoxItem();
        TextBlock tbkz = new TextBlock()
        {
            Text = "Directories affected", Foreground = Brushes.DarkGreen, FontFamily = ff
            , FontWeight = FontWeights.Medium
        };
        cbiz.Tag = "XX";
        cbiz.Content = tbkz;
        CboPreviewCategory.Items.Add(cbiz);

        y = j.TaskCount("DA");
        if (y > 0)
        {
            ComboBoxItem cbi = new ComboBoxItem();
            TextBlock tbk = new TextBlock()
            {
                Text = string.Format(CultureInfo.CurrentCulture, "Directories to be added ({0})", y)
                , Foreground = Brushes.DarkGreen, FontFamily = ff, FontWeight = FontWeights.Medium
            };
            cbi.Tag = "DA";
            cbi.Content = tbk;
            CboPreviewCategory.Items.Add(cbi);
        }

        y = j.TaskCount("DD");
        if (y > 0)
        {
            ComboBoxItem cbi = new ComboBoxItem();
            TextBlock tbk = new TextBlock()
            {
                Text = string.Format(CultureInfo.CurrentCulture, "Directories to be deleted ({0})", y)
                , Foreground = Brushes.Maroon, FontFamily = ff, FontWeight = FontWeights.Medium
            };
            cbi.Tag = "DD";
            cbi.Content = tbk;
            CboPreviewCategory.Items.Add(cbi);
        }

        y = j.TaskCount("FA");
        if (y > 0)
        {
            ComboBoxItem cbi = new ComboBoxItem();
            TextBlock tbk = new TextBlock()
            {
                Text = string.Format(CultureInfo.CurrentCulture, "Files to be added ({0})", y)
                , Foreground = Brushes.DarkGreen, FontFamily = ff, FontWeight = FontWeights.Medium
            };
            cbi.Tag = "FA";
            cbi.Content = tbk;
            CboPreviewCategory.Items.Add(cbi);
        }

        y = j.TaskCount("FU");
        if (y > 0)
        {
            ComboBoxItem cbi = new ComboBoxItem();
            TextBlock tbk = new TextBlock()
            {
                Text = string.Format(CultureInfo.CurrentCulture, "Files to be updated ({0})", y)
                , Foreground = Brushes.DarkOliveGreen, FontFamily = ff, FontWeight = FontWeights.Medium
            };
            cbi.Tag = "FU";
            cbi.Content = tbk;
            CboPreviewCategory.Items.Add(cbi);
        }

        y = j.TaskCount("FD");
        if (y > 0)
        {
            ComboBoxItem cbi = new ComboBoxItem();
            TextBlock tbk = new TextBlock()
            {
                Text = string.Format(CultureInfo.CurrentCulture, "Files to be deleted ({0})", y)
                , Foreground = Brushes.Maroon, FontFamily = ff, FontWeight = FontWeights.Medium
            };
            cbi.Tag = "FD";
            cbi.Content = tbk;
            CboPreviewCategory.Items.Add(cbi);
        }

        if (CboPreviewCategory.Items.Count > 0)
        {
            CboPreviewCategory.SelectedIndex = 0;
        }

        LvwPreview.ItemsSource = _listedChanges;
    }

    private void CboPreviewCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CboPreviewCategory.SelectedItem is ComboBoxItem {Tag: string code, Content: TextBlock tbk})
        {
            LvwPreview.Foreground = tbk.Foreground;
            _listedChanges.Clear();
            if (code == "XX")
            {
                AffectedDirectoriesListBox.Visibility = Visibility.Visible;
                LvwPreview.Visibility = Visibility.Hidden;
                Dictionary<string, List<string>> qd = new Dictionary<string, List<string>>();
                foreach (ReplicaAction act in _tasks.AllTasks())
                {
                    string s = act.DestinationFolder.Substring(_destinationRootLength);
                    if (qd.ContainsKey(s))
                    {
                        if (!qd[s].Contains(act.ActionCode))
                        {
                            qd[s].Add(act.ActionCode);
                        }
                    }
                    else
                    {
                        List<string> u = new List<string>
                        {
                            act.ActionCode
                        };
                        qd.Add(s, u);
                    }
                }

                List<string> kl = new List<string>();
                foreach (string a in qd.Keys)
                {
                    kl.Add(a);
                }

                kl.Sort();

                foreach (string s in kl)
                {
                    TextBlock t = new TextBlock() {Text = s, Foreground = Brushes.SaddleBrown};
                    TextBlock tt = new TextBlock() {Foreground = Brushes.Blue};
                    string z = string.Empty;
                    foreach (string w in qd[s])
                    {
                        z += " " + w;
                    }

                    tt.Text = z;
                    StackPanel sp = new StackPanel() {Orientation = Orientation.Horizontal};
                    sp.Children.Add(t);
                    sp.Children.Add(tt);
                    ListBoxItem l = new ListBoxItem() {Content = sp};
                    AffectedDirectoriesListBox.Items.Add(l);
                }
            }
            else
            {
                AffectedDirectoriesListBox.Visibility = Visibility.Hidden;
                LvwPreview.Visibility = Visibility.Visible;
                foreach (ReplicaAction act in _tasks.TaskList(code))
                {
                    _listedChanges.Add(act);
                }
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}