﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace PlayerInterface {

    /// <summary>
    /// Interaction logic for ScreenOverlay.xaml
    /// </summary>
    public partial class ScreenOverlay : Window {
        private ScreenOverlayViewModel ViewModel => DataContext as ScreenOverlayViewModel;

        public int DefaultAutoHideTimeMs {
            get; set;
        }

        private long HideTimeStamp;

        public ScreenOverlay(int defaultAutoHideTimeMs) {
            InitializeComponent();
            DefaultAutoHideTimeMs = defaultAutoHideTimeMs;

            Width = SystemParameters.WorkArea.Width;
            DataContext = new ScreenOverlayViewModel();
        }

        public void DisplayText(string text) {
            DisplayText(text, DefaultAutoHideTimeMs);
        }

        public void DisplayText(string text, TimeSpan autoHideTime) {
            DisplayText(text, (int)autoHideTime.TotalMilliseconds);
        }

        public void DisplayText(string text, int autoHideTimeMs) {
            ViewModel.Text = text;
            if(!IsVisible) {
                Application.Current.Dispatcher.Invoke((() => Show()));
            }

            HideTimeStamp = Environment.TickCount + autoHideTimeMs;
            Task.Delay(autoHideTimeMs + 50).GetAwaiter().OnCompleted(() => {
                if(Environment.TickCount > HideTimeStamp && IsVisible) {
                    Application.Current.Dispatcher.Invoke((() => Hide()));
                }
            });
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e) {
            Hide();
        }
    }

    internal class ScreenOverlayViewModel : INotifyPropertyChanged {
        private string text;

        public string Text {
            get { return text; }
            set {
                if(value != text) {
                    text = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}