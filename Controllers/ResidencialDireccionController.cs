using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResidencialDireccionController : ControllerBase
    {
        private readonly IConfiguration _config;
        public ResidencialDireccionController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT TOP (1000) * FROM ResidencialDireccion", cn);
                using var da = new SqlDataAdapter(cmd); da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("Get")]
        public IActionResult Get(int id, int? residencialId)
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT * FROM ResidencialDireccion WHERE ResidencialDireccionID = @id" + (residencialId.HasValue ? " AND ResidencialID=@res" : ""), cn);
                cmd.Parameters.AddWithValue("@id", id);
                if (residencialId.HasValue) cmd.Parameters.AddWithValue("@res", residencialId.Value);
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
                if (ExistsSP("sp_InsertarResidencialDireccion")) { ExecSP("sp_InsertarResidencialDireccion", body); return Ok("Insertado (SP)"); }
                // fallback - build INSERT generically if needed (here we assume all fields passed)
                var cols = string.Join(",", body.Keys);
                var vals = string.Join(",", body.Keys.Select(k => "@" + k));
                var sql = $"INSERT INTO ResidencialDireccion ({cols}) VALUES ({vals})";
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
                if (ExistsSP("sp_ActualizarResidencialDireccion")) { ExecSP("sp_ActualizarResidencialDireccion", body); return Ok("Actualizado (SP)"); }
                var id = body["ResidencialDireccionID"];
                var resid = body["ResidencialID"];
                var setters = body.Keys.Where(k => k != "ResidencialDireccionID" && k != "ResidencialID").Select(k => $"[{k}] = @{k}");
                var sql = $"UPDATE ResidencialDireccion SET {string.Join(",", setters)} WHERE ResidencialDireccionID=@ResidencialDireccionID AND ResidencialID=@ResidencialID";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                foreach (var kv in body)
                {
                    cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                }
                cn.Open(); var rows = cmd.ExecuteNonQuery();
                return Ok($"Actualizado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("Eliminar")]
        public IActionResult Eliminar([FromQuery] int id, [FromQuery] int? residencialId)
        {
            try
            {
                if (ExistsSP("sp_EliminarResidencialDireccionIR"))
                {
                    ExecSP("sp_EliminarResidencialDireccionIR", new Dictionary<string, object> { { "ResidencialDireccionID_a_Eliminar", id } });
                    return Ok("Eliminado (SP)");
                }
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM ResidencialDireccion WHERE ResidencialDireccionID=@id" + (residencialId.HasValue ? " AND ResidencialID=@res" : ""), cn);
                cmd.Parameters.AddWithValue("@id", id);
                if (residencialId.HasValue) cmd.Parameters.AddWithValue("@res", residencialId.Value);
                cn.Open(); var rows = cmd.ExecuteNonQuery();
                return Ok($"Eliminado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        #region helpers (same as previous)
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

