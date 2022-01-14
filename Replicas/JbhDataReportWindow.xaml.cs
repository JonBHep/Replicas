using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Replicas;

public partial class JbhDataReportWindow
{
    public JbhDataReportWindow()
    {
        InitializeComponent();
    }
    readonly List<string> _infoList=new List<string>();
    private readonly List<string> _businessList=new List<string>();
        private readonly FontFamily _myFamily = new FontFamily("Verdana");
        private readonly double _mySize = 14;
        private readonly Thickness _myThick = new Thickness(8, 3, 8, 3);

        public JbhDataReportWindow(List<string> infoDates, List<string> businessDates)
        {
            InitializeComponent();
            _infoList = infoDates;
            _businessList = businessDates;
            TextBlockHeading.FontFamily = _myFamily;
            TextBlockHeading.FontSize = _mySize;
            TextBlockRubric.FontFamily = _myFamily;
            TextBlockRubric.FontSize = _mySize;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

            foreach (string s in _infoList)
            {
                string[] part = s.Split("^".ToCharArray());

                StackPanel spnl = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                TextBlock tbkDate = new TextBlock()
                {
                    Text = part[0],
                    FontFamily = _myFamily,
                    FontSize = _mySize,
                    Foreground = Brushes.Peru,
                    Padding = _myThick,
                    Width = 200
                };
                spnl.Children.Add(tbkDate);

                TextBlock tbkWhen = new TextBlock()
                {
                    Text = part[2],
                    FontFamily = _myFamily,
                    FontSize = _mySize,
                    Foreground = Brushes.SandyBrown,
                    Padding = _myThick,
                    Width = 120
                };
                spnl.Children.Add(tbkWhen);

                TextBlock tbkName = new TextBlock()
                {
                    Text = part[1],
                    FontFamily = _myFamily,
                    FontSize = _mySize,
                    Foreground = Brushes.Sienna,
                    Padding = _myThick
                };
                spnl.Children.Add(tbkName);

                InfoItemsControl.Items.Add(spnl);
            }

            foreach (string s in _businessList)
            {
                string[] part = s.Split("^".ToCharArray());

                StackPanel spnl = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                TextBlock tbkDate = new TextBlock()
                {
                    Text = part[0],
                    FontFamily = _myFamily,
                    FontSize = _mySize,
                    Foreground = Brushes.Peru,
                    Padding = _myThick,
                    Width = 200
                };
                spnl.Children.Add(tbkDate);

                TextBlock tbkWhen = new TextBlock()
                {
                    Text = part[2],
                    FontFamily = _myFamily,
                    FontSize = _mySize,
                    Foreground = Brushes.SandyBrown,
                    Padding = _myThick,
                    Width = 120
                };
                spnl.Children.Add(tbkWhen);

                TextBlock tbkName = new TextBlock()
                {
                    Text = part[1],
                    FontFamily = _myFamily,
                    FontSize = _mySize,
                    Foreground = Brushes.Sienna,
                    Padding = _myThick
                };
                spnl.Children.Add(tbkName);

                BusinessItemsControl.Items.Add(spnl);
            }

            DisplayThermometers();
        }

        private void DisplayThermometers()
        {
            double wid = InfoLabel.ActualWidth;
            if (BusinessLabel.ActualWidth > wid) { wid = BusinessLabel.ActualWidth; }
            wid += 12;
            InfoLabel.Width = BusinessLabel.Width = wid;
            double chartWidth = InfoPanel.ActualWidth - wid;
            chartWidth -= 24;

            List<TimeSpan> itimes = Kernel.Instance.JobProfiles.JbhInfoAgos();
            TimeSpan most = itimes.Max();

            List<TimeSpan> differences = new List<TimeSpan>();
            TimeSpan previous = TimeSpan.Zero;
            foreach (TimeSpan t in itimes)
            {
                TimeSpan diff = t - previous;
                differences.Add(diff);
                previous = t;
            }

            bool swapper = false;
            foreach (TimeSpan t in differences)
            {
                double chunkWidth = chartWidth * t.Ticks / most.Ticks;
                InfoPanel.Children.Add(new Rectangle() { Width = chunkWidth, Height = 6, Fill = swapper ? Brushes.Peru : Brushes.LightYellow });
                swapper = !swapper;
            }

            List<TimeSpan> btimes = Kernel.Instance.JobProfiles.JbhBusinessAgos();
            most = btimes.Max();

            differences = new List<TimeSpan>();
            previous = TimeSpan.Zero;
            foreach (TimeSpan t in btimes)
            {
                TimeSpan diff = t - previous;
                differences.Add(diff);
                previous = t;
            }

            swapper = false;
            foreach (TimeSpan t in differences)
            {
                double chunkWidth = chartWidth * t.Ticks / most.Ticks;
                BusinessPanel.Children.Add(new Rectangle() { Width = chunkWidth, Height = 6, Fill = swapper ? Brushes.Peru : Brushes.LightYellow });
                swapper = !swapper;
            }
        }
}