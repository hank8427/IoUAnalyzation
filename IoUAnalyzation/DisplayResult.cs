using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace IoUAnalyzation
{
    public class DisplayResult
    {
        public string ImageName { get; set; }
        public int WrongCount { get; set; }
        public int MissingCount { get; set; }
        public int DetectCount { get; set; }
        public int AnnotationCount { get; set; }
    }
}
