namespace Tutorial7.DTOs;

public class ErrorResponseDto
{
    public string Message { get; set; }

    public ErrorResponseDto(string message)
    {
        Message = message;
    }
}