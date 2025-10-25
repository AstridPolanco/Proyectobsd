using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Data;

namespace ARSAN_FAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MantenimientosCRUDController : ControllerBase
    {
        private readonly IConfiguration _config;
        public MantenimientosCRUDController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        // ----------- CONSULTAR POR ID -----------
        [HttpGet("Consultar/{entity}/{id}")]
        public IActionResult Consultar(string entity, int id)
        {
            var sp = $"sp_Consultar{entity}PorID";
            return Ok(ExecSP(sp, new Dictionary<string, object> { { $"{entity}ID", id } }));

        }

        // ----------- INSERTAR -----------
        [HttpPost("Insertar/{entity}")]
        public IActionResult Insertar(string entity, [FromBody] JObject data)
        {
            var sp = $"sp_Insertar{entity}";
            var pars = JObjectToPars(data);
            ExecSP(sp, pars);
            return Ok($"Insertado correctamente ({entity})");
        }

        // ----------- ACTUALIZAR -----------
        [HttpPut("Actualizar/{entity}")]
        public IActionResult Actualizar(string entity, [FromBody] JObject data)
        {
            var sp = $"sp_Actualizar{entity}";
            var pars = JObjectToPars(data);
            ExecSP(sp, pars);
            return Ok($"Actualizado correctamente ({entity})");
        }

        // ----------- ELIMINAR -----------
        [HttpDelete("Eliminar/{entity}/{id}")]
        public IActionResult Eliminar(string entity, int id)
        {
            string sp = entity.Equals("Persona", StringComparison.OrdinalIgnoreCase)
                ? "sp_EliminarPersonaIR"
                : $"sp_Eliminar{entity}";
            var pars = new Dictionary<string, object>
            {
                { entity.Equals("Persona") ? "PersonaID_a_Eliminar" : $"{entity}ID", id }
            };
            ExecSP(sp, pars);
            return Ok($"Eliminado correctamente ({entity})");
        }

        // ----------- HELPER -----------
        private DataTable ExecSP(string sp, IDictionary<string, object> pars)
        {
            using var cn = new SqlConnection(Conn);
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            foreach (var kv in pars) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        private Dictionary<string, object> JObjectToPars(JObject j)
        {
            var d = new Dictionary<string, object>();
            foreach (var p in j.Properties())
                d[p.Name] = p.Value.Type == JTokenType.Null ? DBNull.Value : p.Value.ToObject<object>();
            return d;
        }
    }
}
