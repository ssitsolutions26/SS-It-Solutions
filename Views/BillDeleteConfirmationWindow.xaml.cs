using System.Windows;
using System.Windows.Controls;
using SolarQuotationBillingSystem.ViewModels;

namespace SolarQuotationBillingSystem.Views
{
    public partial class BillDeleteConfirmationWindow : Window
    {
        public BillDeleteConfirmationWindow(BillDeleteConfirmationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.OnClose += (result) => 
            {
                this.DialogResult = result;
                this.Close();
            };
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is BillDeleteConfirmationViewModel vm)
            {
                vm.AdminPassword = ((PasswordBox)sender).Password;
            }
        }
    }
}
