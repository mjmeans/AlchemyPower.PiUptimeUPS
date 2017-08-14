using Windows.UI.Xaml.Controls;

namespace PiUptimeUPS_Sample2
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.ViewModel = new MainPageViewModel();
            this.InitializeComponent();
        }

        public MainPageViewModel ViewModel { get; set; }
    }
}
