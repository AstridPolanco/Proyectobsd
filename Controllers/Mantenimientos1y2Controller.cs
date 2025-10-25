using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ARSAN_FAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Mantenimientos1y2Controller : ControllerBase
    {
        private readonly IConfiguration _config;

        public Mantenimientos1y2Controller(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection GetConnection() => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        [HttpPost("{procedimiento}")]
        public IActionResult EjecutarMantenimiento(string procedimiento, [FromBody] Dictionary<string, object> parametros)
        {
            try
            {
                using var conn = GetConnection();
                using var cmd = new SqlCommand(procedimiento, conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parametros != null)
                {
                    foreach (var p in parametros)
                        cmd.Parameters.AddWithValue("@" + p.Key, p.Value ?? DBNull.Value);
                }

                conn.Open();
                cmd.ExecuteNonQuery();

                return Ok($"Procedimiento {procedimiento} ejecutado correctamente.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error ejecutando {procedimiento}: {ex.Message}");
            }
        }
    }
}

