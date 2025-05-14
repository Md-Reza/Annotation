using Microsoft.Win32;
using System;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        string keyPath = @"Software\LKey";
        string KeyName = "Key";
        string KeyStartDate = "StartDate";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var key = ReadDecryptedValue(keyPath, KeyName);

            if (CheckRegistryForKey())
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                //var main = new MainWindow();
                //main.Hide();
            }
            else if (key == null)
            {
                FRMLicenseValidator licenseValidator = new FRMLicenseValidator();
                licenseValidator.Show();
            }
            else
            {
                FRMExpireLicense validateWindow = new FRMExpireLicense();
                validateWindow.Show();
            }
        }
        public DateTime ExtractExpiryDate(string formattedLicenseKey)
        {
            // Step 1: Remove all hyphens
            string cleaned = formattedLicenseKey.Replace("-", "");

            // Step 2: Split on dot
            string[] parts = cleaned.Split('.');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid license key format.");

            string base64Data = parts[0];

            // Step 3: Add padding if needed
            int mod4 = base64Data.Length % 4;
            if (mod4 > 0)
                base64Data = base64Data.PadRight(base64Data.Length + (4 - mod4), '=');

            // Step 4: Decode base64 data
            byte[] combined = Convert.FromBase64String(base64Data);

            if (combined.Length < 16)
                throw new ArgumentException("Corrupted license key.");

            // Step 5: Read 8 bytes from offset 8
            long dateBinary = BitConverter.ToInt64(combined, 8);
            return DateTime.FromBinary(dateBinary);
        }

        private bool CheckRegistryForKey()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    EnableWriteAndRewrite();
                    DateTime startDate;
                    object value1 = key.GetValue(KeyName);
                    var startDateString = key.GetValue(KeyStartDate) as string;
                    if (startDateString != null) {   
                        startDate = DateTime.Parse(startDateString).Date;
                        var keyValue = ReadDecryptedValue(keyPath, KeyName);
                        DateTime expiryDate = ExtractExpiryDate(keyValue).Date;

                        if (startDate < expiryDate)
                            return true;
                    }  
                }
            }

            return false;
        }

        private static string ReadDecryptedValue(string path, string name)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path))
            {
                if (key == null)
                    return null;

                string encryptedBase64 = key.GetValue(name)?.ToString();

                if (string.IsNullOrEmpty(encryptedBase64))
                    throw new Exception("Registry value not found or empty.");

                byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

                // Decrypt
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(plainBytes);
            }
        }

        private void WriteRegistry()
        {
            try
            {
                // Write to registry
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (key == null)
                    {
                        MessageBox.Show("Failed to create or open the registry key.");
                        return;
                    }
                    key.SetValue(KeyStartDate, DateTime.Now.ToString("O"), RegistryValueKind.String);

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
                    if (key == null)
                    {
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
