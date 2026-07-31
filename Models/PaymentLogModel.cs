using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SolarQuotationBillingSystem.Models
{
    public partial class PaymentLogModel : ObservableObject
    {
        [ObservableProperty] private int paymentID;
        [ObservableProperty] private string invoiceNo = string.Empty;
        [ObservableProperty] private DateTime paymentDate;
        [ObservableProperty] private decimal paidAmount;
        [ObservableProperty] private string paymentMode = string.Empty;
        [ObservableProperty] private string referenceNo = string.Empty;
        [ObservableProperty] private string remarks = string.Empty;
    }
}
