using Infrastructure.Persistence.Models;
using Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Web.Services
{
    public interface IClienteService
    {
        Task UpdatePersonalDataAsync(MisDatosViewModel model, int clienteId);
        Task<List<vehiculo>> ListVehiclesAsync(int clienteId);
        Task AddVehicleAsync(VehiculoViewModel model, int clienteId);
        Task UpdateVehicleAsync(VehiculoViewModel model, int clienteId);
        Task DeleteVehicleAsync(int vehicleId, int clienteId);
        Task ScheduleAppointmentAsync(CrearAgendaClienteViewModel model, int clienteId);
        Task<List<dynamic>> GetOccupiedSlotsAsync(DateTime start, DateTime end);
    }
}