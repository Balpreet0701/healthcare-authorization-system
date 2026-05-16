namespace HealthcareAuth.Api.Contracts;

public record PatientCreateRequest(
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Phone,
    string Email,
    string InsuranceProvider,
    string MemberNumber);

public record PatientUpdateRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Phone,
    string Email,
    string InsuranceProvider,
    string MemberNumber);

public record PatientResponse(
    int Id,
    string MedicalRecordNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string Phone,
    string Email,
    string InsuranceProvider,
    string MemberNumber,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
