using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERPSystem.Data.Entities;

public class PdfService
{
    public string GenerateContractPdf(StudentContract contract)
    {
        var folder = Path.Combine("wwwroot", "contracts");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName = $"{contract.ContractNumber}.pdf";

        var filePath = Path.Combine(folder, fileName);

        var signatureBytes = string.IsNullOrEmpty(contract.ClientSignature)
            ? null
            : Convert.FromBase64String(
                contract.ClientSignature.Replace("data:image/png;base64,", "")
            );

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header()
                    .Text($"Contract {contract.ContractNumber}")
                    .FontSize(20)
                    .Bold();

                page.Content()
                    .Column(col =>
                    {
                        col.Item().Text(contract.ContractBody);

                        col.Item().PaddingTop(30);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col2 =>
                            {
                               
                            });

                            row.RelativeItem().Column(col2 =>
                            {
                              

                                if (signatureBytes != null)
                                {
                                    col2.Item().Image(signatureBytes);
                                }

                                col2.Item().Text(
                                    contract.ClientSignedAtUtc?.ToString("dd.MM.yyyy")
                                );
                            });
                        });
                    });
            });
        })
        .GeneratePdf(filePath);

        return fileName;
    }
}