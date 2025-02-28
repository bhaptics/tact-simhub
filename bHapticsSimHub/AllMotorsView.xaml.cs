using System;
using System.Collections.Generic;
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

namespace bHapticsSimHub
{
    /// <summary>
    /// Interaction logic for AllMotorsView.xaml
    /// </summary>
    public partial class AllMotorsView : UserControl
    {
        public AllMotorsView()
        {
            InitializeComponent();
        }

        private void Slider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (!(sender is Slider slider))
            {
                return;
            }

            //slider.Value;
        }

        private void Slider_ValueChanged_1(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider && DataContext is SimhubViewModel viewModel)
            {
                int index = Convert.ToInt32(slider.Tag);
                double intensity = e.NewValue;
                viewModel.SetProfileMotorIntensity(index, (int)intensity);
            }
        }
    }
}
