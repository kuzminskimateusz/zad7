using Tutorial7.DTOs;

namespace Tutorial7.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string status,string patientLastName);
    Task<IEnumerable<AppointmentDetailsDto>> GetAppointmentsIdAsync(int? id);
    Task<int> CreateApointmentAsync(CreateAppointmentRequestDto appointment);
    Task<int> UpdateApointmentAsync(int id, UpdateAppointmentRequestDto appointment);
    Task<int> DeleteAppointmentAsync(int id);
}