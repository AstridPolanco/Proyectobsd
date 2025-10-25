using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

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
                using var cmd = new SqlCommand("SELECT * FROM Persona WHERE PersonaID = @id", cn);
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
                var bodyDict = JsonElementToDictionary(body);

                if (ExistsSP("sp_InsertarPersona"))
                {
                    ExecSP("sp_InsertarPersona", bodyDict);
                    return Ok("Insertado (SP) sp_InsertarPersona");
                }

                // Fallback generico
                var cols = string.Join(",", bodyDict.Keys);
                var vals = string.Join(",", bodyDict.Keys.Select(k => "@" + k));
                var sql = $"INSERT INTO Persona ({cols}) VALUES ({vals})";

                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);

                foreach (var kv in bodyDict)
                {
                    var safeValue = GetSafeParameterValue(kv.Value);
                    cmd.Parameters.AddWithValue("@" + kv.Key, safeValue);
                }

                cn.Open();
                cmd.ExecuteNonQuery();
                return Ok("Insertado (directo)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al crear persona: {ex.Message}");
            }
        }

        [HttpPut("Actualizar")]
        public IActionResult Actualizar([FromBody] JsonElement body)
        {
            try
            {
                var bodyDict = JsonElementToDictionary(body);

                if (ExistsSP("sp_ActualizarPersona"))
                {
                    ExecSP("sp_ActualizarPersona", bodyDict);
                    return Ok("Actualizado (SP) sp_ActualizarPersona");
                }

                // Fallback update: detect PersonaID
                if (!bodyDict.ContainsKey("PersonaID"))
                    return BadRequest("PersonaID requerido");

                var idName = "PersonaID";
                var idVal = bodyDict[idName];
                var setters = bodyDict.Where(kv => kv.Key != idName)
                                    .Select(kv => $"[{kv.Key}] = @{kv.Key}");
                var sql = $"UPDATE Persona SET {string.Join(",", setters)} WHERE PersonaID = @PersonaID";

                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);

                foreach (var kv in bodyDict)
                {
                    var safeValue = GetSafeParameterValue(kv.Value);
                    cmd.Parameters.AddWithValue("@" + kv.Key, safeValue);
                }

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
                if (ExistsSP("sp_EliminarPersonaIR"))
                {
                    ExecSP("sp_EliminarPersonaIR", new Dictionary<string, object> { { "PersonaID_a_Eliminar", id } });
                    return Ok("Eliminado (SP) sp_EliminarPersonaIR");
                }

                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM Persona WHERE PersonaID = @id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                var rows = cmd.ExecuteNonQuery();
                return Ok($"Eliminado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        #region Helpers (mismos métodos que en Municipio)
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

        private Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            foreach (var property in element.EnumerateObject())
            {
                object value;
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        value = property.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (property.Value.TryGetInt32(out int intVal))
                            value = intVal;
                        else if (property.Value.TryGetDecimal(out decimal decimalVal))
                            value = decimalVal;
                        else
                            value = property.Value.GetDouble();
                        break;
                    case JsonValueKind.True:
                        value = true;
                        break;
                    case JsonValueKind.False:
                        value = false;
                        break;
                    case JsonValueKind.Null:
                        value = null;
                        break;
                    default:
                        value = property.Value.ToString();
                        break;
                }
                dict[property.Name] = value;
            }
            return dict;
        }

        private object GetSafeParameterValue(object value)
        {
            if (value == null) return DBNull.Value;

            if (value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString() ?? (object)DBNull.Value;
                    case JsonValueKind.Number:
                        if (element.TryGetInt32(out int intVal))
                            return intVal;
                        if (element.TryGetDecimal(out decimal decimalVal))
                            return decimalVal;
                        return element.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return DBNull.Value;
                    default:
                        return element.ToString() ?? (object)DBNull.Value;
                }
            }

            return value ?? DBNull.Value;
        }
        #endregion
    }
}
