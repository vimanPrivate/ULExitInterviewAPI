namespace ULExitInterview.Application.Divisions;

public sealed record DivisionDto(
    string DivisionCode,
    string DivisionName,
    IReadOnlyList<CostCenterDto> CostCenters);
