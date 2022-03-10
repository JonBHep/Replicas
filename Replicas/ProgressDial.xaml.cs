using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Replicas;

public partial class ProgressDial
{
    public ProgressDial()
        {
            InitializeComponent();
            _polyDialFace.Fill = Brushes.Ivory;
            DrawPolygon();
            _polyProgress.Stroke = Brushes.Tan;
            _polyProgress.StrokeThickness = 0.5;
            _polyProgress.Fill = Brushes.Tan;
        }

        private double _radius = 24;
        private readonly Polygon _polyProgress = new Polygon();
        private readonly Polygon _polyDialFace = new Polygon();
        private readonly Point[] _percentPoints = new Point[101];

        private void DrawPolygon()
        {
            // Calculate the centre and perimeter points
            for (int x = 0; x <= 100; x++)
            {
                double angle = Math.PI * 2 * x / 100;
                double xpt = _radius + (_radius * Math.Sin(angle));
                double ypt = _radius - (_radius * Math.Cos(angle));
                Point p = new Point(xpt, ypt);
                _percentPoints[x] = p;
            }

            _polyDialFace.Points.Clear();
            for (int x = 1; x <= 100; x++)
            {
                _polyDialFace.Points.Add(_percentPoints[x]);
            }
            _polyDialFace.Stroke = Brushes.Tan;
            _polyDialFace.StrokeThickness = 0.5;
            
            CanvasDial.Children.Add(_polyDialFace);

            DrawSecondPolygon(0);
            CanvasDial.Children.Add(_polyProgress);
        }

        private void DrawSecondPolygon(int segments)
        {
            _polyProgress.Points.Clear();
            if (segments == 0) { return; }
            _polyProgress.Points.Add(new Point(_radius, _radius)); // centre of clock face
            for (int x = 0; x <= segments; x++)
            {
                _polyProgress.Points.Add(_percentPoints[x]);
            }
            
        }

        private void UpdateProgressDisplay(int percent)
        {
            _polyDialFace.ToolTip = _polyProgress.ToolTip = CanvasDial.ToolTip = $"{percent}%";
            DrawSecondPolygon(percent);
        }

        public void SetPercentage(int w)
        {
            UpdateProgressDisplay(w);
        }

        public new Brush Foreground
        {
            set
            {
                _polyProgress.Stroke = value;
                _polyProgress.Fill = value;
                _polyDialFace.Stroke = value;
            }
        }

        public new Brush Background
        {
            set => _polyDialFace.Fill = value;
        }
}