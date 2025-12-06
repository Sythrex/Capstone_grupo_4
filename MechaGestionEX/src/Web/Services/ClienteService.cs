using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Models;

namespace Web.Services
{
    public class ClienteService : IClienteService
    {
        private readonly TallerMecanicoContext _db;

        public ClienteService(TallerMecanicoContext db)
        {
            _db = db;
        }

        public async Task UpdatePersonalDataAsync(MisDatosViewModel model, int clienteId)
        {
            var cliente = await _db.cliente.FindAsync(clienteId);
            if (cliente == null) throw new Exception("Cliente no encontrado");

            cliente.rut = model.Rut;
            cliente.nombre = model.Nombre;
            cliente.correo = model.Correo;
            cliente.telefono = model.Telefono;
            cliente.direccion = model.Direccion;
            cliente.comuna_id = model.ComunaId;

            await _db.SaveChangesAsync();
        }

        public async Task<List<vehiculo>> ListVehiclesAsync(int clienteId)
        {
            return await _db.cliente_vehiculo
                .Where(cv => cv.cliente_id == clienteId)
                .Include(cv => cv.vehiculo)
                    .ThenInclude(v => v.tipo)
                .Select(cv => cv.vehiculo)
                .ToListAsync();
        }

        public async Task AddVehicleAsync(VehiculoViewModel model, int clienteId)
        {
            var nuevoVehiculo = new vehiculo
            {
                patente = model.Patente,
                vin = model.Vin,
                anio = model.Anio,
                kilometraje = model.Kilometraje,
                color = model.Color,
                tipo_id = model.TipoId
            };
            _db.vehiculo.Add(nuevoVehiculo);
            await _db.SaveChangesAsync();

            var nuevoLink = new cliente_vehiculo
            {
                cliente_id = clienteId,
                vehiculo_id = nuevoVehiculo.id,
                principal = false,
                fecha_desde = DateOnly.FromDateTime(DateTime.Now),
                created_at = DateOnly.FromDateTime(DateTime.Now)
            };
            _db.cliente_vehiculo.Add(nuevoLink);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateVehicleAsync(VehiculoViewModel model, int clienteId)
        {
            var vehiculo = await _db.vehiculo.FindAsync(model.Id);
            if (vehiculo == null) throw new Exception("Vehículo no encontrado");

            var linkExists = await _db.cliente_vehiculo.AnyAsync(cv => cv.cliente_id == clienteId && cv.vehiculo_id == model.Id);
            if (!linkExists) throw new Exception("No autorizado para actualizar este vehículo");

            vehiculo.patente = model.Patente;
            vehiculo.vin = model.Vin;
            vehiculo.anio = model.Anio;
            vehiculo.kilometraje = model.Kilometraje;
            vehiculo.color = model.Color;
            vehiculo.tipo_id = model.TipoId;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteVehicleAsync(int vehicleId, int clienteId)
        {
            var link = await _db.cliente_vehiculo
                .FirstOrDefaultAsync(cv => cv.cliente_id == clienteId && cv.vehiculo_id == vehicleId);
            if (link == null) throw new Exception("Link no encontrado");

            _db.cliente_vehiculo.Remove(link);

            var otherLinks = await _db.cliente_vehiculo.AnyAsync(cv => cv.vehiculo_id == vehicleId && cv.cliente_id != clienteId);
            if (!otherLinks)
            {
                var vehiculo = await _db.vehiculo.FindAsync(vehicleId);
                if (vehiculo != null) _db.vehiculo.Remove(vehiculo);
            }

            await _db.SaveChangesAsync();
        }

        public async Task ScheduleAppointmentAsync(CrearAgendaClienteViewModel model, int clienteId)
        {
            var occupied = await _db.agenda
                .AnyAsync(a => a.fecha_agenda == model.FechaAgenda && a.estado != "Cancelada");
            if (occupied) throw new Exception("Slot ocupado");

            var nuevaAgenda = new agenda
            {
                titulo = "Solicitud de Cliente",
                fecha_agenda = model.FechaAgenda,
                estado = "Pendiente"
            };

            var nuevaAtencion = new atencion
            {
                observaciones = model.Observaciones,
                cliente_id = clienteId,
                vehiculo_id = model.VehiculoId,
                taller_id = 1,
                administrativo_id = 2,
                agenda = nuevaAgenda,
                estado = "Solicitud pendiente",
                kilometraje_ingreso = 0
            };

            _db.atencion.Add(nuevaAtencion);
            await _db.SaveChangesAsync();

            var nuevaBitacora = new bitacora
            {
                atencion_id = nuevaAtencion.id,
                descripcion = "Reserva de hora a través del portal.",
                created_at = DateTime.Now,
                tipo = "Cliente"
            };
            _db.bitacora.Add(nuevaBitacora);
            await _db.SaveChangesAsync();
        }

        public async Task<List<dynamic>> GetOccupiedSlotsAsync(DateTime start, DateTime end)
        {
            return await _db.agenda
                .Where(a => a.fecha_agenda >= start && a.fecha_agenda <= end && a.estado != "Cancelada")
                .Select(a => new
                {
                    start = a.fecha_agenda,
                    end = a.fecha_agenda.AddHours(1)
                })
                .ToListAsync<dynamic>();
        }
    }
}