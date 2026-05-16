using System.Net.Http.Json;
using System.Text.Json;
using HealthcareAuth.Api.Models;
using HealthcareAuth.Api.Options;
using Microsoft.Extensions.Options;

namespace HealthcareAuth.Api.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _client;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(HttpClient client, IOptions<OllamaOptions> options, ILogger<OllamaService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateMedicalSummaryAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
        You are a utilization management clinical assistant.
        Produce a concise medical necessity summary for this prior authorization request.
        Include patient context, requested service, diagnosis/procedure codes, key clinical evidence, and missing information.

        Patient: {request.Patient?.FirstName} {request.Patient?.LastName}, DOB {request.Patient?.DateOfBirth}
        Service: {request.RequestedService}
        Diagnosis: {request.DiagnosisCode}
        Procedure: {request.ProcedureCode}
        Priority: {request.Priority}
        Notes: {request.ClinicalNotes}
        OCR documents:
        {string.Join("\n---\n", request.Documents.Select(x => x.OcrText).Where(x => !string.IsNullOrWhiteSpace(x)))}
        """;

        return await GenerateTextAsync(prompt, cancellationToken) ?? CreateFallbackSummary(request);
    }

    public async Task<RecommendationResult> GenerateRecommendationAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
        You are a prior authorization recommendation assistant.
        Return only valid JSON with keys: decision, confidence, rationale.
        decision must be one of: Approve, Deny, NeedMoreInfo.
        confidence must be a decimal from 0 to 1.
        Base the recommendation on medical necessity, documentation completeness, and obvious contraindications.

        Requested service: {request.RequestedService}
        Diagnosis: {request.DiagnosisCode}
        Procedure: {request.ProcedureCode}
        Priority: {request.Priority}
        Clinical notes: {request.ClinicalNotes}
        OCR documents:
        {string.Join("\n---\n", request.Documents.Select(x => x.OcrText).Where(x => !string.IsNullOrWhiteSpace(x)))}
        """;

        var response = await GenerateTextAsync(prompt, cancellationToken);
        if (!string.IsNullOrWhiteSpace(response) && TryParseRecommendation(response, out var recommendation))
        {
            return recommendation;
        }

        return CreateFallbackRecommendation(request);
    }

    private async Task<string?> GenerateTextAsync(string prompt, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        try
        {
            var response = await _client.PostAsJsonAsync("/api/generate", new
            {
                model = _options.Model,
                prompt,
                stream = false
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama returned {StatusCode}", response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
            return payload?.Response?.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama is unavailable. Falling back to deterministic recommendation logic.");
            return null;
        }
    }

    private static bool TryParseRecommendation(string response, out RecommendationResult recommendation)
    {
        recommendation = new RecommendationResult("NeedMoreInfo", 0.5m, "Unable to parse AI response.");

        var jsonStart = response.IndexOf('{');
        var jsonEnd = response.LastIndexOf('}');

        if (jsonStart < 0 || jsonEnd <= jsonStart)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(response[jsonStart..(jsonEnd + 1)]);
            var root = document.RootElement;

            var decision = root.TryGetProperty("decision", out var decisionElement)
                ? decisionElement.GetString() ?? "NeedMoreInfo"
                : "NeedMoreInfo";

            var confidence = root.TryGetProperty("confidence", out var confidenceElement)
                ? confidenceElement.GetDecimal()
                : 0.5m;

            var rationale = root.TryGetProperty("rationale", out var rationaleElement)
                ? rationaleElement.GetString() ?? "No rationale returned."
                : "No rationale returned.";

            recommendation = new RecommendationResult(NormalizeDecision(decision), Math.Clamp(confidence, 0, 1), rationale);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static RecommendationResult CreateFallbackRecommendation(AuthorizationRequest request)
    {
        var notes = request.ClinicalNotes.ToLowerInvariant();
        var hasDocuments = request.Documents.Any(x => !string.IsNullOrWhiteSpace(x.OcrText));

        if (!hasDocuments && request.Documents.Count == 0)
        {
            return new RecommendationResult(
                "NeedMoreInfo",
                0.62m,
                "No supporting medical document has been uploaded. Request additional clinical documentation before final review.");
        }

        if (notes.Contains("experimental", StringComparison.OrdinalIgnoreCase) || notes.Contains("not medically necessary", StringComparison.OrdinalIgnoreCase))
        {
            return new RecommendationResult(
                "Deny",
                0.71m,
                "Clinical notes contain language suggesting experimental care or lack of medical necessity.");
        }

        if (request.Priority is PriorityLevel.Urgent or PriorityLevel.Stat && request.ClinicalNotes.Length > 80)
        {
            return new RecommendationResult(
                "Approve",
                0.78m,
                "Urgent request includes diagnosis, procedure, and clinical history supporting medical necessity.");
        }

        return new RecommendationResult(
            "NeedMoreInfo",
            0.58m,
            "Documentation is present but limited. Reviewer should confirm guideline criteria before approval.");
    }

    private static string CreateFallbackSummary(AuthorizationRequest request)
    {
        var documentCount = request.Documents.Count;
        return $"Request {request.RequestNumber} is for {request.RequestedService} with diagnosis {request.DiagnosisCode} and procedure {request.ProcedureCode}. Priority is {request.Priority}. Clinical notes indicate: {request.ClinicalNotes}. Supporting documents uploaded: {documentCount}.";
    }

    private static string NormalizeDecision(string decision)
    {
        return decision.Trim().ToLowerInvariant() switch
        {
            "approve" or "approved" => "Approve",
            "deny" or "denied" => "Deny",
            _ => "NeedMoreInfo"
        };
    }

    private sealed record OllamaGenerateResponse(string? Response);
}
