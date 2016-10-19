using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
namespace Monitor
{
    class StepViewModel : INotifyPropertyChanged
    {
        ObservableCollection<StepDesc> stepDescs = new ObservableCollection<StepDesc>();
        int curIndex = -1;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public StepViewModel()
        {
            string sDataFolder = Helper.GetDataFolder();
            stepDescs.Add(new StepDesc("测量液面", new BitmapImage(new Uri(sDataFolder + "measure.png")),Stage.Measure));
            stepDescs.Add(new StepDesc("分装样品", new BitmapImage(new Uri(sDataFolder + "pipettings.jpg")),Stage.Pipetting));
            //stepDescs.Add(new StepDesc("核酸抽提", new BitmapImage(new Uri(sDataFolder + "DNA.jpg"))));
            curIndex = 0;
        }


        public ObservableCollection<StepDesc> StepsModel
        {
            get
            {
                return stepDescs;
            }

            set
            {
                stepDescs = value;
            }
        }
    }

    class StepDesc
    {
        string name;
        BitmapImage image;
        Stage correspondingStage;
        public StepDesc(string name, BitmapImage bmp,Stage stage)
        {
            this.name = name;
            this.image = bmp;
            correspondingStage = stage;
        }
        public Stage CorrespondingStage
        {
            get { return correspondingStage; }
            set { correspondingStage = value; }
        }
        public string Name 
        { 
            get { return name; } 
            set { name = value; }
        }
        public BitmapImage Image
        { 
            get { return image; } 
            set { image = value; }
        }
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            StepDesc anotherDesc = obj as StepDesc;
            if ((System.Object)anotherDesc == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (name == anotherDesc.name);
        }


        public static bool operator ==(StepDesc a, StepDesc b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(StepDesc a, StepDesc b)
        {
            return !(a == b);
        }

    }
}
