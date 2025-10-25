using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json.Linq;

namespace ARSAN_FAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Mantenimientos4Controller : ControllerBase
    {
        private readonly IConfiguration _config;
        public Mantenimientos4Controller(IConfiguration config) { _config = config; }
        private string Conn => _config.GetConnectionString("DefaultConnection");

        // 🔹 LISTAR
        [HttpGet("Listar{entity}")]
        public IActionResult Listar(string entity)
        {
            try
            {
                var dt = new DataTable();
                using var cn = new SqlConnection(Conn);
                using var cmd = new SqlCommand($"SELECT TOP (1000) * FROM [{entity}]", cn);
                using var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        // 🔹 BUSCAR POR ID o FILTRO
        [HttpGet("Buscar{entity}")]
        public IActionResult Buscar(string entity)
        {
            try
            {
                var q = Request.Query;
                if (q.ContainsKey("id"))
                {
                    var id = q["id"].ToString();
                    var spName = $"sp_Consultar{entity}PorID";

                    if (ExistsSP(spName))
                    {
                        var pars = new Dictionary<string, object> { { $"{entity}ID", id } };
                        var dt = ExecSP(spName, pars);
                        return Ok(DataTableToList(dt));
                    }
                    else
                    {
                        var dt = new DataTable();
                        using var cn = new SqlConnection(Conn);
                        using var cmd = new SqlCommand($"SELECT * FROM [{entity}] WHERE [{entity}ID] = @id", cn);
                        cmd.Parameters.AddWithValue("@id", id);
                        using var da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                        return Ok(DataTableToList(dt));
                    }
                }
                else if (q.ContainsKey("q"))
                {
                    var qval = q["q"].ToString();
                    var dt = new DataTable();
                    using var cn = new SqlConnection(Conn);
                    string sql = $"SELECT TOP (500) * FROM [{entity}] WHERE ";
                    var cols = new[] { "Descripcion", "NombreResidencial", "PrimerNombre", "PrimerApellido", "Placa" };
                    sql += string.Join(" OR ", cols.Select(c => $"[{c}] LIKE @q"));
                    using var cmd = new SqlCommand(sql, cn);
                    cmd.Parameters.AddWithValue("@q", $"%{qval}%");
                    using var da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                    return Ok(DataTableToList(dt));
                }

                return BadRequest("Parámetro 'id' o 'q' requerido.");
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        // 🔹 INSERTAR
        [HttpPost("Insertar{entity}")]
        public IActionResult Insertar(string entity, [FromBody] JObject payload)
        {
            try
            {
                var sp = $"sp_Insertar{entity}";
                if (ExistsSP(sp))
                {
                    ExecSP(sp, JObjectToPars(payload));
                    return Ok($"Insertado correctamente ({sp})");
                }
                else
                {
                    return BadRequest($"No existe procedimiento almacenado {sp}");
                }
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        // 🔹 EDITAR
        [HttpPut("Editar{entity}")]
        public IActionResult Editar(string entity, [FromBody] JObject payload)
        {
            try
            {
                var sp = $"sp_Actualizar{entity}";
                if (ExistsSP(sp))
                {
                    ExecSP(sp, JObjectToPars(payload));
                    return Ok($"Actualizado correctamente ({sp})");
                }
                else
                {
                    return BadRequest($"No existe procedimiento almacenado {sp}");
                }
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        // 🔹 ELIMINAR
        [HttpDelete("Eliminar{entity}")]
        public IActionResult Eliminar(string entity, [FromQuery] int id)
        {
            try
            {
                var sp = $"sp_Eliminar{entity}";
                if (ExistsSP(sp))
                {
                    ExecSP(sp, new Dictionary<string, object> { { $"{entity}ID", id } });
                    return Ok($"Eliminado correctamente ({sp})");
                }
                else
                {
                    using var cn = new SqlConnection(Conn);
                    using var cmd = new SqlCommand($"DELETE FROM [{entity}] WHERE [{entity}ID] = @id", cn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    return Ok($"Registro eliminado de {entity}");
                }
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        // ========== 🔧 HELPERS ==========
        private bool ExistsSP(string spName)
        {
            using var cn = new SqlConnection(Conn);
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.procedures WHERE name = @n", cn);
            cmd.Parameters.AddWithValue("@n", spName);
            cn.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }

        private DataTable ExecSP(string sp, IDictionary<string, object> pars)
        {
            var dt = new DataTable();
            using var cn = new SqlConnection(Conn);
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            foreach (var kv in pars)
                cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        private List<Dictionary<string, object>> DataTableToList(DataTable t)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow r in t.Rows)
            {
                var d = new Dictionary<string, object>();
                foreach (DataColumn c in t.Columns)
                    d[c.ColumnName] = r[c] == DBNull.Value ? null : r[c];
                list.Add(d);
            }
            return list;
        }

        private Dictionary<string, object> JObjectToPars(JObject j)
        {
            var dict = new Dictionary<string, object>();
            foreach (var p in j.Properties())
                dict[p.Name] = p.Value.Type == JTokenType.Null ? null : p.Value.ToObject<object>();
            return dict;
        }
    }
}

