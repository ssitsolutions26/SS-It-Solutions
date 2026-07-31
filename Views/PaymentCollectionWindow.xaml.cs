using System.Windows;
using SolarQuotationBillingSystem.ViewModels;

namespace SolarQuotationBillingSystem.Views
{
    public partial class PaymentCollectionWindow : Window
    {
        public PaymentCollectionWindow(PaymentCollectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.OnClose += () => this.Close();
        }
    }
}
