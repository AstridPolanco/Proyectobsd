using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ARSAN_FAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcedimientosController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ProcedimientosController(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        [HttpGet("{procedimiento}")]
        public IActionResult EjecutarProcedimiento(string procedimiento, [FromQuery] Dictionary<string, string>? parametros)
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
                        cmd.Parameters.AddWithValue("@" + p.Key, string.IsNullOrEmpty(p.Value) ? DBNull.Value : p.Value);
                }

                var adapter = new SqlDataAdapter(cmd);
                var table = new DataTable();
                adapter.Fill(table);

                var result = new List<Dictionary<string, object>>();
                foreach (DataRow row in table.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in table.Columns)
                        dict[col.ColumnName] = row[col];
                    result.Add(dict);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al ejecutar {procedimiento}: {ex.Message}");
            }
        }
    }
}

