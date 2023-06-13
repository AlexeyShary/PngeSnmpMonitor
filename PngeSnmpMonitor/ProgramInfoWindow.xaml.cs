using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PngeSnmpMonitor
{
    public partial class ProgramInfoWindow : Window
    {
        private bool isClosing;

        public ProgramInfoWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Owner = null;

            if (!isClosing)
                Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            isClosing = true;
        }
    }
}