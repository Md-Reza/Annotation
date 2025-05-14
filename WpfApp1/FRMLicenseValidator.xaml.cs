using Microsoft.Win32;
using System;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.Base;
using WpfApp1.Model;
using WpfApp1.Services;

namespace WpfApp1
{
    public partial class FRMLicenseValidator : Window
    {
        ValidateViewModal validateViewModal = new ValidateViewModal();
        RegistryKey key;
        string keyPath = @"Software\LKey";
        string KeyName = "Key";
        string KeyStartDate = "StartDate";
        public FRMLicenseValidator()
        {
            InitializeComponent();
            Loaded += Test_Loaded;
        }

        private void Test_Loaded(object sender, RoutedEventArgs e)
        {
            txtMacAddress.Text = GetMac();
            txtLicenseInput.Focus();
        }

        private string GetMac()
        {
            var macAddress = NetworkInterface.GetAllNetworkInterfaces();
            return BitConverter.ToString(macAddress[0].GetPhysicalAddress().GetAddressBytes());
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void txtLicenseInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtLicenseInput.Text) && txtLicenseInput.Text.Length > 0)
                textLicenseInput.Visibility = Visibility.Collapsed;
            else txtLicenseInput.Visibility = Visibility.Visible;
        }

        private void textLicenseInput_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtLicenseInput.Focus();
        }

        private void textMacAddress_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtMacAddress.Focus();
        }

        private void txtMacAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtMacAddress.Text) && txtMacAddress.Text.Length > 0)
                textMacAddress.Visibility = Visibility.Collapsed;
            else txtMacAddress.Visibility = Visibility.Visible;
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
           await KeyValidateKey();
        }
        public async Task KeyValidateKey()
        {
            validateViewModal = (new ValidateViewModal
            {
                mac_id = txtMacAddress.Text.ToString(),
                key = txtLicenseInput.Text.ToString(),
            });

            if (validateViewModal is null)
            {
                MessageBox.Show("Please check your key.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (validateViewModal.key is null)
            {
                MessageBox.Show("Please check your key.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (validateViewModal.mac_id is null)
            {
                MessageBox.Show("Please check your mac address.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var res = await ValidateKey.GenerateKeyAsync(validateViewModal);
                if (res.IsSuccessStatusCode)
                {
                    var responceData = SDeserializer.Deserialize<ResponseViewModel>(res);
                    MessageBox.Show(responceData.data.message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    EnableWriteAndRewrite();
                    Application.Current.Shutdown();
                }
                else
                {
                    var errorResponseViewModel = SDeserializer.Deserialize<ErrorResponseViewModel>(res);
                    MessageBox.Show(errorResponseViewModel.message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void WriteRegistry()
        {
            try
            {
                // Prepare plain text
                string licenseText = txtLicenseInput.Text ?? string.Empty;
                //string expiryDateText = validate.ToString("O"); // ISO 8601 format for precision

                // Convert to bytes
                byte[] plainLicenseBytes = Encoding.UTF8.GetBytes(licenseText);
                //byte[] plainExpiryBytes = Encoding.UTF8.GetBytes(expiryDateText);

                // Encrypt data (machine/user bound)
                byte[] encryptedLicense = ProtectedData.Protect(plainLicenseBytes, null, DataProtectionScope.CurrentUser);
                //byte[] encryptedExpiry = ProtectedData.Protect(plainExpiryBytes, null, DataProtectionScope.CurrentUser);

                // Convert encrypted bytes to Base64
                string licenseBase64 = Convert.ToBase64String(encryptedLicense);
                //string expiryBase64 = Convert.ToBase64String(encryptedExpiry);

                // Write to registry
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (key == null)
                    {
                        MessageBox.Show("Failed to create or open the registry key.");
                        return;
                    }

                    key.SetValue(KeyName, licenseBase64, RegistryValueKind.String);
                    key.SetValue(KeyStartDate, DateTime.Now, RegistryValueKind.String);

                    // Get the current user identity
                    string currentUser = WindowsIdentity.GetCurrent().Name;

                    // Set security to deny write permissions
                    RegistrySecurity security = new RegistrySecurity();
                    security.AddAccessRule(new RegistryAccessRule(
                        currentUser,
                        RegistryRights.SetValue | RegistryRights.CreateSubKey | RegistryRights.Delete | RegistryRights.WriteKey,
                        InheritanceFlags.None,
                        PropagationFlags.None,
                        AccessControlType.Deny)); // Deny these rights

                    key.SetAccessControl(security);
                    key.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void EnableRegistry()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey))
            {
                if (key == null)
                    return;

                string currentUser = WindowsIdentity.GetCurrent().Name;

                RegistrySecurity security = key.GetAccessControl();

                RegistryAccessRule denyRule = new RegistryAccessRule(
                    currentUser,
                    RegistryRights.SetValue | RegistryRights.CreateSubKey | RegistryRights.Delete | RegistryRights.WriteKey,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Deny);

                security.RemoveAccessRule(denyRule);
                key.SetAccessControl(security);
            }
        }
        private void DisableRegistry()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey))
            {
                if (key == null)
                {
                    MessageBox.Show("Registry key not found.");
                    return;
                }

                string currentUser = WindowsIdentity.GetCurrent().Name;

                RegistrySecurity security = new RegistrySecurity();
                security.AddAccessRule(new RegistryAccessRule(
                    currentUser,
                    RegistryRights.SetValue | RegistryRights.CreateSubKey | RegistryRights.Delete | RegistryRights.WriteKey,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Deny)); // Deny these rights

                key.SetAccessControl(security);
            }
        }
        public void EnableWriteAndRewrite()
        {
            try
            {
                // Step 1: Enable (remove deny rule)
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey))
                {
                    if (key == null) {
                        WriteRegistry();
                        DisableRegistry();
                    }
                    else
                    {
                        EnableRegistry();
                        WriteRegistry();
                        DisableRegistry();
                    }   
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

    }
}
