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

        var fileName = $"{contract.ContractNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
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

                    RenderHtml(col, contract.ContractBody);

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

    public string GenerateAdditionalActPdf(ContractAdditionalAct act)
    {
        var folder = Path.Combine("wwwroot", "contracts");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName = $"{act.ActNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(folder, fileName);

        var clientSignature = GetImage(act.ClientSignature);
        var adminSignature = GetImage(act.AdminSignature);

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
                    col.Item().AlignCenter().Text("ACT ADIȚIONAL")
                        .FontSize(16)
                        .Bold();

                    col.Item().AlignCenter().Text($"Nr. {act.ActNumber}")
                        .FontSize(12);

                    col.Item().AlignCenter().Text(
                        $"Data: {DateTime.UtcNow:dd.MM.yyyy}"
                    )
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
                });

                // ================= CONTENT =================
                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    RenderHtml(col, act.Body);

                   
                    col.Item().PaddingTop(30);

                    // ================= SEMNĂTURI =================
                    col.Item().Row(row =>
                    {
                        // ADMIN
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(4);

                            left.Item()
                                .Text("PRESTATOR")
                                .Bold();

                            left.Item()
                                .Text(act.Contract?.CompanyNameSnapshot ?? "-");

                            left.Item().PaddingTop(10);

                            if (adminSignature != null)
                                left.Item()
                                    .Height(60)
                                    .Image(adminSignature);

                            left.Item().Text(
                                act.AdminSignedAtUtc?.ToString("dd.MM.yyyy") ?? "-"
                            )
                            .FontSize(10);
                        });

                        // CLIENT
                        row.RelativeItem().Column(right =>
                        {
                            right.Spacing(4);

                            right.Item()
                                .AlignRight()
                                .Text("BENEFICIAR")
                                .Bold();

                            right.Item()
                                .AlignRight()
                                .Text(act.Contract?.BeneficiaryNameSnapshot ?? "-");

                            right.Item().PaddingTop(10);

                            if (clientSignature != null)
                                right.Item()
                                    .Height(60)
                                    .Image(clientSignature);

                            right.Item()
                                .AlignRight()
                                .Text(
                                    act.ClientSignedAtUtc?.ToString("dd.MM.yyyy") ?? "-"
                                )
                                .FontSize(10);
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
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

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
                    col.Spacing(2);

                    RenderHtml(col, model.Body);

                    col.Item().PaddingTop(20);

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

    private void RenderHtml(ColumnDescriptor col, string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return;

        html = html
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("&nbsp;", " ");

        var blocks = Regex.Matches(
            html,
            @"<(h3|p|li)[^>]*>(.*?)</\1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match block in blocks)
        {
            var tag = block.Groups[1].Value.ToLower();

            var text = Regex.Replace(block.Groups[2].Value, "<.*?>", string.Empty);
            text = System.Net.WebUtility.HtmlDecode(text).Trim();

            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (tag == "h3")
            {
                col.Item()
                    .PaddingTop(7)
                    .PaddingBottom(2)
                    .Text(text)
                    .Bold()
                    .FontSize(12);
            }
            else if (tag == "li")
            {
                col.Item()
                    .PaddingLeft(12)
                    .Text("• " + text)
                    .FontSize(10)
                    .LineHeight(1.15f);
            }
            else
            {
                col.Item()
                    .Text(text)
                    .FontSize(10)
                    .LineHeight(1.15f);
            }
        }
    }
    private byte[]? GetImage(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        var commaIndex = base64.IndexOf(",");
        if (commaIndex >= 0)
            base64 = base64.Substring(commaIndex + 1);

        return Convert.FromBase64String(base64);
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