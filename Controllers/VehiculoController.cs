using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculoController : ControllerBase
    {
        private readonly IConfiguration _config;
        public VehiculoController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var da = new SqlDataAdapter("SELECT * FROM Vehiculo", cn);
                da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCodes(500, ex.Message); }
        }

        private IActionResult Ok(List<Dictionary<string, object>> list)
        {
            throw new NotImplementedException();
        }

        [HttpGet("Get")]
        public IActionResult Get(int id)
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("SELECT * FROM Vehiculo WHERE VehiculoID = @id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                using var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCodes(500, ex.Message); }
        }

        [HttpPost("Crear")]
        public IActionResult Crear([FromBody] JsonElement body)
        {
            try
            {
                var dict = JsonToDict(body);
                var cols = string.Join(",", dict.Keys);
                var vals = string.Join(",", dict.Keys.Select(k => "@" + k));
                var sql = $"INSERT INTO Vehiculo ({cols}) VALUES ({vals})";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                foreach (var kv in dict) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok("Vehículo insertado correctamente");
            }
            catch (Exception ex) { return StatusCodes(500, ex.Message); }
        }

        [HttpPut("Actualizar")]
        public IActionResult Actualizar([FromBody] JsonElement body)
        {
            try
            {
                var dict = JsonToDict(body);
                if (!dict.ContainsKey("VehiculoID")) return BadRequest("VehiculoID requerido");
                var sets = string.Join(",", dict.Where(kv => kv.Key != "VehiculoID").Select(kv => $"{kv.Key}=@{kv.Key}"));
                var sql = $"UPDATE Vehiculo SET {sets} WHERE VehiculoID=@VehiculoID";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                foreach (var kv in dict) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok("Vehículo actualizado correctamente");
            }
            catch (Exception ex) { return StatusCodes(500, ex.Message); }
        }

        private IActionResult BadRequest(string v)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("Eliminar")]
        public IActionResult Eliminar(int id)
        {
            try
            {
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand("DELETE FROM Vehiculo WHERE VehiculoID=@id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok("Vehículo eliminado correctamente");
            }
            catch (Exception ex) { return StatusCodes(500, ex.Message); }
        }

        private IActionResult Ok(string v)
        {
            throw new NotImplementedException();
        }

        private IActionResult StatusCodes(int v, string message)
        {
            throw new NotImplementedException();
        }

        private static List<Dictionary<string, object>> DataTableToList(DataTable t)
        {
            var l = new List<Dictionary<string, object>>();
            foreach (DataRow r in t.Rows)
            {
                var d = new Dictionary<string, object>();
                foreach (DataColumn c in t.Columns) d[c.ColumnName] = r[c] == DBNull.Value ? null : r[c];
                l.Add(d);
            }
            return l;
        }

        private static Dictionary<string, object> JsonToDict(JsonElement el)
        {
            var d = new Dictionary<string, object>();
            foreach (var p in el.EnumerateObject())
                d[p.Name] = p.Value.ValueKind switch
                {
                    JsonValueKind.String => p.Value.GetString(),
                    JsonValueKind.Number => p.Value.TryGetInt32(out var i) ? i : p.Value.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null
                };
            return d;
        }
    }
}
