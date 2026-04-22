namespace Tutorial7.DTOs;

public class CreateAppointmentRequestDto
{
    public int IdAppointment { get; set; }
    public int IdDoctor { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string Reason { get; set; } = string.Empty;
}