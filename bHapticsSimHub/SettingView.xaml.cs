using SimHub.Plugins;
using SimHub.Plugins.UI;
using System;
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

        
    }
}
