using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonaController : ControllerBase
    {
        private readonly IConfiguration _config;
        public PersonaController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT TOP (1000) * FROM Persona", cn);
                using var da = new SqlDataAdapter(cmd); da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("Get")]
        public IActionResult Get(int id)
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT * FROM Persona WHERE PersonaID = @id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                using var da = new SqlDataAdapter(cmd); da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("Crear")]
        public IActionResult Crear([FromBody] Dictionary<string, object> body)
        {
            try
            {
                if (ExistsSP("sp_InsertarPersona")) { ExecSP("sp_InsertarPersona", body); return Ok("Insertado (SP) sp_InsertarPersona"); }
                // fallback generic
                var cols = string.Join(",", body.Keys);
                var vals = string.Join(",", body.Keys.Select(k => "@" + k));
                var sql = $"INSERT INTO Persona ({cols}) VALUES ({vals})";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                foreach (var kv in body) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok("Insertado (directo)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPut("Actualizar")]
        public IActionResult Actualizar([FromBody] Dictionary<string, object> body)
        {
            try
            {
                if (ExistsSP("sp_ActualizarPersona")) { ExecSP("sp_ActualizarPersona", body); return Ok("Actualizado (SP) sp_ActualizarPersona"); }
                // fallback update: detect PersonaID
                if (!body.ContainsKey("PersonaID")) return BadRequest("PersonaID requerido");
                var idName = "PersonaID";
                var idVal = body[idName];
                var setters = body.Where(kv => kv.Key != idName).Select(kv => $"[{kv.Key}] = @{kv.Key}");
                var sql = $"UPDATE Persona SET {string.Join(",", setters)} WHERE PersonaID = @PersonaID";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                foreach (var kv in body) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); var rows = cmd.ExecuteNonQuery();
                return Ok($"Actualizado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("Eliminar")]
        public IActionResult Eliminar([FromQuery] int id)
        {
            try
            {
                if (ExistsSP("sp_EliminarPersonaIR"))
                {
                    ExecSP("sp_EliminarPersonaIR", new Dictionary<string, object> { { "PersonaID_a_Eliminar", id } });
                    return Ok("Eliminado (SP) sp_EliminarPersonaIR");
                }
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM Persona WHERE PersonaID=@id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open(); var rows = cmd.ExecuteNonQuery();
                return Ok($"Eliminado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        #region helpers
        private bool ExistsSP(string spName)
        {
            using var cn = new SqlConnection(Conn);
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.procedures WHERE name = @name", cn);
            cmd.Parameters.AddWithValue("@name", spName);
            cn.Open(); return ((int)cmd.ExecuteScalar()) > 0;
        }
        private void ExecSP(string spName, IDictionary<string, object> pars)
        {
            using var cn = new SqlConnection(Conn);
            using var cmd = new SqlCommand(spName, cn) { CommandType = CommandType.StoredProcedure };
            foreach (var kv in pars) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
            cn.Open(); cmd.ExecuteNonQuery();
        }
        private List<Dictionary<string, object>> DataTableToList(DataTable table)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var d = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns) d[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                list.Add(d);
            }
            return list;
        }
        #endregion
    }
}
