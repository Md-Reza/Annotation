using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for UserEntry.xaml
    /// </summary>
    public partial class UserEntry : Window
    {
        public UserEntry()
        {
            InitializeComponent();
        }
        private void Submit_Click(object sender, RoutedEventArgs e)
        {


        }
        private void txtUserName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtUserName.Focus();
        }
        private void txtUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUserName.Text) && txtUserName.Text.Length > 0)
                textUserName.Visibility = Visibility.Collapsed;
            else txtUserName.Visibility = Visibility.Visible;
        }

        private void txtPassWord_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPassWord.Password) && txtPassWord.Password.Length > 0)
                textPassWord.Visibility = Visibility.Collapsed;
            else txtPassWord.Visibility = Visibility.Visible;
        }

        private void textPassWord_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassWord.Focus();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Success");
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
