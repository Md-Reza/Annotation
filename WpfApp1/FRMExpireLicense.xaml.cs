using System.Windows;

namespace WpfApp1
{
    public partial class FRMExpireLicense : Window
    {
        public FRMExpireLicense()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void LicenseKeyHyperlink_Click(object sender, RoutedEventArgs e)
        {
            FRMLicenseValidator validateLicense = new FRMLicenseValidator();
            validateLicense.Show();
        }
    }
}
