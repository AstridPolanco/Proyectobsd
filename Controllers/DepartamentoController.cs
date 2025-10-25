using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

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
        public IActionResult Crear([FromBody] JsonElement body)
        {
            try
            {
                // Convertir JsonElement a Dictionary manejando tipos correctamente
                var bodyDict = JsonElementToDictionary(body);

                if (ExistsSP("sp_InsertarDepartamento"))
                {
                    ExecSP("sp_InsertarDepartamento", bodyDict);
                    return Ok("Insertado (SP) sp_InsertarDepartamento");
                }

                // fallback con manejo de tipos seguro
                var sql = "INSERT INTO Departamento(DepartamentoID, Descripcion) VALUES(@DepartamentoID, @Descripcion)";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);

                // Manejar tipos explícitamente
                cmd.Parameters.AddWithValue("@DepartamentoID", GetSafeIntValue(bodyDict, "DepartamentoID"));
                cmd.Parameters.AddWithValue("@Descripcion", GetSafeStringValue(bodyDict, "Descripcion"));

                cn.Open();
                cmd.ExecuteNonQuery();
                return Ok("Insertado (directo)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al crear departamento: {ex.Message}");
            }
        }

        [HttpPut("Actualizar")]
        public IActionResult Actualizar([FromBody] JsonElement body)
        {
            try
            {
                var bodyDict = JsonElementToDictionary(body);

                if (ExistsSP("sp_ActualizarDepartamento"))
                {
                    ExecSP("sp_ActualizarDepartamento", bodyDict);
                    return Ok("Actualizado (SP) sp_ActualizarDepartamento");
                }

                // fallback update
                var sql = "UPDATE Departamento SET Descripcion = @Descripcion WHERE DepartamentoID = @DepartamentoID";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Descripcion", GetSafeStringValue(bodyDict, "Descripcion"));
                cmd.Parameters.AddWithValue("@DepartamentoID", GetSafeIntValue(bodyDict, "DepartamentoID"));
                cn.Open();
                var rows = cmd.ExecuteNonQuery();
                return Ok($"Actualizado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("Eliminar")]
        public IActionResult Eliminar([FromQuery] int id)
        {
            try
            {
                if (ExistsSP("sp_EliminarDepartamentoIR"))
                {
                    ExecSP("sp_EliminarDepartamentoIR", new Dictionary<string, object> { { "DepartamentoID_a_Eliminar", id } });
                    return Ok("Eliminado (SP) sp_EliminarDepartamentoIR");
                }
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM Departamento WHERE DepartamentoID = @id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                var rows = cmd.ExecuteNonQuery();
                return Ok($"Eliminado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        #region helpers mejorados
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
            foreach (var kv in pars)
            {
                // Manejar tipos específicos para evitar errores de serialización
                var safeValue = GetSafeParameterValue(kv.Value);
                cmd.Parameters.AddWithValue("@" + kv.Key, safeValue);
            }
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        private List<Dictionary<string, object>> DataTableToList(DataTable table)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var d = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                    d[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                list.Add(d);
            }
            return list;
        }

        // Nuevos métodos para manejar serialización segura
        // Método simplificado para convertir JsonElement
        private Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.TryGetInt32(out int intVal) ? intVal : property.Value.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => property.Value.ToString() // Simplemente convertir a string
                };
            }
            return dict;
        }

        private object GetSafeParameterValue(object value)
        {
            if (value == null) return DBNull.Value;

            // Manejar JsonElement si todavía existe
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString() ?? (object)DBNull.Value,
                    JsonValueKind.Number => element.TryGetInt32(out int intVal) ? intVal : element.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => DBNull.Value,
                    _ => element.ToString() // Sin DBNull.Value aquí
                };
            }

            return value ?? DBNull.Value;
        }

        private object GetSafeIntValue(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                if (dict[key] is int intVal) return intVal;
                if (dict[key] is long longVal) return (int)longVal;
                if (int.TryParse(dict[key].ToString(), out int parsedVal)) return parsedVal;
            }
            return DBNull.Value;
        }

        private object GetSafeStringValue(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
                return dict[key].ToString() ?? (object)DBNull.Value;
            return DBNull.Value;
        }
        #endregion
    }
}
