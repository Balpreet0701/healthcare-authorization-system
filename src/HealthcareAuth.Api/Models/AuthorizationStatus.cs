namespace HealthcareAuth.Api.Models;

public enum AuthorizationStatus
{
    Draft,
    Submitted,
    InReview,
    PendingInformation,
    Approved,
    Denied,
    Cancelled
}

public enum PriorityLevel
{
    Routine,
    Urgent,
    Stat
}

public enum OcrStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
