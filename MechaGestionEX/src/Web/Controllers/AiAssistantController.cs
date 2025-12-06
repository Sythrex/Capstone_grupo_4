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
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

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

            string systemPrompt = @"
                Eres un asistente AI amigable y eficiente para clientes de un taller mecánico. Tu rol es ayudar con tareas como: 
                - Actualizar datos personales (nombre, correo, etc.).
                - Listar, agregar, actualizar o eliminar vehículos.
                - Verificar disponibilidad de slots y agendar atenciones, sugiriendo alternativas si está ocupado.
                - Siempre responde en español simple, directo y conversacional.

                La fecha actual es 5 de diciembre de 2025. Usa esta fecha para calcular fechas relativas como 'mañana', 'próximo martes', etc. Por ejemplo, si hoy es viernes 5 de diciembre de 2025, el próximo martes es el 9 de diciembre de 2025.

                Instrucciones clave:
                1. **Mantén el contexto**: Recuerda la conversación anterior. Por ejemplo, si el usuario menciona un vehículo en un mensaje previo, úsalo en el siguiente sin repetir preguntas innecesarias.
                2. **Parseo de fechas**: Si el usuario da fechas en lenguaje natural (e.g., 'mañana a las 10am', 'el próximo lunes', '15 de diciembre 2025'), conviértelas a formato ISO (YYYY-MM-DDTHH:MM:SS) antes de llamar a herramientas. Asume zona horaria local (Chile) y hora actual si no se especifica. Usa lógica para inferir (e.g., 'mañana' = fecha actual +1 día).
                3. **Confirmaciones**: Para cualquier acción (actualizar, agregar, eliminar, agendar), resume los parámetros y pide confirmación explícita del usuario antes de ejecutar la herramienta. Si confirma, procede; si no, ajusta.
                4. **Uso de herramientas**: 
                   - Usa herramientas solo cuando sea necesario y los parámetros estén completos y confirmados.
                   - Procesa los resultados de las herramientas antes de responder: e.g., si un slot está ocupado, usa 'check_occupied_slots' para encontrar alternativas y sugiérelas.
                   - Si no hay vehículos para agendar, sugiere agregar uno primero.
                5. **Razona paso a paso**: Antes de responder o llamar a una herramienta, piensa: ¿Qué quiere el usuario? ¿Falta información? ¿Necesito confirmar? ¿Hay contexto previo?
                6. **Manejo de errores**: Si una herramienta falla, explica amigablemente y ofrece opciones. Para consultas desconocidas o indebidas, rechaza cordialmente y ofrece contactar al mecánico.
                7. **Ejemplos implícitos**: Para agendar: Usuario dice 'agenda para mañana', tú: '¿Para qué vehículo y hora? ¿Observaciones?'. Luego confirma y verifica slots.
                8. **Limites**: No hagas nada ilegal o inapropiado. Mantén respuestas concisas pero útiles.";

            var tools = new List<object>
            {
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "update_personal_data",
                        description = "Modificar datos personales del cliente. Requiere confirmación.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                rut = new { type = "string", description = "RUT del cliente" },
                                nombre = new { type = "string", description = "Nombre completo" },
                                correo = new { type = "string", description = "Correo electrónico" },
                                telefono = new { type = "string", description = "Número de teléfono" },
                                direccion = new { type = "string", description = "Dirección física" },
                                comunaId = new { type = "integer", description = "ID de la comuna" }
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
                        description = "Listar vehículos del cliente. Útil para seleccionar uno en agendamientos.",
                        parameters = new { type = "object", properties = new { } }
                    }
                },
                new
                {
                    type = "function",
                    function = new
                    {
                        name = "add_vehicle",
                        description = "Agregar un nuevo vehículo. Requiere confirmación.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                patente = new { type = "string", description = "Patente del vehículo" },
                                vin = new { type = "string", description = "VIN (opcional)" },
                                anio = new { type = "integer", description = "Año de fabricación" },
                                kilometraje = new { type = "integer", description = "Kilometraje actual" },
                                color = new { type = "string", description = "Color" },
                                tipoId = new { type = "integer", description = "ID del tipo de vehículo (e.g., 1=Auto, 2=Moto)" }
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
                        description = "Modificar un vehículo existente. Requiere confirmación.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                id = new { type = "integer", description = "ID del vehículo" },
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
                        description = "Eliminar un vehículo. Requiere doble confirmación por ser destructivo.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                id = new { type = "integer", description = "ID del vehículo" }
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
                        description = "Agendar una atención después de verificar slots. Usa check_occupied_slots primero si es necesario. Requiere confirmación.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                fechaAgenda = new { type = "string", description = "Fecha y hora en ISO (YYYY-MM-DDTHH:MM:SS). Tú parseas del input del usuario." },
                                observaciones = new { type = "string", description = "Observaciones o comentarios" },
                                vehiculoId = new { type = "integer", description = "ID del vehículo (de list_vehicles)" }
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
                        description = "Verificar slots ocupados en un rango de fechas. Usa para sugerir alternativas. Parsea fechas a ISO.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                start = new { type = "string", description = "Fecha inicio en ISO" },
                                end = new { type = "string", description = "Fecha fin en ISO" }
                            },
                            required = new[] { "start", "end" }
                        }
                    }
                }
            };

            var sessionKey = $"ChatHistory_{clienteId}";
            var messages = HttpContext.Session.Get<List<object>>(sessionKey) ?? new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            messages.Add(new { role = "user", content = request.UserMessage });

            string reply = "";
            const int maxRounds = 10;
            int round = 0;

            var client = _httpClientFactory.CreateClient("xAIClient");

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
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: {errorContent}");
                    return StatusCode((int)response.StatusCode, $"Error en la API de xAI: {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
                var choice = result.RootElement.GetProperty("choices")[0];
                var message = choice.GetProperty("message");

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

                        using var argsDocument = JsonDocument.Parse(argsJson ?? "{}");
                        var argsRoot = argsDocument.RootElement;

                        string toolReply = "";
                        try
                        {
                            switch (functionName)
                            {
                                case "update_personal_data":
                                    var datosModel = new MisDatosViewModel
                                    {
                                        Rut = argsRoot.TryGetProperty("rut", out var rut) ? rut.GetString() ?? "" : "",
                                        Nombre = argsRoot.TryGetProperty("nombre", out var nombre) ? nombre.GetString() ?? "" : "",
                                        Correo = argsRoot.TryGetProperty("correo", out var correo) ? correo.GetString() ?? "" : "",
                                        Telefono = argsRoot.TryGetProperty("telefono", out var telefono) ? telefono.GetString() ?? "" : "",
                                        Direccion = argsRoot.TryGetProperty("direccion", out var direccion) ? direccion.GetString() ?? "" : "",
                                        ComunaId = argsRoot.TryGetProperty("comunaId", out var comunaId) ? comunaId.GetInt32() : 0
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
                                        Patente = argsRoot.TryGetProperty("patente", out var patente) ? patente.GetString() ?? "" : "",
                                        Vin = argsRoot.TryGetProperty("vin", out var vin) ? vin.GetString() ?? "" : "",
                                        Anio = argsRoot.TryGetProperty("anio", out var anio) ? anio.GetInt32() : 0,
                                        Kilometraje = argsRoot.TryGetProperty("kilometraje", out var kilometraje) ? kilometraje.GetInt32() : 0,
                                        Color = argsRoot.TryGetProperty("color", out var color) ? color.GetString() ?? "" : "",
                                        TipoId = argsRoot.TryGetProperty("tipoId", out var tipoId) ? tipoId.GetInt32() : 0
                                    };
                                    await _clienteService.AddVehicleAsync(addModel, clienteId);
                                    toolReply = "Vehículo agregado exitosamente.";
                                    break;

                                case "update_vehicle":
                                    var updateModel = new VehiculoViewModel
                                    {
                                        Id = argsRoot.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                                        Patente = argsRoot.TryGetProperty("patente", out var upatente) ? upatente.GetString() ?? "" : "",
                                        Vin = argsRoot.TryGetProperty("vin", out var uvin) ? uvin.GetString() ?? "" : "",
                                        Anio = argsRoot.TryGetProperty("anio", out var uanio) ? uanio.GetInt32() : 0,
                                        Kilometraje = argsRoot.TryGetProperty("kilometraje", out var ukilometraje) ? ukilometraje.GetInt32() : 0,
                                        Color = argsRoot.TryGetProperty("color", out var ucolor) ? ucolor.GetString() ?? "" : "",
                                        TipoId = argsRoot.TryGetProperty("tipoId", out var utipoId) ? utipoId.GetInt32() : 0
                                    };
                                    await _clienteService.UpdateVehicleAsync(updateModel, clienteId);
                                    toolReply = "Vehículo actualizado exitosamente.";
                                    break;

                                case "delete_vehicle":
                                    var deleteId = argsRoot.TryGetProperty("id", out var did) ? did.GetInt32() : 0;
                                    await _clienteService.DeleteVehicleAsync(deleteId, clienteId);
                                    toolReply = "Vehículo eliminado exitosamente.";
                                    break;

                                case "schedule_appointment":
                                    var agendaModel = new CrearAgendaClienteViewModel
                                    {
                                        FechaAgenda = argsRoot.TryGetProperty("fechaAgenda", out var fecha) ? DateTime.Parse(fecha.GetString() ?? DateTime.Now.ToString("o")) : DateTime.Now,
                                        Observaciones = argsRoot.TryGetProperty("observaciones", out var obs) ? obs.GetString() ?? "" : "",
                                        VehiculoId = argsRoot.TryGetProperty("vehiculoId", out var vid) ? vid.GetInt32() : 0
                                    };
                                    await _clienteService.ScheduleAppointmentAsync(agendaModel, clienteId);
                                    toolReply = "Atención agendada exitosamente.";
                                    break;

                                case "check_occupied_slots":
                                    var start = argsRoot.TryGetProperty("start", out var sdate) ? DateTime.Parse(sdate.GetString() ?? DateTime.Now.ToString("o")) : DateTime.Now;
                                    var end = argsRoot.TryGetProperty("end", out var edate) ? DateTime.Parse(edate.GetString() ?? DateTime.Now.AddDays(1).ToString("o")) : DateTime.Now.AddDays(1);
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
                            toolReply = $"Error al ejecutar la herramienta: {ex.Message}. Por favor, verifica los parámetros.";
                            Console.WriteLine($"Tool Error: {ex}");
                        }

                        messages.Add(new { role = "tool", content = $"Resultado de {functionName}: {toolReply}" });
                    }
                }
                else
                {
                    reply = message.GetProperty("content").GetString() ?? "No hay respuesta.";
                    break;
                }

                round++;
            }

            if (string.IsNullOrEmpty(reply))
            {
                reply = "Se alcanzó el límite de rondas sin una respuesta final. Intenta reformular tu consulta.";
            }

            if (messages.Count > 20)
            {
                messages = messages.TakeLast(20).ToList();
            }
            HttpContext.Session.Set(sessionKey, messages);

            return Ok(new { reply });
        }
    }

    public class ChatRequest
    {
        public string UserMessage { get; set; } = string.Empty;
    }


    public static class SessionExtensions
    {
        public static T Get<T>(this ISession session, string key)
        {
            var data = session.GetString(key);
            return data == null ? default : JsonSerializer.Deserialize<T>(data);
        }

        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }
    }
}