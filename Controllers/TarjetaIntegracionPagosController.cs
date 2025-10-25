using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace ARSAN_FAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetaIntegracionPagosController : ControllerBase
    {
        private readonly IConfiguration _config;
        public TarjetaIntegracionPagosController(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        [HttpGet("Listar")] public IActionResult Listar() => Query("SELECT * FROM TarjetaIntegracionPagos");
        [HttpGet("Get")] public IActionResult Get(int id) => Query($"SELECT * FROM TarjetaIntegracionPagos WHERE TarjetaIntegracionPagosID={id}");
        [HttpPost("Crear")] public IActionResult Crear([FromBody] JsonElement b) => Insert(b, "TarjetaIntegracionPagos");
        [HttpPut("Actualizar")] public IActionResult Actualizar([FromBody] JsonElement b) => Update(b, "TarjetaIntegracionPagos", "TarjetaIntegracionPagosID");
        [HttpDelete("Eliminar")] public IActionResult Eliminar(int id) => Delete("TarjetaIntegracionPagos", "TarjetaIntegracionPagosID", id);

        private IActionResult Query(string sql)
        {
            try
            {
                using var cn = new SqlConnection(Conn);
                using var da = new SqlDataAdapter(sql, cn);
                var dt = new DataTable(); da.Fill(dt);
                return Ok(ToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private IActionResult Insert(JsonElement b, string t)
        {
            try
            {
                var d = ToDict(b);
                var cols = string.Join(",", d.Keys);
                var vals = string.Join(",", d.Keys.Select(k => "@" + k));
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand($"INSERT INTO {t} ({cols}) VALUES ({vals})", cn);
                foreach (var kv in d) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok($"{t} insertado correctamente");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private IActionResult Update(JsonElement b, string t, string pk)
        {
            try
            {
                var d = ToDict(b);
                if (!d.ContainsKey(pk)) return BadRequest($"{pk} requerido");
                var sets = string.Join(",", d.Where(kv => kv.Key != pk).Select(kv => $"{kv.Key}=@{kv.Key}"));
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand($"UPDATE {t} SET {sets} WHERE {pk}=@{pk}", cn);
                foreach (var kv in d) cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok($"{t} actualizado correctamente");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private IActionResult Delete(string t, string pk, int id)
        {
            try
            {
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand($"DELETE FROM {t} WHERE {pk}=@id", cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open(); cmd.ExecuteNonQuery();
                return Ok($"{t} eliminado correctamente");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private static List<Dictionary<string, object>> ToList(DataTable t)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow r in t.Rows)
            {
                var d = new Dictionary<string, object>();
                foreach (DataColumn c in t.Columns) d[c.ColumnName] = r[c] == DBNull.Value ? null : r[c];
                list.Add(d);
            }
            return list;
        }

        private static Dictionary<string, object> ToDict(JsonElement e)
        {
            var d = new Dictionary<string, object>();
            foreach (var p in e.EnumerateObject())
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
