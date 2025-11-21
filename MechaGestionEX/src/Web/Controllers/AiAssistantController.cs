using Infrastructure.Persistence;
using Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Cliente")]
    [Route("ai-assistant")]
    [ApiController]
    public class AiAssistantController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TallerMecanicoContext _db;
        private readonly IClienteService _clienteService;

        public AiAssistantController(IHttpClientFactory httpClientFactory, TallerMecanicoContext db, IClienteService clienteService)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _clienteService = clienteService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            int clienteId = int.Parse(User.FindFirst("ClienteId")?.Value ?? "0");
            if (clienteId <= 0) return Unauthorized();

            string systemPrompt = @"Eres un asistente AI para clientes de un taller mecánico. Puedes ayudar con: modificar datos personales, listar/agregar/modificar/eliminar vehículos, agendar atenciones verificando slots ocupados. Usa herramientas para ejecutar acciones. Responde en español de forma simple y directa. Siempre procesa los resultados de tools antes de responder: si un slot está ocupado, sugiere alternativas; si no hay vehículos, avisa. Pide confirmación para acciones como eliminar.";

            var tools = new List<object>
            {
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "update_personal_data",
                        description = "Modificar datos personales del cliente",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                rut = new { type = "string" },
                                nombre = new { type = "string" },
                                correo = new { type = "string" },
                                telefono = new { type = "string" },
                                direccion = new { type = "string" },
                                comunaId = new { type = "integer" }
                            },
                            required = new[] { "nombre", "correo" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "list_vehicles",
                        description = "Listar vehículos del cliente",
                        parameters = new { type = "object", properties = new { } }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "add_vehicle",
                        description = "Agregar un nuevo vehículo",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                patente = new { type = "string" },
                                vin = new { type = "string" },
                                anio = new { type = "integer" },
                                kilometraje = new { type = "integer" },
                                color = new { type = "string" },
                                tipoId = new { type = "integer" }
                            },
                            required = new[] { "patente", "anio", "tipoId" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "update_vehicle",
                        description = "Modificar un vehículo existente",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                id = new { type = "integer" },
                                patente = new { type = "string" },
                                vin = new { type = "string" },
                                anio = new { type = "integer" },
                                kilometraje = new { type = "integer" },
                                color = new { type = "string" },
                                tipoId = new { type = "integer" }
                            },
                            required = new[] { "id" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "delete_vehicle",
                        description = "Eliminar un vehículo",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                id = new { type = "integer" }
                            },
                            required = new[] { "id" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "schedule_appointment",
                        description = "Agendar una atención verificando slots ocupados",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                fechaAgenda = new { type = "string", description = "Fecha y hora en formato ISO" },
                                comentarios = new { type = "string" },
                                observaciones = new { type = "string" },
                                vehiculoId = new { type = "integer" }
                            },
                            required = new[] { "fechaAgenda", "observaciones" }
                        }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "check_occupied_slots",
                        description = "Verificar slots ocupados en una fecha",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                start = new { type = "string", description = "Fecha inicio ISO" },
                                end = new { type = "string", description = "Fecha fin ISO" }
                            },
                            required = new[] { "start", "end" }
                        }
                    }
                }
            };

            var client = _httpClientFactory.CreateClient("xAIClient");

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = request.UserMessage }
            };

            string reply = "";
            const int maxRounds = 5; // Prevent infinite loop
            int round = 0;

            while (round < maxRounds)
            {
                var requestBody = new
                {
                    model = "grok-4-1-fast-reasoning",
                    messages,
                    tools
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Error en la API de xAI");
                }

                var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var choice = result.RootElement.GetProperty("choices")[0];
                var message = choice.GetProperty("message");

                // Add assistant message to history
                JsonObject assistantMessage = new JsonObject
                {
                    { "role", "assistant" }
                };
                if (message.TryGetProperty("content", out var assistContent) && assistContent.ValueKind != JsonValueKind.Null)
                {
                    assistantMessage.Add("content", assistContent.GetString());
                }
                if (message.TryGetProperty("tool_calls", out var assistToolCalls) && assistToolCalls.ValueKind == JsonValueKind.Array)
                {
                    assistantMessage.Add("tool_calls", JsonNode.Parse(assistToolCalls.GetRawText()));
                }
                messages.Add(assistantMessage);

                if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array && toolCalls.GetArrayLength() > 0)
                {
                    foreach (var toolCall in toolCalls.EnumerateArray())
                    {
                        var function = toolCall.GetProperty("function");
                        var functionName = function.GetProperty("name").GetString();
                        var argsJson = function.GetProperty("arguments").GetString();
                        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson ?? "{}") ?? new Dictionary<string, object>();

                        string toolReply = "";
                        try
                        {
                            switch (functionName)
                            {
                                case "update_personal_data":
                                    var datosModel = new MisDatosViewModel
                                    {
                                        Rut = args.GetValueOrDefault("rut")?.ToString() ?? "",
                                        Nombre = args.GetValueOrDefault("nombre")?.ToString() ?? "",
                                        Correo = args.GetValueOrDefault("correo")?.ToString() ?? "",
                                        Telefono = args.GetValueOrDefault("telefono")?.ToString() ?? "",
                                        Direccion = args.GetValueOrDefault("direccion")?.ToString() ?? "",
                                        ComunaId = Convert.ToInt32(args.GetValueOrDefault("comunaId") ?? 0)
                                    };
                                    await _clienteService.UpdatePersonalDataAsync(datosModel, clienteId);
                                    toolReply = "Datos personales actualizados exitosamente.";
                                    break;

                                case "list_vehicles":
                                    var vehicles = await _clienteService.ListVehiclesAsync(clienteId);
                                    if (vehicles.Count == 0)
                                    {
                                        toolReply = "No se encontraron vehículos registrados.";
                                    }
                                    else
                                    {
                                        toolReply = string.Join("\n", vehicles.Select(v => $"{v.id}: {v.patente} - {v.anio} {v.tipo?.nombre ?? "Tipo desconocido"} {v.color} ({v.kilometraje} km)"));
                                    }
                                    break;

                                case "add_vehicle":
                                    var addModel = new VehiculoViewModel
                                    {
                                        Patente = args.GetValueOrDefault("patente")?.ToString() ?? "",
                                        Vin = args.GetValueOrDefault("vin")?.ToString() ?? "",
                                        Anio = Convert.ToInt32(args.GetValueOrDefault("anio") ?? 0),
                                        Kilometraje = Convert.ToInt32(args.GetValueOrDefault("kilometraje") ?? 0),
                                        Color = args.GetValueOrDefault("color")?.ToString() ?? "",
                                        TipoId = Convert.ToInt32(args.GetValueOrDefault("tipoId") ?? 0)
                                    };
                                    await _clienteService.AddVehicleAsync(addModel, clienteId);
                                    toolReply = "Vehículo agregado exitosamente.";
                                    break;

                                case "update_vehicle":
                                    var updateModel = new VehiculoViewModel
                                    {
                                        Id = Convert.ToInt32(args.GetValueOrDefault("id") ?? 0),
                                        Patente = args.GetValueOrDefault("patente")?.ToString() ?? "",
                                        Vin = args.GetValueOrDefault("vin")?.ToString() ?? "",
                                        Anio = Convert.ToInt32(args.GetValueOrDefault("anio") ?? 0),
                                        Kilometraje = Convert.ToInt32(args.GetValueOrDefault("kilometraje") ?? 0),
                                        Color = args.GetValueOrDefault("color")?.ToString() ?? "",
                                        TipoId = Convert.ToInt32(args.GetValueOrDefault("tipoId") ?? 0)
                                    };
                                    await _clienteService.UpdateVehicleAsync(updateModel, clienteId);
                                    toolReply = "Vehículo actualizado exitosamente.";
                                    break;

                                case "delete_vehicle":
                                    var deleteId = Convert.ToInt32(args.GetValueOrDefault("id") ?? 0);
                                    await _clienteService.DeleteVehicleAsync(deleteId, clienteId);
                                    toolReply = "Vehículo eliminado exitosamente.";
                                    break;

                                case "schedule_appointment":
                                    var agendaModel = new CrearAgendaClienteViewModel
                                    {
                                        FechaAgenda = DateTime.Parse(args.GetValueOrDefault("fechaAgenda")?.ToString() ?? DateTime.Now.ToString("o")),
                                        Comentarios = args.GetValueOrDefault("comentarios")?.ToString() ?? "",
                                        Observaciones = args.GetValueOrDefault("observaciones")?.ToString() ?? "",
                                        VehiculoId = Convert.ToInt32(args.GetValueOrDefault("vehiculoId") ?? 0)
                                    };
                                    await _clienteService.ScheduleAppointmentAsync(agendaModel, clienteId);
                                    toolReply = "Atención agendada exitosamente.";
                                    break;

                                case "check_occupied_slots":
                                    var start = DateTime.Parse(args.GetValueOrDefault("start")?.ToString() ?? DateTime.Now.ToString("o"));
                                    var end = DateTime.Parse(args.GetValueOrDefault("end")?.ToString() ?? DateTime.Now.AddDays(1).ToString("o"));
                                    var occupied = await _clienteService.GetOccupiedSlotsAsync(start, end);
                                    if (occupied.Count == 0)
                                    {
                                        toolReply = "No hay slots ocupados en el rango especificado.";
                                    }
                                    else
                                    {
                                        toolReply = string.Join("\n", occupied.Select(o => $"{o.start} - {o.end} ocupado"));
                                    }
                                    break;

                                default:
                                    toolReply = "Herramienta no reconocida.";
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            toolReply = $"Error al ejecutar la herramienta: {ex.Message}";
                        }

                        // Add tool response to messages
                        messages.Add(new { role = "tool", content = $"Resultado de {functionName}: {toolReply}" });
                    }
                }
                else
                {
                    // No more tool calls, get the final content
                    reply = message.GetProperty("content").GetString() ?? "No hay respuesta.";
                    break;
                }

                round++;
            }

            if (string.IsNullOrEmpty(reply))
            {
                reply = "Se alcanzó el límite de rondas sin una respuesta final.";
            }

            return Ok(new { reply });
        }
    }

    public class ChatRequest
    {
        public string UserMessage { get; set; } = string.Empty;
    }
}