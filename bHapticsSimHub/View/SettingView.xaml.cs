using System.Windows.Controls;

namespace bHapticsSimHub
{
    /// <summary>
    /// Interaction logic for SettingView.xaml
    /// </summary>
    public partial class SettingView : UserControl
    {
        private readonly bHapticsSimHub Plugin;

        public SettingView(bHapticsSimHub plugin)
        {
            InitializeComponent();
            this.Plugin = plugin;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            profileList.IsDropDownOpen = false;
        }
    }
}
