namespace ERPSystem.Modules.Contracts.Models;

public record ContractListItemDto(
    int Id,
    string ContractNumber,
    string MainGuardianName,
    DateTime StartDate,
    DateTime? EndDate,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAtUtc
);

public record ContractDetailsDto(
    int Id,
    string ContractNumber,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsUnlimited,
    decimal TotalAmount,
    int Installments,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? FinalizedAtUtc,
    DateTime? SignedAtUtc,
    string ContractBody,
    List<ContractPartyDto> Parties,
    List<ContractCourseDto> Courses,
    List<ContractDiscountDto> Discounts
);
public record ContractPartyDto(
    int? StudentId,
    string? StudentName,
    int? GuardianId,
    string? GuardianName,
    string Role
);

public record ContractDiscountDto(
    string Type,
    decimal Value,
    string Reason
);

public record ContractCourseDto(
    int CourseSessionId,
    string CourseName,
    string SessionName,
    decimal Price
);

public record CreateContractDto(
    int? GuardianId,
    List<int> StudentIds,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsUnlimited,
    int Installments,
    List<int> CourseSessionIds,
    List<CreateDiscountDto>? Discounts
);

public record CreateDiscountDto(
    string Type,
    decimal Value,
    string Reason
);

public record UpdateContractDto(
    DateTime StartDate,
    DateTime? EndDate,
    bool IsUnlimited,
    int Installments,
    List<int> CourseSessionIds,
    List<CreateDiscountDto>? Discounts
);
public record UpdateContractBodyDto(
    string ContractBody
);

