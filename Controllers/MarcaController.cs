using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarcaController : ControllerBase
    {
        private readonly IConfiguration _config;
        public MarcaController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")] public IActionResult Listar() => Query("SELECT * FROM Marca");
        [HttpGet("Get")] public IActionResult Get(int id) => Query($"SELECT * FROM Marca WHERE MarcaID={id}");
        [HttpPost("Crear")] public IActionResult Crear([FromBody] JsonElement body) => Insert(body, "Marca");
        [HttpPut("Actualizar")] public IActionResult Actualizar([FromBody] JsonElement body) => Update(body, "Marca", "MarcaID");
        [HttpDelete("Eliminar")] public IActionResult Eliminar(int id) => Delete("Marca", "MarcaID", id);

        private IActionResult Query(string sql)
        {
            try
            {
                using var cn = new SqlConnection(Conn);
                using var da = new SqlDataAdapter(sql, cn);
                var dt = new DataTable(); da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private IActionResult Insert(JsonElement body, string tabla)
        {
            try
            {
                var dict = JsonToDict(body);
                var cols = string.Join(",", dict.Keys);
                var vals = string.Join(",", dict.Keys.Select(k => "@" + k));
                var sql = $"INSERT INTO {tabla} ({cols}) VALUES ({vals})";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                foreach (var kv in dict) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok($"{tabla} insertado correctamente");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private IActionResult Update(JsonElement body, string tabla, string pk)
        {
            try
            {
                var dict = JsonToDict(body);
                if (!dict.ContainsKey(pk)) return BadRequest($"{pk} requerido");
                var sets = string.Join(",", dict.Where(kv => kv.Key != pk).Select(kv => $"{kv.Key}=@{kv.Key}"));
                var sql = $"UPDATE {tabla} SET {sets} WHERE {pk}=@{pk}";
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand(sql, cn);
                foreach (var kv in dict) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok($"{tabla} actualizado correctamente");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private IActionResult Delete(string tabla, string pk, int id)
        {
            try
            {
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand($"DELETE FROM {tabla} WHERE {pk}=@id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok($"{tabla} eliminado correctamente");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
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
