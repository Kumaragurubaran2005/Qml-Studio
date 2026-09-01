using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace QML_Studio.pages
{
    public sealed partial class ExperimentsPage : Page
    {
        public ExperimentsPage()
        {
            this.InitializeComponent();
        }

        private void NewExperimentButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame?.Navigate(typeof(ModelSelectionPage));
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e) { }
        private void FilterCompleted_Click(object sender, RoutedEventArgs e) { }
        private void FilterRunning_Click(object sender, RoutedEventArgs e) { }
    }
}
