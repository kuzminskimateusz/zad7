using Microsoft.Data.SqlClient;
using Tutorial7.DTOs;

namespace Tutorial7.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly string _connectionString;

    public AppointmentsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ;
    }

    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status,string? patientLastName)
    {
        var query = "SELECT a.IdAppointment, a.AppointmentDate, a.Status,a.Reason,p.FirstName + N' ' + " +
                    "p.LastName AS PatientFullName,p.Email AS PatientEmail FROM dbo.Appointments a JOIN dbo.Patients " +
                    "p ON p.IdPatient = a.IdPatient WHERE (@Status IS NULL OR a.Status = @Status) AND " +
                    "(@PatientLastName IS NULL OR p.LastName = @PatientLastName) ORDER BY a.AppointmentDate";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand();
        command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue("@PatientLastName", (object?)patientLastName ?? DBNull.Value);
        command.Connection = connection;
        command.CommandText = query;

        var reader = await command.ExecuteReaderAsync();
        
        var appointments = new List<AppointmentListDto>();
        while (await reader.ReadAsync())
        {
            var appointment = new AppointmentListDto()
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(3) + " " + reader.GetString(4),
                PatientEmail = reader.GetString(5),
            };
            appointments.Add(appointment);
            Console.WriteLine(appointment);
        }

        return appointments;
    }
    
    public async Task<IEnumerable<AppointmentDetailsDto>> GetAppointmentsIdAsync(int? id)
    {
        var query = "SELECT a.IdAppointment, a.AppointmentDate, p.FirstName, p.LastName, p.DateOfBirth, a.Status,a.Reason, " +
                    "p.Email AS PatientEmail FROM dbo.Appointments a JOIN dbo.Patients " +
                    "p ON p.IdPatient = a.IdPatient WHERE (a.IdAppointment = @Id) ORDER BY a.AppointmentDate";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand();
        command.Parameters.AddWithValue("@Id", (object?)id ?? DBNull.Value);
        
        command.Connection = connection;
        command.CommandText = query;

        var reader = await command.ExecuteReaderAsync();
        
        var appointments = new List<AppointmentDetailsDto>();
        while (await reader.ReadAsync())
        {
            var appointment = new AppointmentDetailsDto()
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3),
                DateOfBirth = reader.GetDateTime(4),
                Status = reader.GetString(5),
                Reason = reader.GetString(6),
                PatientEmail = reader.GetString(7),
            };
            appointments.Add(appointment);
            Console.WriteLine(appointment);
        }

        return appointments;
    }

    public async Task<int> CreateApointmentAsync(CreateAppointmentRequestDto appointment)
    {
        if (appointment.AppointmentDate <= DateTime.Now)
        {
            throw new ArgumentException("Zła data");
        }
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query =
            @"
        SELECT 
            (SELECT COUNT(*) FROM Patients WHERE IdPatient = @IdPatient AND IsActive = 1),
            (SELECT COUNT(*) FROM Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1),
            (SELECT COUNT(*) FROM Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @Date)";
        

        await using var command = new SqlCommand();
        command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
        command.Parameters.AddWithValue("@Date",appointment.AppointmentDate);
        command.Parameters.AddWithValue("@Reason",appointment.Reason);
        
        command.Connection = connection;
        command.CommandText = query;

        var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            if(reader.GetInt32(0) == 0) throw new KeyNotFoundException("Pacjent nie istnieje");
            if(reader.GetInt32(1) == 0) throw new KeyNotFoundException("Lekarz nie istnieje");
            if(reader.GetInt32(2) > 0) throw new InvalidOperationException("Konflikt");
        }

        await reader.CloseAsync();

        var query2 = "INSERT INTO Appointments(IdPatient,IdDoctor,AppointmentDate,Reason,Status) VALUES(@IdPatient,@IdDoctor,@Date,@Reason,'Scheduled'); SELECT SCOPE_IDENTITY();";
        
        await using var insert = new SqlCommand();
        insert.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
        insert.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
        insert.Parameters.AddWithValue("@Date", appointment.AppointmentDate);
        insert.Parameters.AddWithValue("@Reason", appointment.Reason);
        
        insert.Connection = connection;
        insert.CommandText = query2;

        var newID = Convert.ToInt32(await insert.ExecuteScalarAsync());
        return newID;

    }
    
    
     public async Task<int> UpdateApointmentAsync(int id,UpdateAppointmentRequestDto appointment)
    {
        var possibleOutcome = new[]{"Scheduled","Completed","Cancelled"};
        if (!possibleOutcome.Contains(appointment.Status))
        {
            throw new ArgumentException(new ErrorResponseDto(appointment.Status + " wrong").ToString());
        }
        
       
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query =
            @"
        SELECT 
            (SELECT Status FROM Appointments WHERE IdAppointment = @Id),
            (SELECT AppointmentDate FROM Appointments WHERE IdAppointment = @Id),
            (SELECT COUNT(*) FROM Patients WHERE IdPatient = @IdPatient AND IsActive = 1),
            (SELECT COUNT(*) FROM Doctors WHERE IdDoctor = @IdDoctor AND IsActive = 1),
            (SELECT COUNT(*) FROM Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @Date AND IdAppointment <> @Id)";
        

        await using var command = new SqlCommand();
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
        command.Parameters.AddWithValue("@Date", appointment.AppointmentDate);
        
        command.Connection = connection;
        command.CommandText = query;
        String status = "";
        DateTime appointmentDate = DateTime.Now;
        
        var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            status = reader.GetString(0);
            appointmentDate = reader.GetDateTime(1);
            if(reader.GetInt32(2) == 0) throw new KeyNotFoundException("Pacjent nie istnieje");
            if (reader.GetInt32(3) == 0)
                throw new KeyNotFoundException("Lekarz nie istnieje");
            if(reader.GetInt32(4) > 0) throw new InvalidOperationException("Konflikt");
        }
        await reader.CloseAsync();
        
        if(status == "Completed" && appointmentDate != appointment.AppointmentDate) throw new InvalidOperationException(appointment.Status + " wrong"); 

        var query2 = "UPDATE Appointments SET IdPatient = @IdPatient, IdDoctor = @IdDoctor,AppointmentDate = @Date,  Status=@Status, Reason = @Reason, InternalNotes = @Internal WHERE IdAppointment=@Id";
        
        await using var insert = new SqlCommand();
        insert.Parameters.AddWithValue("@Id", id);
        insert.Parameters.AddWithValue("@IdPatient", appointment.IdPatient);
        insert.Parameters.AddWithValue("@IdDoctor", appointment.IdDoctor);
        insert.Parameters.AddWithValue("@Date", appointment.AppointmentDate);
        insert.Parameters.AddWithValue("@Status", appointment.Status);
        insert.Parameters.AddWithValue("@Reason", appointment.Reason);
        insert.Parameters.AddWithValue("@Internal", appointment.InternalNotes);
        
        insert.Connection = connection;
        insert.CommandText = query2;

        var newID = Convert.ToInt32(await insert.ExecuteScalarAsync());
        return newID;

    }

    public async Task<int> DeleteAppointmentAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "DELETE from Appointments WHERE IdAppointment = @Id";
            
        
        await using var command = new SqlCommand();
        command.Parameters.AddWithValue("@Id", id);
        command.Connection = connection;
        command.CommandText = query;
        int howManyDeleted = await command.ExecuteNonQueryAsync();
        return howManyDeleted;
    }
    
    
}