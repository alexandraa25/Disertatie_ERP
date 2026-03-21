using ERPSystem.Data.Entities;
using ERPSystem.Shared.DTOs.PDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;

public class PdfService
{
    public string GenerateContractPdf(StudentContract contract)
    {
        var folder = Path.Combine("wwwroot", "contracts");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName = $"{contract.ContractNumber}.pdf";
        var filePath = Path.Combine(folder, fileName);

        var clientSignature = GetImage(contract.ClientSignature);
        var adminSignature = GetImage(contract.AdminSignature);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.DefaultTextStyle(x => x
                    .FontSize(11)
                );

                // ================= HEADER =================
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("CONTRACT DE PRESTĂRI SERVICII")
                        .FontSize(16).Bold();

                    col.Item().AlignCenter().Text($"Nr. {contract.ContractNumber}")
                        .FontSize(12);

                    col.Item().AlignCenter().Text(
                        $"Data: {DateTime.UtcNow:dd.MM.yyyy}"
                    ).FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                // ================= CONTENT =================
                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    // 🔹 BODY (din template)
                    var cleanText = CleanHtml(contract.ContractBody);
                    var paragraphs = SplitText(cleanText);

                    foreach (var p in paragraphs)
                    {
                        col.Item().Text(p).LineHeight(1.4f);
                    }

                    // 🔹 SPACING
                    col.Item().PaddingTop(30);

                    // ================= SEMNĂTURI =================
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(4);

 
                            if (adminSignature != null)
                                left.Item().Height(60).Image(adminSignature);

                            left.Item().Text(
                                contract.AdminSignedAtUtc?.ToString("dd.MM.yyyy") ?? "-"
                            ).FontSize(10);
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Spacing(4);

                            

                            if (clientSignature != null)
                                right.Item().Height(60).Image(clientSignature);

                            right.Item().AlignRight().Text(
                                contract.ClientSignedAtUtc?.ToString("dd.MM.yyyy") ?? "-"
                            ).FontSize(10);
                        });
                    });
                });

                // ================= FOOTER =================
                page.Footer()
    .AlignCenter()
    .Text(x =>
    {
        x.DefaultTextStyle(t => t
            .FontSize(10)
            .FontColor(Colors.Grey.Darken1)
        );

        x.Span("Pagina ");
        x.CurrentPageNumber();
    });

            });
        })
        .GeneratePdf(filePath);

        return fileName;
    }

    public string GeneratePdf(PdfDocumentModel model, string filePrefix)
    {
        var folder = Path.Combine("wwwroot", "contracts");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var safeNumber = model.Number.Replace(" ", "_");
        var fileName = $"{filePrefix}_{safeNumber}.pdf";
        var filePath = Path.Combine(folder, fileName);

        var clientSignature = GetImage(model.ClientSignature);
        var adminSignature = GetImage(model.AdminSignature);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.DefaultTextStyle(x => x.FontSize(11));

                // ================= HEADER =================
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(model.Title)
                        .FontSize(16).Bold();

                    col.Item().AlignCenter().Text($"Nr. {model.Number}")
                        .FontSize(12);

                    col.Item().AlignCenter().Text(
                        $"Data: {DateTime.UtcNow:dd.MM.yyyy}"
                    ).FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                // ================= CONTENT =================
                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    var cleanText = CleanHtml(model.Body);
                    var paragraphs = SplitText(cleanText);

                    foreach (var p in paragraphs)
                    {
                        col.Item().Text(p).LineHeight(1.4f);
                    }

                    col.Item().PaddingTop(30);

                    // ================= SEMNĂTURI =================
                    col.Item().Row(row =>
                    {
                        // PRESTATOR
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(4);

                            left.Item().Text("PRESTATOR").Bold();
                            left.Item().Text(model.CompanyName);

                            left.Item().Element(container =>
                            {
                                if (adminSignature != null)
                                    left.Item().Height(60).Image(adminSignature);
                            });

                            left.Item().Text(
                                model.AdminSignedAt?.ToString("dd.MM.yyyy") ?? "-"
                            ).FontSize(9);
                        });

                        // BENEFICIAR
                        row.RelativeItem().Column(right =>
                        {
                            right.Spacing(4);

                            right.Item().AlignRight().Text("BENEFICIAR").Bold();
                            right.Item().AlignRight().Text(model.BeneficiaryName);

                            right.Item().Element(container =>
                            {
                                if (clientSignature != null)
                                    right.Item().Height(60).Image(clientSignature);
                            });

                            right.Item().AlignRight().Text(
                                model.ClientSignedAt?.ToString("dd.MM.yyyy") ?? "-"
                            ).FontSize(9);
                        });
                    });
                });

                // ================= FOOTER =================
                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken1));
                        x.Span("Pagina ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf(filePath);

        return fileName;
    }


    // ================= HELPERS =================

    private byte[]? GetImage(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        var commaIndex = base64.IndexOf(",");
        if (commaIndex >= 0)
            base64 = base64.Substring(commaIndex + 1);

        return Convert.FromBase64String(base64);
    }

    private List<string> SplitText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return text
            .Replace("\r\n", "\n")
            .Split("\n")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    private string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        // 🔥 transformă paragrafe în newline
        html = html.Replace("</p>", "\n")
                   .Replace("<br>", "\n")
                   .Replace("<br/>", "\n")
                   .Replace("<br />", "\n");

        // 🔥 scoate restul tagurilor
        var text = Regex.Replace(html, "<.*?>", string.Empty);

        // 🔥 decode entities
        text = System.Net.WebUtility.HtmlDecode(text);

        return text;
    }
    public byte[] GenerateContractPdfBytes(StudentContract contract)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Content().Text(contract.ContractBody ?? "No content");
            });
        }).GeneratePdf();
    }

}