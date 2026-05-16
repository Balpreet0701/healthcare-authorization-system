using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Services;

public class BackgroundProcessingService : IBackgroundProcessingService
{
    private readonly ApplicationDbContext _db;
    private readonly IOcrService _ocrService;
    private readonly IAuthorizationWorkflowService _workflowService;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<BackgroundProcessingService> _logger;

    public BackgroundProcessingService(
        ApplicationDbContext db,
        IOcrService ocrService,
        IAuthorizationWorkflowService workflowService,
        IRabbitMqPublisher rabbitMqPublisher,
        ILogger<BackgroundProcessingService> logger)
    {
        _db = db;
        _ocrService = ocrService;
        _workflowService = workflowService;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    public async Task ProcessDocumentAsync(int documentId)
    {
        var document = await _db.MedicalDocuments.FirstOrDefaultAsync(x => x.Id == documentId);
        if (document is null)
        {
            return;
        }

        document.OcrStatus = OcrStatus.Processing;
        await _db.SaveChangesAsync();

        try
        {
            document.OcrText = await _ocrService.ExtractTextAsync(document.FilePath);
            document.OcrStatus = OcrStatus.Completed;
            document.OcrError = null;
            await _db.SaveChangesAsync();

            await _rabbitMqPublisher.PublishAsync("document.ocr.completed", new
            {
                document.Id,
                document.AuthorizationRequestId,
                document.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document OCR failed for {DocumentId}", documentId);
            document.OcrStatus = OcrStatus.Failed;
            document.OcrError = ex.Message;
            await _db.SaveChangesAsync();
        }
    }

    public async Task AnalyzeAuthorizationAsync(int authorizationRequestId)
    {
        await _workflowService.GenerateAiInsightsAsync(authorizationRequestId);
    }
}
