using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SolarQuotationBillingSystem.Models
{
    public partial class BillDeleteModel : ObservableObject
    {
        [ObservableProperty] private int invoiceID;
        [ObservableProperty] private string invoiceNo = string.Empty;
        [ObservableProperty] private string quotationNo = string.Empty;
        [ObservableProperty] private DateTime invoiceDate;
        [ObservableProperty] private string customerName = string.Empty;
        [ObservableProperty] private string mobile = string.Empty;
        [ObservableProperty] private decimal totalAmount;
        [ObservableProperty] private decimal paidAmount;
        [ObservableProperty] private decimal balanceAmount;
        [ObservableProperty] private string paymentStatus = "Pending";
        [ObservableProperty] private string createdBy = string.Empty;
    }
}
