using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NOVIX.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace NOVIX.Controllers
{
    public class RegistrosController : Controller
    {
        private readonly IConfiguration _configuration;

        public RegistrosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index(string? buscar, string? tipo, string? categoria, DateTime? desde, DateTime? hasta)
        {
            List<MovimientoFinanciero> registros = new List<MovimientoFinanciero>();
            List<string> categorias = new List<string>();

            ViewBag.Buscar = buscar ?? "";
            ViewBag.TipoSeleccionado = tipo ?? "";
            ViewBag.CategoriaSeleccionada = categoria ?? "";
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd") ?? "";

            try
            {
                using (SqlConnection conexion = new SqlConnection(_configuration.GetConnectionString("NOVIXConnection")))
                {
                    conexion.Open();
                    string consulta = @"
                        SELECT Id, Fecha, Descripcion, Tipo, Categoria, Monto
                        FROM MovimientosFinancieros
                        WHERE (@Buscar = '' OR Descripcion LIKE '%' + @Buscar + '%' OR Categoria LIKE '%' + @Buscar + '%')
                          AND (@Tipo = '' OR Tipo = @Tipo)
                          AND (@Categoria = '' OR Categoria = @Categoria)
                          AND (@Desde IS NULL OR Fecha >= @Desde)
                          AND (@Hasta IS NULL OR Fecha < DATEADD(DAY, 1, @Hasta))
                        ORDER BY Fecha DESC, Id DESC";

                    using (SqlCommand comando = new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.Add("@Buscar", SqlDbType.NVarChar, 200).Value = buscar?.Trim() ?? "";
                        comando.Parameters.Add("@Tipo", SqlDbType.NVarChar, 20).Value = tipo == "Ingreso" || tipo == "Gasto" ? tipo : "";
                        comando.Parameters.Add("@Categoria", SqlDbType.NVarChar, 100).Value = categoria?.Trim() ?? "";
                        comando.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.HasValue ? (object)desde.Value.Date : DBNull.Value;
                        comando.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.HasValue ? (object)hasta.Value.Date : DBNull.Value;

                        using (SqlDataReader lector = comando.ExecuteReader())
                        {
                            while (lector.Read())
                            {
                                MovimientoFinanciero registro = new MovimientoFinanciero();
                                registro.Id = Convert.ToInt32(lector["Id"]);
                                registro.Fecha = Convert.ToDateTime(lector["Fecha"]);
                                registro.Descripcion = lector["Descripcion"].ToString() ?? "";
                                registro.Tipo = lector["Tipo"].ToString() ?? "";
                                registro.Categoria = lector["Categoria"].ToString() ?? "";
                                registro.Monto = Convert.ToDecimal(lector["Monto"]);
                                registros.Add(registro);
                            }
                        }
                    }

                    string consultaCategorias = @"SELECT DISTINCT Categoria FROM MovimientosFinancieros WHERE Categoria IS NOT NULL AND LTRIM(RTRIM(Categoria)) <> '' ORDER BY Categoria";
                    using (SqlCommand comandoCategorias = new SqlCommand(consultaCategorias, conexion))
                    using (SqlDataReader lectorCategorias = comandoCategorias.ExecuteReader())
                    {
                        while (lectorCategorias.Read()) categorias.Add(lectorCategorias["Categoria"].ToString() ?? "");
                    }
                }
                ViewBag.Categorias = categorias;
            }
            catch (Exception)
            {
                ViewBag.Error = "No fue posible cargar los registros. Verifica la conexión y la estructura de NOVIXDB.";
                ViewBag.Categorias = categorias;
            }

            decimal ingresos = 0m;
            decimal gastos = 0m;
            foreach (MovimientoFinanciero registro in registros)
            {
                if (registro.Tipo == "Ingreso") ingresos += registro.Monto;
                else if (registro.Tipo == "Gasto") gastos += registro.Monto;
            }

            ViewBag.TotalRegistros = registros.Count;
            ViewBag.TotalIngresos = ingresos;
            ViewBag.TotalGastos = gastos;
            ViewBag.Balance = ingresos - gastos;
            return View(registros);
        }
    }
}
