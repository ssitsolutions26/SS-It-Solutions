using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SolarQuotationBillingSystem.Models;
using SolarQuotationBillingSystem.ViewModels;
using System;
using System.Linq;

namespace SolarQuotationBillingSystem.Helpers
{
    public class InvoiceDocument : IDocument
    {
        private readonly InvoiceViewModel _model;

        public InvoiceDocument(InvoiceViewModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Cambria"));

                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                // Top Header (ADISH ENTERPRISES + Logo)
                column.Item().Row(row =>
                {
                    // Left Column: Logo + Company Name
                    // Left Column: Logo + Company Name vertically stacked
                    row.RelativeItem(3).Column(col =>
                    {
                        // Logo (top left)
                        string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                        if (System.IO.File.Exists(logoPath))
                        {
                            col.Item().Width(70).Image(logoPath);
                        }

                        // Company Name in one line
                        col.Item().Text(_model.MyCompanyName).FontFamily("Segoe UI").FontSize(24).FontColor("#1976D2").Bold();
                        col.Item().Text("COMPLETE SOLAR SOLUTION").FontFamily("Segoe UI").FontSize(11).FontColor("#FF8C00").SemiBold();
                    });
                    
                    // Right Column: Address and Contact Details (aligned to bottom)
                    row.RelativeItem(2).AlignBottom().AlignRight().Column(col =>
                    {
                        col.Item().Text("Bhopal, Madhya Pradesh").FontFamily("Segoe UI").FontSize(11).Bold().AlignRight();
                        col.Item().Text("C-3 Inox Garden, K-Sector, Ayodhya Nagar,").FontFamily("Segoe UI").FontSize(10).AlignRight();
                        col.Item().Text("Bhopal, M.P. - 462011").FontFamily("Segoe UI").FontSize(10).AlignRight();
                        col.Item().Text("Phone: +91-9407299837").FontFamily("Segoe UI").FontSize(10).AlignRight();
                        col.Item().Text("Email: adishenterprises09@gmail.com").FontFamily("Segoe UI").FontSize(10).AlignRight();
                    });
                });

                column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // INVOICE Title Banner
                column.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignCenter().Text("INVOICE").FontSize(16).FontColor(Colors.Blue.Darken2).Bold();
                
                column.Item().PaddingBottom(15);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                // Include Header only on the first page
                column.Item().Element(ComposeHeader);

                // Details Row
                column.Item().Row(row =>
                {
                    // Swapped: Customer Details on Left (Removed PREPARED FOR)
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                    {
                        col.Item().Text(text => { text.Span("Customer Name: ").Bold(); text.Span(_model.CustomerName); });
                        col.Item().Text(text => { text.Span("Address: ").Bold(); text.Span($"{_model.Address}, {_model.City}"); });
                        col.Item().Text(text => { text.Span("Contact: ").Bold(); text.Span(_model.Mobile); });
                    });

                    row.ConstantItem(15); // Gap

                    // Right side: Invoice Details Box (Without Header)
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                    {
                        col.Item().Text(text => { text.Span("Invoice No: ").Bold(); text.Span(_model.InvoiceNo); });
                        col.Item().Text(text => { text.Span("Date: ").Bold(); text.Span(_model.InvoiceDate.ToString("dd MMMM yyyy")); });
                        if (!string.IsNullOrWhiteSpace(_model.CompanyGstNumber))
                        {
                            col.Item().Text(text => { text.Span("GST No: ").Bold(); text.Span(_model.CompanyGstNumber); });
                        }
                        if (!string.IsNullOrWhiteSpace(_model.MyCompanyPAN))
                        {
                            col.Item().Text(text => { text.Span("PAN No: ").Bold(); text.Span(_model.MyCompanyPAN); });
                        }
                    });
                });



                // Table 1: Technical Specs
                column.Item().PaddingTop(15).PaddingBottom(5).Text("SYSTEM COMPONENTS").Bold().FontSize(12);
                
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30); // S.No
                        columns.RelativeColumn(3); // Component
                        columns.RelativeColumn(4); // Description
                        columns.RelativeColumn(1); // Qty
                        columns.RelativeColumn(2); // Brand
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken1).Padding(3).Text("S.No").FontSize(10).FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken1).Padding(3).Text("Component").FontSize(10).FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken1).Padding(3).Text("Description / Specification").FontSize(10).FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken1).Padding(3).Text("Qty").FontSize(10).FontColor(Colors.White).SemiBold();
                        header.Cell().Background(Colors.Blue.Darken1).Padding(3).Text("Brand").FontSize(10).FontColor(Colors.White).SemiBold();
                    });

                    int sno = 1;
                    foreach (var item in _model.InvoiceItems)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3).Text(sno.ToString()).FontSize(10);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3).Text(item.Component).FontSize(10).SemiBold();
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3).Text(item.Description).FontSize(10);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3).Text($"{item.Quantity} {item.Unit}").FontSize(10);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3).Text(item.Brand).FontSize(10);
                        sno++;
                    }
                });

                // Table 2: Commercial Proposal
                column.Item().PaddingTop(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(120);
                    });

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text("Amount").SemiBold();
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text($"₹{_model.TotalTaxableAmount:N2}").SemiBold();

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text("Total CGST");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text($"₹{_model.TotalCGST:N2}");

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text("Total SGST");
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text($"₹{_model.TotalSGST:N2}");

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text("Less: Govt. Subsidy").FontColor(Colors.Green.Darken2);
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text($"- ₹{_model.Subsidy:N2}").FontColor(Colors.Green.Darken2);
                    
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Blue.Lighten5).Padding(10).AlignRight().Text("Net Payable Amount").FontColor(Colors.Blue.Darken2).Bold();
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Blue.Lighten5).Padding(10).AlignRight().Text($"₹{_model.NetPayable:N2}").FontColor(Colors.Blue.Darken2).Bold();
                });

                string payRefInfo = !string.IsNullOrWhiteSpace(_model.PaymentRefNo) ? $" ({_model.GetPaymentRefHeader()}: {_model.PaymentRefNo})" : "";
                column.Item().PaddingTop(12).Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Payment Mode: ").Bold();
                        t.Span($"{_model.SelectedPaymentMode ?? "Cash"}{payRefInfo}");
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Total Amount: ").Bold();
                        t.Span($"₹{_model.NetPayable:N2}").Bold().FontColor(Colors.Blue.Darken2);
                    });
                });
                
                // Signatory
                string sigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "signature.png");
                if (!System.IO.File.Exists(sigPath))
                {
                    sigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "signature.png");
                }

                column.Item().PaddingTop(15).AlignRight().Width(180).Column(col => 
                {
                    if (System.IO.File.Exists(sigPath))
                    {
                        col.Item().AlignCenter().Width(150).Height(60).Image(sigPath);
                    }
                    else
                    {
                        col.Item().PaddingTop(30);
                    }
                    col.Item().AlignCenter().Text("Mukesh Yadav").Bold();
                    col.Item().AlignCenter().Text("Lead Consultant").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text("* This is a computer-generated invoice. No signature is required. The information provided herein is completely valid and authentic.").FontSize(8).FontColor(Colors.Grey.Medium);
                row.ConstantItem(100).AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }
    }
}
