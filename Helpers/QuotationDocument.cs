using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SolarQuotationBillingSystem.Models;
using SolarQuotationBillingSystem.ViewModels;
using System;
using System.Linq;

namespace SolarQuotationBillingSystem.Helpers
{
    public class QuotationDocument : IDocument
    {
        private readonly QuotationViewModel _model;

        public QuotationDocument(QuotationViewModel model)
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
                    page.Margin(40);
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

                // QUOTATION Title Banner
                column.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignCenter().Text("QUOTATION").FontSize(16).FontColor(Colors.Blue.Darken2).Bold();
                
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

                    // Swapped: QUOTATION DETAILS on Right
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                    {
                        col.Item().Text(text => { text.Span("Quotation No: ").Bold(); text.Span(_model.QuotationNo); });
                        col.Item().Text(text => { text.Span("Date: ").Bold(); text.Span(_model.QuotationDate.ToString("dd MMMM yyyy")); });
                        col.Item().Text(text => { text.Span("Valid Until: ").Bold(); text.Span(_model.ValidUntil.ToString("dd MMMM yyyy")); });
                        if (!string.IsNullOrWhiteSpace(_model.MyCompanyGstNumber))
                        {
                            col.Item().Text(text => { text.Span("GST No: ").Bold(); text.Span(_model.MyCompanyGstNumber); });
                        }
                        if (!string.IsNullOrWhiteSpace(_model.MyCompanyPAN))
                        {
                            col.Item().Text(text => { text.Span("PAN No: ").Bold(); text.Span(_model.MyCompanyPAN); });
                        }
                    });
                });

                // Subject Line
                column.Item().PaddingTop(15).PaddingBottom(15).Background(Colors.Blue.Lighten5).Padding(8)
                    .Text("Subject: Quotation for 3kW Havells Complete On-Grid Solar System with Govt. Subsidy").FontColor(Colors.Blue.Darken2).SemiBold();

                // Greeting
                column.Item().Text("Dear Sir/Madam,");
                column.Item().PaddingTop(5).PaddingBottom(15).Text("Thank you for giving us the opportunity to quote for your solar requirement. We are pleased to submit our best commercial offer for a premium 3kW Havells Solar System under the PM-Surya Ghar Scheme. Below are the complete technical and financial details:");

                // Table 1: Technical Specs
                column.Item().PaddingBottom(5).Text("SYSTEM COMPONENTS").Bold().FontSize(12);
                
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
                    foreach (var item in _model.QuotationItems)
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
                column.Item().PaddingTop(20).PaddingBottom(5).Text("2. COMMERCIAL & FINANCIAL PROPOSAL").Bold().FontSize(12);
                
                column.Item().Table(table =>
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

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text("Less: Central Govt. Subsidy (PM Surya Ghar Yojana)").FontColor(Colors.Green.Darken2);
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignRight().Text($"- ₹{_model.Subsidy:N2}").FontColor(Colors.Green.Darken2);
                    
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Orange.Lighten5).Padding(10).AlignRight().Text("Net Effective Cost to Customer (Approx.)").FontColor(Colors.Orange.Darken2).Bold();
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Orange.Lighten5).Padding(10).AlignRight().Text($"₹{_model.NetPayable:N2}").FontColor(Colors.Orange.Darken2).Bold();
                });

                column.Item().PaddingTop(15).Border(1).BorderColor(Colors.Orange.Medium).Padding(10).AlignCenter().Text($"Net Investment: ₹{_model.NetPayable:N0}/- ({_model.AmountInWords})").FontColor(Colors.Orange.Darken2).Bold();

                // Terms and Conditions
                column.Item().PaddingTop(20).PaddingBottom(5).Text("3. KEY TERMS & CONDITIONS").Bold().FontSize(12);
                column.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                
                column.Item().PaddingBottom(3).Text(t => { t.Span("• Warranty: ").Bold(); t.Span("Solar Panels carry a 25-year performance warranty; Solar Inverter carries a 5-year standard warranty (provided by Havells)."); });
                column.Item().PaddingBottom(3).Text(t => { t.Span("• Subsidy Note: ").Bold(); t.Span("The government subsidy will be credited directly to the customer's bank account after successful installation, inspection, and net-metering activation via the National Portal."); });
                column.Item().PaddingBottom(3).Text(t => { t.Span("• Delivery & Installation: ").Bold(); t.Span("Within 7-10 working days from the date of receiving the advance payment."); });
                column.Item().PaddingBottom(3).Text(t => { t.Span("• Payment Terms: ").Bold(); t.Span("60% advance along with the work order, 30% on delivery of material at the site, and 10% post-successful installation & commissioning."); });
                column.Item().PaddingBottom(3).Text(t => { t.Span("• Net-Metering: ").Bold(); t.Span("Net-metering approval and discom (electricity board) documentation will be completely assisted by ADISH Enterprises."); });
                
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
                row.RelativeItem().Text("* This is a computer-generated quotation. No signature is required. The information provided herein is completely valid and authentic.").FontSize(8).FontColor(Colors.Grey.Medium);
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
