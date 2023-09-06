using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoUAnalyzation
{
    public class DetectionResult
    {
        public string ImageName { get; set; }
        public double Score {  get; set; }
        public double IoU { get; set; }
        public string Result { get; set; }
        public Rectangle Rectangle { get; set; }
    }
}
