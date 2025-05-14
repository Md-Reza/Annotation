using Microsoft.Win32;
using System;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace WpfApp1.Utilitys
{
    public sealed class SystemRegistry
    {
        string keyPath = @"Software\LKey";
        string KeyName = "Key";
        string KeyStartDate = "StartDate";
        private void WriteRegistry( string licenseKey)
        {
            try
            {
                // Prepare plain text
                string licenseText = licenseKey ?? string.Empty;
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

        public void EnableWriteAndRewrite(string licenseKey)
        {
            try
            {
                // Step 1: Enable (remove deny rule)
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey))
                {
                    if (key == null)
                    {
                        WriteRegistry(licenseKey);
                        DisableRegistry();
                    }
                    else
                    {
                        EnableRegistry();
                        WriteRegistry(licenseKey);
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
