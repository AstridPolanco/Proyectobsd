using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace ARSAN_FAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActualizacionesController : ControllerBase
    {
        private readonly IConfiguration _config;
        public ActualizacionesController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        // PUT api/Actualizaciones/Exec/sp_ActualizarPersona
        [HttpPut("Exec/{procName}")]
        public IActionResult ExecActualizar(string procName, [FromBody] JObject body)
        {
            try
            {
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(procName, cn) { CommandType = CommandType.StoredProcedure };
                foreach (var p in body.Properties())
                {
                    cmd.Parameters.AddWithValue("@" + p.Name, p.Value.Type == JTokenType.Null ? DBNull.Value : p.Value.ToObject<object>());
                }
                cn.Open();
                cmd.ExecuteNonQuery();
                return Ok($"Ejecutado {procName}");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }
    }
}


