using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ULExitInterview.Application.ExitStaff;

public sealed class GetExitInterviewInfoRequest
{
    public string? StaffName { get; init; }
    public string? StaffNo { get; init; }
    public string? CostCenterName { get; init; }
    public string? CostCenterCode { get; init; }
    public string? DivisionName { get; init; }
    public string? DivisionCode { get; init; }

    [Range(0, int.MaxValue)]
    public int? Status { get; init; }

    [JsonConverter(typeof(NullableDateTimeEmptyStringConverter))]
    public DateTime? LastWorkingDateFrom { get; init; }

    [JsonConverter(typeof(NullableDateTimeEmptyStringConverter))]
    public DateTime? LastWorkingDateTo { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNo { get; init; } = 1;

    [Range(1, 500)]
    public int PageSize { get; init; } = 10;
}

public sealed record ExitInterviewInfoResponseDto(
    int NotStarted,
    int Drafted,
    int Submitted,
    int Completed,
    int Overdue,
    int TotalRecords,
    int PageNo,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    IReadOnlyList<ExitInterviewInfoItemDto> Info);

public sealed record ExitInterviewInfoItemDto(
    string StaffNo,
    string EmpName,
    string Grade,
    int FlowId,
    int TemplateId,
    string Division,
    string DivisionCode,
    string CostCenter,
    string CostCenterCode,
    DateTime? EffectiveDate,
    int Status);

public sealed record ExitInterviewFlowSummaryDto(
    int FlowId,
    string StaffNo,
    int TemplateId,
    int Status);

public sealed record ExitInterviewStaffSearchDto(
    string StaffNo,
    string EmpName,
    string Grade,
    string Division,
    string DivisionCode,
    string CostCenter,
    string CostCenterCode,
    DateTime? EffectiveDate);

public sealed class NullableDateTimeEmptyStringConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Date must be a string, empty string, or null.");

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date))
            return date;

        throw new JsonException($"'{value}' is not a valid date.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
