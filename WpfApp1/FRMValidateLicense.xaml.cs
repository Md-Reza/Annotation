using Microsoft.Win32;
using System;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Base;
using WpfApp1.Model;
using WpfApp1.Services;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for FRMValidateLicense.xaml
    /// </summary>
    public partial class FRMValidateLicense : Window
    {
        string keyPath = @"Software\LKey";
        string KeyName = "LKey";
        string KeyName1 = "KeySDate";
        string KeyName2 = "KeyEDate";
        string valueToWrite = "LKey";
        ValidateViewModal validateViewModal = new ValidateViewModal();
        RegistryKey key;
        public FRMValidateLicense()
        {
            InitializeComponent();
            Loaded += FRMValidateLicense_Loaded;
        }

        private void FRMValidateLicense_Loaded(object sender, RoutedEventArgs e)
        {
            txtMacAddress.Text = GetMac();
        }
        private string GetMac()
        {
            var macAddress = NetworkInterface.GetAllNetworkInterfaces();
            return BitConverter.ToString(macAddress[0].GetPhysicalAddress().GetAddressBytes());
        }
        private void WriteRegistry(DateTime validate)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(txtLicenseInput.Text.ToString());

            // Encrypt data (machine-bound)
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

            // Convert to base64 string to store in registry
            string encryptedBase64 = Convert.ToBase64String(encryptedBytes);

            using (key = Registry.CurrentUser.CreateSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                if (key != null)
                {
                    key.SetValue(KeyName, encryptedBase64, RegistryValueKind.String);
                    key.SetValue(KeyName1, validate);
                    //MakeKeyReadOnly(key);
                }
                else
                {
                    Console.WriteLine("Failed to create or open the registry key.");
                }
            }
        }
        private void MakeKeyReadOnly(RegistryKey key)
        {
            try
            {
                RegistrySecurity security = key.GetAccessControl();

                // Protect the key itself, but do not apply to subkeys
                security.SetAccessRuleProtection(true, true);

                SecurityIdentifier users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

                // Deny write permissions
                RegistryAccessRule denyWrite = new RegistryAccessRule(
                    users,
                    RegistryRights.SetValue | RegistryRights.CreateSubKey | RegistryRights.Delete | RegistryRights.WriteKey,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Deny);

                // Allow read permissions
                RegistryAccessRule allowRead = new RegistryAccessRule(
                    users,
                    RegistryRights.ReadKey,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                security.AddAccessRule(denyWrite);
                security.AddAccessRule(allowRead);

                key.SetAccessControl(security);

                Console.WriteLine($"🔒 Registry key '{key.Name}' is now read-only.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error setting key read-only: {ex.Message}");
            }
        }


        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            MakeKeyDeletable(key);
        }
        private void MakeKeyDeletable(RegistryKey key)
        {
            try
            {
                RegistrySecurity security = key.GetAccessControl();

                SecurityIdentifier users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

                // Remove the deny write permissions
                RegistryAccessRule denyWrite = new RegistryAccessRule(
                    users,
                    RegistryRights.SetValue | RegistryRights.CreateSubKey | RegistryRights.Delete | RegistryRights.WriteKey,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Deny);

                bool modified;
                security.RemoveAccessRuleSpecific(denyWrite);

                // Optionally: remove protection too, if needed
                security.SetAccessRuleProtection(false, false);

                key.SetAccessControl(security);

                Console.WriteLine($"🔓 Permissions reset. You can now delete '{key.Name}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error resetting permissions: {ex.Message}");
            }
        }
        private void BtnWrite_Click(object sender, RoutedEventArgs e)
        {
            //WriteRegistry();
        }
        private async void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            await KeyValidateKey();
            //GetRegistry();
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
                    WriteRegistry(responceData.data.valid);
                    Application.Current.Shutdown();
                    return;
                }
                else
                    MessageBox.Show(res.RequestMessage.ToString(), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString(), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
