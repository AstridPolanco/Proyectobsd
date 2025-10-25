using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartamentoController : ControllerBase
    {
        private readonly IConfiguration _config;
        public DepartamentoController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT TOP (1000) * FROM Departamento", cn);
                using var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
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
                using var cmd = new SqlCommand("SELECT * FROM Departamento WHERE DepartamentoID = @id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                using var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("Crear")]
        public IActionResult Crear([FromBody] Dictionary<string, object> body)
        {
            try
            {
                // prefer SP if exists
                if (ExistsSP("sp_InsertarDepartamento"))
                {
                    ExecSP("sp_InsertarDepartamento", body);
                    return Ok("Insertado (SP) sp_InsertarDepartamento");
                }
                // fallback
                var sql = "INSERT INTO Departamento(DepartamentoID, Descripcion) VALUES(@DepartamentoID, @Descripcion)";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@DepartamentoID", body["DepartamentoID"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Descripcion", body["Descripcion"] ?? DBNull.Value);
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
                if (ExistsSP("sp_ActualizarDepartamento"))
                {
                    ExecSP("sp_ActualizarDepartamento", body);
                    return Ok("Actualizado (SP) sp_ActualizarDepartamento");
                }
                // fallback update
                var id = body["DepartamentoID"];
                var sql = "UPDATE Departamento SET Descripcion = @Descripcion WHERE DepartamentoID = @DepartamentoID";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Descripcion", body["Descripcion"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DepartamentoID", id);
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
                // try IR sp if exists
                if (ExistsSP("sp_EliminarDepartamentoIR"))
                {
                    ExecSP("sp_EliminarDepartamentoIR", new Dictionary<string, object> { { "DepartamentoID_a_Eliminar", id } });
                    return Ok("Eliminado (SP) sp_EliminarDepartamentoIR");
                }
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM Departamento WHERE DepartamentoID = @id", cn);
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
            cn.Open();
            return ((int)cmd.ExecuteScalar()) > 0;
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
