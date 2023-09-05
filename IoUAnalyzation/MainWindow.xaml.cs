using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using System.IO;
using Json.Net;
using Newtonsoft.Json;
using System.Dynamic;
using Rectangle = System.Drawing.Rectangle;
using Emgu.CV.Util;
using System.Drawing;
using System.ComponentModel;

namespace IoUAnalyzation
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public double Threshold { get; set; } = 0.005;
        public int WrongCount { get; set; }
        public int TotalCount { get; set; }
        public List<DetectionResult> Results {  get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();            
        }       

        public void IoUCalculation()
        {
            Results = new List<DetectionResult>();
            var toolPath = "C:\\Users\\User\\Desktop\\IoU\\DuckEgg_20230901_9_MarkTogether_Rotation\\Instance Segmentation4 Tool1";

            var detectionFilePath = toolPath + "\\DetectImages\\Validation\\Detection\\";
            var detections = Directory.GetFiles(detectionFilePath, "*.json").ToList();

            var annotateFilePath = toolPath + "\\DetectImages\\SegmentImgs\\";
            var annotations = Directory.GetFiles(annotateFilePath, "*.txt").Where(x => !x.Contains("keypoints")).ToList();


            for (int i=0; i< 8; i++)
            {
                var rectangles = GetDetectionRect(detections[i]);


                var annotation = annotations.Where(x => x.Contains(detections[i].Split('\\').LastOrDefault().Split('.')[0])).ToList();

                var points = GetAnnotationPoints(annotation);
                var annotationArea = CvInvoke.ContourArea(new VectorOfPointF(points.ToArray()));
                //var annotationRect = GetAnnotationRect(points);
                //var annotationArea = annotationRect.Width * annotationRect.Height;

                foreach (var rect in rectangles)
                {
                    double intersectionArea = 0;
                    var intersection = new VectorOfPointF();
                    var rectPoints = GetRectPoints(rect);

                    //CvInvoke.IntersectConvexConvex(new VectorOfPointF(points.ToArray()), new VectorOfPointF(rectPoints.ToArray()), intersection);
                    //if (intersection.Length != 0)
                    //{
                    //    intersectionArea = CvInvoke.ContourArea(intersection);
                    //}

                    foreach (var point in points)
                    {
                        if (point.X >= rect.Rectangle.Left && point.X <= rect.Rectangle.Right &&
                            point.Y >= rect.Rectangle.Top && point.Y <= rect.Rectangle.Bottom)
                        {
                            intersectionArea += 1;
                        }
                    }

                    //var intersectionRect = Rectangle.Intersect(rect.Rectangle, annotationRect);
                    //intersectionArea = intersectionRect.Width * intersectionRect.Height;

                    var detectionArea = rect.Rectangle.Width * rect.Rectangle.Height;

                    var union = annotationArea + detectionArea - intersectionArea;
                    Results.Add(new DetectionResult()
                    {
                        Score = rect.Score,
                        IoU = intersectionArea / union,
                        Result = (intersectionArea / union) > Threshold ? "Real Target" : " "
                    });
                }
            }
            
            WrongCount = Results.Where(x=>x.IoU < Threshold).Count();
            TotalCount = Results.Count;
        }

        private List<PointF> GetRectPoints(RectangleResult rect)
        {
            var rectPoints = new List<PointF>();
            for (int x = rect.Rectangle.Left; x <= rect.Rectangle.Right; x++)
            {
                rectPoints.Add(new PointF(x, rect.Rectangle.Top));
                rectPoints.Add(new PointF(x, rect.Rectangle.Bottom));
            }
            for (int y = rect.Rectangle.Top; y <= rect.Rectangle.Bottom; y++)
            {
                rectPoints.Add(new PointF(y, rect.Rectangle.Left));
                rectPoints.Add(new PointF(y, rect.Rectangle.Right));
            }
            return rectPoints;
        }

        private List<PointF> GetAnnotationPoints(List<string> filePaths)
        {
            var points = new List<PointF>();
            foreach (var filePath in filePaths)
            {
                var lines = File.ReadLines(filePath);

                foreach (var line in lines)
                {
                    if (line != "#start" && line != "#end")
                    {
                        PointF point = new PointF();
                        point.X = float.Parse(line.Split(',')[0]);
                        point.Y = float.Parse(line.Split(',')[1]);

                        points.Add(point);
                    }
                };
            }    
            return points;
        }


        private List<RectangleResult> GetDetectionRect(string filePath)
        {
           
            using (var sr = new StreamReader(filePath))
            {
                var json = sr.ReadToEnd();
                dynamic rectangle = JsonConvert.DeserializeObject<ExpandoObject>(json);
                var rectangleResults = new List<RectangleResult>();

                var vertices = new System.Drawing.Point[4];
                for (int i = 0; i < rectangle.annotations.Count; i++)
                {
                    rectangleResults.Add(new RectangleResult(){
                        Score = Math.Round(rectangle.annotations[i].score, 2),
                        
                        Rectangle = new Rectangle()
                        {
                            X = (int)rectangle.annotations[i].bbox[0],
                            Y = (int)rectangle.annotations[i].bbox[1],
                            Width = (int)(rectangle.annotations[i].bbox[2] - rectangle.annotations[i].bbox[0]),
                            Height = (int)(rectangle.annotations[i].bbox[3] - rectangle.annotations[i].bbox[1]),
                        }
                    });
                }
                return rectangleResults;
            }          
        }

        private Rectangle GetAnnotationRect(List<PointF> annotationPoints)
        {
            var annotationRect = new Rectangle()
            {
                X = (int)annotationPoints.Min(p => p.X),
                Y = (int)annotationPoints.Min(p => p.Y),
                Width = (int)annotationPoints.Max(p => p.X) - (int)annotationPoints.Min(p => p.X),
                Height = (int)annotationPoints.Max(p => p.Y) - (int)annotationPoints.Min(p => p.Y),
            };
            return annotationRect;
        }

        private void Calculate_OnClick(object sender, RoutedEventArgs e)
        {
            IoUCalculation();
        }
    }
}
