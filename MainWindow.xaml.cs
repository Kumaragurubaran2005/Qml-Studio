using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using QML_Studio.pages;
using System;
using Windows.UI;

namespace QML_Studio
{
    public sealed partial class MainWindow : Window
    {
        private readonly SolidColorBrush _activeBgBrush = new SolidColorBrush(Color.FromArgb(255, 16, 22, 42)); // #10162A
        private readonly SolidColorBrush _transparentBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        private readonly SolidColorBrush _activeFgBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
        private readonly SolidColorBrush _inactiveFgBrush = new SolidColorBrush(Color.FromArgb(255, 158, 168, 192)); // #9EA8C0

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Set default active page
            SetActiveNavButton(DataUploadNavButton);
            ContentFrame.Navigate(typeof(DataUploadPage));
        }

        private void SetActiveNavButton(Button activeButton)
        {
            Button[] navButtons = { DataUploadNavButton, ModelSelectionNavButton, VisualizationNavButton, PredictionNavButton, ExperimentsNavButton };
            foreach (var btn in navButtons)
            {
                if (btn == activeButton)
                {
                    btn.Background = _activeBgBrush;
                    btn.Foreground = _activeFgBrush;
                }
                else
                {
                    btn.Background = _transparentBrush;
                    btn.Foreground = _inactiveFgBrush;
                }
            }
        }

        private void DataUploadNavButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(DataUploadNavButton);
            ContentFrame.Navigate(typeof(DataUploadPage));
        }

        private void ModelSelectionNavButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(ModelSelectionNavButton);
            ContentFrame.Navigate(typeof(ModelSelectionPage));
        }

        private void VisualizationNavButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(VisualizationNavButton);
            ContentFrame.Navigate(typeof(VisualizationPage));
        }

        private void PredictionNavButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(PredictionNavButton);
            ContentFrame.Navigate(typeof(PredictionPage));
        }

        private void ExperimentsNavButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveNavButton(ExperimentsNavButton);
            ContentFrame.Navigate(typeof(ExperimentsPage));
        }
    }
}

