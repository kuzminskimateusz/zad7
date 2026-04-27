using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial7.DTOs;
using Tutorial7.Services;

namespace Tutorial7.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentsService _appointmentsService;

        public AppointmentsController(IAppointmentsService appointmentsService)
        {
            _appointmentsService = appointmentsService;
        }
        
        [HttpGet]
        public async Task<IActionResult> Get(string? status, string? patientLastName)
        {
            var appointments = await _appointmentsService.GetAllAppointmentsAsync(status, patientLastName);
            return Ok(appointments);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int? id)
        {
            var appointment = await _appointmentsService.GetAppointmentsIdAsync(id);
            if (appointment == null)
            {
                var error = new ErrorResponseDto("Blad not found");
                return NotFound(error); 
            }
            return Ok(appointment);
        }
        
        [HttpPost]
        public async Task<IActionResult> Create(CreateAppointmentRequestDto appointment)
        {
            try
            {
                var newId = await _appointmentsService.CreateApointmentAsync(appointment);
                return Created($"/api/appointments/{newId}", new 
                { 
                    Id = newId, 
                    Message = "Wizyta utworzona",
                    Date = appointment.AppointmentDate
                });
            }
            catch (ArgumentException ex)
            {
                var error = new ErrorResponseDto(ex.Message);
                return BadRequest(error); 
            }
            catch (KeyNotFoundException ex)
            {
                var error = new ErrorResponseDto(ex.Message);
                return NotFound(error);
            }
            catch (InvalidOperationException ex)
            {
                var error = new ErrorResponseDto(ex.Message);
                return Conflict(error); 
            }
            
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateAppointmentRequestDto appointment)
        {
            try
            {
                var newId = await _appointmentsService.UpdateApointmentAsync(id, appointment);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                var error = new ErrorResponseDto(ex.Message);
                return BadRequest(error); 
            }
            catch (KeyNotFoundException ex)
            {
                var error = new ErrorResponseDto(ex.Message);
                return NotFound(error);
            }
            catch (InvalidOperationException ex)
            {
                var error = new ErrorResponseDto(ex.Message);
                return Conflict(error); 
            }
        }
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _appointmentsService.DeleteAppointmentAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErrorResponseDto(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponseDto("Nie można usunąć wizyty. Może być powiązana z innymi danymi."));
            }
        }
    }
}
