using System.Diagnostics;

namespace HealthcareAuth.Api.Services;

public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension is ".txt" or ".csv" or ".json")
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }

        if (extension is ".pdf")
        {
            return "PDF uploaded successfully. OCR for PDFs requires converting pages to images before Tesseract extraction.";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "tesseract",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(filePath);
        startInfo.ArgumentList.Add("stdout");

        try
        {
            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start tesseract process.");

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                    ? "Tesseract failed without an error message."
                    : error.Trim());
            }

            return string.IsNullOrWhiteSpace(output)
                ? "OCR completed, but no text was detected."
                : output.Trim();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _logger.LogWarning(ex, "Tesseract OCR is not available or failed for {FilePath}", filePath);
            return "OCR pending. Install Tesseract OCR locally and upload an image file to extract text.";
        }
    }
}
