namespace PsP.Contracts.Common;

public class ApiErrorResponse
{
    public string Error { get; set; }
    public string? Details { get; set; }

    public ApiErrorResponse(string error, string? details = null)
    {
        Error = error;
        Details = details;
    }
}