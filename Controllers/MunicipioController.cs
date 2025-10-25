using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MunicipioController : ControllerBase
    {
        private readonly IConfiguration _config;
        public MunicipioController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT TOP (1000) * FROM Municipio", cn);
                using var da = new SqlDataAdapter(cmd); da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("Get")]
        public IActionResult Get(int id, int? deptoId)
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT * FROM Municipio WHERE MunicipioID = @id" + (deptoId.HasValue ? " AND DepartamentoID = @dep" : ""), cn);
                cmd.Parameters.AddWithValue("@id", id);
                if (deptoId.HasValue) cmd.Parameters.AddWithValue("@dep", deptoId.Value);
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
                if (ExistsSP("sp_InsertarMunicipio")) { ExecSP("sp_InsertarMunicipio", body); return Ok("Insertado (SP)"); }
                var sql = "INSERT INTO Municipio(MunicipioID, Descripcion, DepartamentoID) VALUES(@MunicipioID,@Descripcion,@DepartamentoID)";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@MunicipioID", body["MunicipioID"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Descripcion", body["Descripcion"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DepartamentoID", body["DepartamentoID"] ?? DBNull.Value);
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
                if (ExistsSP("sp_ActualizarMunicipio")) { ExecSP("sp_ActualizarMunicipio", body); return Ok("Actualizado (SP)"); }
                var sql = "UPDATE Municipio SET Descripcion=@Descripcion WHERE MunicipioID=@MunicipioID AND DepartamentoID=@DepartamentoID";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Descripcion", body["Descripcion"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MunicipioID", body["MunicipioID"]);
                cmd.Parameters.AddWithValue("@DepartamentoID", body["DepartamentoID"]);
                cn.Open(); var rows = cmd.ExecuteNonQuery();
                return Ok($"Actualizado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("Eliminar")]
        public IActionResult Eliminar([FromQuery] int id, [FromQuery] int? departamentoId)
        {
            try
            {
                if (ExistsSP("sp_EliminarMunicipioIR"))
                {
                    ExecSP("sp_EliminarMunicipioIR", new Dictionary<string, object> { { "MunicipioID_a_Eliminar", id } });
                    return Ok("Eliminado (SP) sp_EliminarMunicipioIR");
                }
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM Municipio WHERE MunicipioID=@id" + (departamentoId.HasValue ? " AND DepartamentoID=@dep" : ""), cn);
                cmd.Parameters.AddWithValue("@id", id);
                if (departamentoId.HasValue) cmd.Parameters.AddWithValue("@dep", departamentoId.Value);
                cn.Open(); var rows = cmd.ExecuteNonQuery();
                return Ok($"Eliminado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        #region helpers (same as Departamento)
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
