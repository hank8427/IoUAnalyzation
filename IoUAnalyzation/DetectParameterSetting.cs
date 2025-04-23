using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoUAnalyzation
{
    public class DetectParameterSetting : INotifyPropertyChanged
    {
        public string ClassName { get; set; }
        public double ScoreThreshold { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
