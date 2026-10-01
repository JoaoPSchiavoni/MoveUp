namespace MoveUp.Application.Services;

public class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

internal static class Validation
{
    public static string Required(string? value, string field, int max)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0 || text.Length > max)
            throw new ApiException(400, $"{field} deve conter de 1 a {max} caracteres.");
        return text;
    }
    public static string? Description(string? value)
    {
        var text = value?.Trim();
        if (text?.Length > 2000) throw new ApiException(400, "Descrição deve ter até 2000 caracteres.");
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
