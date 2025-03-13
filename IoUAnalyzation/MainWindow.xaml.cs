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
using Microsoft.Win32;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Windows.Markup;
using System.Collections.ObjectModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Annotations;

namespace IoUAnalyzation
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private int myMissingCount { get; set; }
        private int myWrongCount { get; set; }
        public string ToolPath{ get; set; }
        public string ClassName { get; set; } = "NG";
        public double Threshold { get; set; } = 0.005;
        public double ScoreThreshold { get; set; } = 0.5;
        public int WrongCount { get; set; }
        public int MissingCount { get; set; }
        public int TotalCount { get; set; }
        public bool HideNg{ get; set; }
        public bool HideOk{ get; set; }
        private void OnHideNgChanged()
        {
            if (HideNg)
            {
                if(Results != null && Results.Count > 0)
                {
                    IoUCalculation();
                    var result = Results.Where(x => x.WrongCount == 0 && x.MissingCount == 0);
                    Results = new ObservableCollection<DisplayResult>(result);
                }
            }
            else
            {
                IoUCalculation();
            }
        }
        private void OnHideOkChanged()
        {
            if (HideOk)
            {
                if (Results != null && Results.Count > 0)
                {
                    IoUCalculation(); 
                    var result = Results.Where(x => x.WrongCount > 0 || x.MissingCount > 0);
                    Results = new ObservableCollection<DisplayResult>(result);
                }
            }
            else
            {
                IoUCalculation();
            }
        }
        public ObservableCollection<DisplayResult> Results {  get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();            
        }       

        public void IoUCalculation()
        {
            Results = new ObservableCollection<DisplayResult>();
            //ToolPath = "C:\\Users\\User\\Desktop\\IoU\\DuckEgg_20230901_9_MarkTogether_Rotation\\Instance Segmentation4 Tool1";

            if (ToolPath == null)
            {
                return;
            }

            try
            {
                //var detectionFilePath = ToolPath + "\\DetectImages\\Validation\\Detection\\";
                var detectionFilePath = ToolPath + "\\Images\\Validation\\ValidationGroup\\Detection\\";
                var detections = Directory.GetFiles(detectionFilePath, "*.json").ToList();
                var annotateFilePath = ToolPath + "\\Images\\";
                var annotationFiles = Directory.GetFiles(annotateFilePath, "*.json").ToList();

                for (int i = 0; i < detections.Count; i++)
                {
                    myWrongCount = 0;
                    myMissingCount = 0;
                    var rectangles = GetDetectionRect(detections[i]);

                    var annotationFile = annotationFiles.Where(x => x.Contains(detections[i].Split('\\').LastOrDefault().Split('.')[0])).FirstOrDefault();

                    var annotations = GetAnnotationPoints(annotationFile);

                    CountingOfWrong(rectangles, annotations);
                    CountingOfMiss(rectangles, annotations);

                    Results.Add(new DisplayResult()
                    {
                        ImageName = System.IO.Path.GetFileNameWithoutExtension(annotationFile),
                        WrongCount = myWrongCount,
                        MissingCount = myMissingCount,
                        TotalCount = rectangles.Count
                    });
                };

                WrongCount = Results.Where(x=>x.WrongCount > 0).Count();
                MissingCount = Results.Where(x=>x.MissingCount > 0).Count();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
          
        }

        private void CountingOfWrong(List<DetectionResult> rectangles, List<List<PointF>> annotations)
        {
            for (int rect = 0; rect < rectangles.Count; rect++)
            {
                double intersectionArea = 0;
                var intersection = new VectorOfPointF();
                var rectPoints = GetRectPoints(rectangles[rect]);
                bool intersectObject;

                foreach (var annotation in annotations)
                {
                    double annotationArea = 0;
                    if (annotation.Count > 0)
                    {
                        annotationArea = CvInvoke.ContourArea(new VectorOfPointF(annotation.ToArray()));
                    }

                    //var annotationRect = GetAnnotationRect(annotation);
                    //var annotationArea = annotationRect.Width * annotationRect.Height;

                    ////CvInvoke.IntersectConvexConvex(new VectorOfPointF(annotation.ToArray()), new VectorOfPointF(rectPoints.ToArray()), intersection);
                    //if (intersection.Length != 0)
                    //{
                    //    intersectionArea = CvInvoke.ContourArea(intersection);
                    //}

                    intersectionArea = GetIntersectionArea(rectangles, annotation, intersectionArea, rect);

                    //var intersectionRect = Rectangle.Intersect(rect.Rectangle, annotationRect);
                    //intersectionArea = intersectionRect.Width * intersectionRect.Height;

                    var detectionArea = rectangles[rect].Rectangle.Width * rectangles[rect].Rectangle.Height;

                    var union = annotationArea + detectionArea - intersectionArea;

                    if ((intersectionArea / union) >= Threshold)
                    {
                        intersectObject = true;
                        break;
                    }

                    if (annotation == annotations.LastOrDefault())
                    {
                        myWrongCount += 1;
                    }
                }


                if (annotations.Count == 0)
                {
                    myWrongCount += rectangles.Count;
                }

            }
        }

        private void CountingOfMiss(List<DetectionResult> rectangles, List<List<PointF>> annotations)
        {
            foreach (var annotation in annotations)         
            {
                double intersectionArea = 0;             
                var intersectObject = 0;
                double annotationArea = 0;

                if (annotation.Count > 0)
                {
                    annotationArea = CvInvoke.ContourArea(new VectorOfPointF(annotation.ToArray()));
                }

                bool targetFound = false; 

                for (int rect = 0; rect < rectangles.Count; rect++)
                {
                    intersectionArea = GetIntersectionArea(rectangles, annotation, intersectionArea, rect);

                    var detectionArea = rectangles[rect].Rectangle.Width * rectangles[rect].Rectangle.Height;

                    var union = annotationArea + detectionArea - intersectionArea;

                    if ((intersectionArea / union) > Threshold)
                    {
                        intersectObject += 1;
                    }

                    if (annotation.Count > 0)
                    {
                        if (intersectObject != 0)
                        {
                            targetFound = true;
                        }
                    }
                }

                if (!targetFound)
                {
                    myMissingCount += 1;
                }
            }
        }

        private static double GetIntersectionArea(List<DetectionResult> rectangles, List<PointF> annotation, double intersectionArea, int rect)
        {
            foreach (var point in annotation)
            {
                if (point.X >= rectangles[rect].Rectangle.Left && point.X <= rectangles[rect].Rectangle.Right &&
                    point.Y >= rectangles[rect].Rectangle.Top && point.Y <= rectangles[rect].Rectangle.Bottom)
                {
                    intersectionArea += 1;
                }
            }

            return intersectionArea;
        }

        private List<PointF> GetRectPoints(DetectionResult rect)
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

        private List<List<PointF>> GetAnnotationPoints(string filePath)
        {

            var annotations = new List<List<PointF>>();

            if (filePath != null)
            {
                using (var sr = new StreamReader(filePath))
                {
                    var json = sr.ReadToEnd();
                    dynamic expandoObject = JsonConvert.DeserializeObject<ExpandoObject>(json);


                    foreach (var classifyObject in expandoObject.Objects)
                    {
                        var classObject = (IDictionary<string, object>)classifyObject.Class;
                        var className = classObject["$ref"].ToString().Trim('#');
                        if (className == ClassName)
                        {
                            var points = new List<PointF>();
                            foreach (var layer in classifyObject.Layers)
                            {
                                if (((IDictionary<string, object>)layer.Shape).ContainsKey("Points"))
                                {
                                    foreach (var point in layer.Shape.Points)
                                    {
                                        //Console.WriteLine(float.Parse(point.Split(',')[0]));
                                        points.Add(new PointF()
                                        {
                                            X = float.Parse(point.Split(',')[0]),
                                            Y = float.Parse(point.Split(',')[1])
                                        }); ;
                                    }
                                }
                            };
                            annotations.Add(points);
                        }
                    }

                }
            }
            return annotations;
        }


        private List<DetectionResult> GetDetectionRect(string filePath)
        {
           
            using (var sr = new StreamReader(filePath))
            {
                var json = sr.ReadToEnd();
                dynamic rectangle = JsonConvert.DeserializeObject<ExpandoObject>(json);
                var rectangleResults = new List<DetectionResult>();

                var vertices = new System.Drawing.Point[4];
                for (int i = 0; i < rectangle.annotations.Count; i++)
                {
                    var score = Math.Round(rectangle.annotations[i].score, 2);
                    var classId = (int)rectangle.annotations[i].category_id;
                    var className = rectangle.categories[classId-1].name.ToString();
                    if (score > ScoreThreshold && className == ClassName)
                    {
                        rectangleResults.Add(new DetectionResult()
                        {
                            Score = score,

                            Rectangle = new Rectangle()
                            {
                                X = (int)rectangle.annotations[i].bbox[0],
                                Y = (int)rectangle.annotations[i].bbox[1],
                                Width = (int)(rectangle.annotations[i].bbox[2] - rectangle.annotations[i].bbox[0]),
                                Height = (int)(rectangle.annotations[i].bbox[3] - rectangle.annotations[i].bbox[1]),
                            }
                        });
                    }
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
            Reset();
            IoUCalculation();
        }

        private void SelectTool_OnClick(object sender, RoutedEventArgs e)
        {
            var browser =  new CommonOpenFileDialog() { IsFolderPicker = true};
            if (browser.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ToolPath = browser.FileName;
            }            
        }
        private void Reset()
        {
            HideNg = false;
            HideOk = false;
        }
    }
}
