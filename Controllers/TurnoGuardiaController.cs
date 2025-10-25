using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TurnoGuardiaController : ControllerBase
    {
        private readonly IConfiguration _config;
        public TurnoGuardiaController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT TOP (1000) * FROM TurnoGuardia", cn);
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
                using var cmd = new SqlCommand("SELECT * FROM TurnoGuardia WHERE TurnoID = @id", cn);
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

                if (ExistsSP("sp_InsertarTurnoGuardia"))
                {
                    ExecSP("sp_InsertarTurnoGuardia", bodyDict);
                    return Ok("Insertado (SP) sp_InsertarTurnoGuardia");
                }

                var sql = "INSERT INTO TurnoGuardia(TurnoID, GaritaID, GuardiaID, FechaInicio, FechaFin, Observaciones) VALUES(@TurnoID, @GaritaID, @GuardiaID, @FechaInicio, @FechaFin, @Observaciones)";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);

                cmd.Parameters.AddWithValue("@TurnoID", GetSafeIntValue(bodyDict, "TurnoID"));
                cmd.Parameters.AddWithValue("@GaritaID", GetSafeIntValue(bodyDict, "GaritaID"));
                cmd.Parameters.AddWithValue("@GuardiaID", GetSafeIntValue(bodyDict, "GuardiaID"));
                cmd.Parameters.AddWithValue("@FechaInicio", GetSafeIntValue(bodyDict, "FechaInicio"));
                cmd.Parameters.AddWithValue("@FechaFin", GetSafeIntValue(bodyDict, "FechaFin"));
                cmd.Parameters.AddWithValue("@Observaciones", GetSafeIntValue(bodyDict, "Observaciones"));
          

                cn.Open();
                cmd.ExecuteNonQuery();
                return Ok("Insertado (directo)");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al crear turnoguardia: {ex.Message}");
            }
        }

        [HttpPut("Actualizar")]
        public IActionResult Actualizar([FromBody] JsonElement body)
        {
            try
            {
                var bodyDict = JsonElementToDictionary(body);

                if (ExistsSP("sp_ActualizarTurnoGuardia"))
                {
                    ExecSP("sp_ActualizarTurnoGuardia", bodyDict);
                    return Ok("Actualizado (SP) sp_ActualizarTurnoGuardia");
                }

                var sql = "UPDATE TurnoGuardia SET GaritaID, GuardiaID, FechaInicio, FechaFin, Observaciones  =  @GaritaID, @GuardiaID, @FechaInicio, @FechaFin, @Observaciones WHERE TurnoID = @TurnoID";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);

                
                cmd.Parameters.AddWithValue("@GaritaID", GetSafeIntValue(bodyDict, "GaritaID"));
                cmd.Parameters.AddWithValue("@GuardiaID", GetSafeIntValue(bodyDict, "GuardiaID"));
                cmd.Parameters.AddWithValue("@FechaInicio", GetSafeIntValue(bodyDict, "FechaInicio"));
                cmd.Parameters.AddWithValue("@FechaFin", GetSafeIntValue(bodyDict, "FechaFin"));
                cmd.Parameters.AddWithValue("@Observaciones", GetSafeIntValue(bodyDict, "Observaciones"));
                cmd.Parameters.AddWithValue("@TurnoID", GetSafeIntValue(bodyDict, "TurnoID"));

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
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM TurnoGuardia WHERE TurnoID = @id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                var rows = cmd.ExecuteNonQuery();
                return Ok($"Eliminado ({rows} filas)");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        #region Helpers
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