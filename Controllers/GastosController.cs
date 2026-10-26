using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using NOVIX.Models;

namespace NOVIX.Controllers
{
    public class GastosController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GastosController> _logger;

        public GastosController(
            IConfiguration configuration,
            ILogger<GastosController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            List<MovimientoFinanciero> gastos =
                new List<MovimientoFinanciero>();

            try
            {
                using (SqlConnection conexion = CrearConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        SELECT Id, Fecha, Descripcion, Categoria, Monto
                        FROM dbo.MovimientosFinancieros
                        WHERE Tipo = @Tipo
                        ORDER BY Fecha DESC, Id DESC";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.Add(
                            "@Tipo", SqlDbType.NVarChar, 10).Value = "Gasto";

                        using (SqlDataReader lector = comando.ExecuteReader())
                        {
                            while (lector.Read())
                            {
                                MovimientoFinanciero gasto =
                                    new MovimientoFinanciero();

                                gasto.Id =
                                    Convert.ToInt32(lector["Id"]);

                                gasto.Fecha =
                                    Convert.ToDateTime(lector["Fecha"]);

                                gasto.Descripcion =
                                    Convert.ToString(lector["Descripcion"]) ?? "";

                                gasto.Categoria =
                                    Convert.ToString(lector["Categoria"]) ?? "";

                                gasto.Monto =
                                    Convert.ToDecimal(lector["Monto"]);

                                gasto.Tipo = "Gasto";

                                gastos.Add(gasto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los gastos.");

                ViewBag.Error =
                    "No fue posible cargar los gastos. Revisa la conexión y la tabla de movimientos.";
            }

            return View(gastos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(MovimientoFinanciero gasto)
        {
            gasto.Tipo = "Gasto";

            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Verifica la fecha, descripción, categoría y monto del gasto.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                using (SqlConnection conexion = CrearConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        INSERT INTO dbo.MovimientosFinancieros
                            (Fecha, Descripcion, Tipo, Categoria, Monto)
                        VALUES
                            (@Fecha, @Descripcion, @Tipo, @Categoria, @Monto)";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.Add(
                            "@Fecha", SqlDbType.Date).Value = gasto.Fecha.Date;

                        comando.Parameters.Add(
                            "@Descripcion", SqlDbType.NVarChar, 200).Value =
                            gasto.Descripcion.Trim();

                        comando.Parameters.Add(
                            "@Tipo", SqlDbType.NVarChar, 10).Value = "Gasto";

                        comando.Parameters.Add(
                            "@Categoria", SqlDbType.NVarChar, 100).Value =
                            gasto.Categoria.Trim();

                        SqlParameter parametroMonto =
                            comando.Parameters.Add("@Monto", SqlDbType.Decimal);

                        parametroMonto.Precision = 12;
                        parametroMonto.Scale = 2;
                        parametroMonto.Value = gasto.Monto;

                        comando.ExecuteNonQuery();
                    }
                }

                TempData["Mensaje"] =
                    "Gasto registrado correctamente.";
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al registrar un gasto.");

                TempData["Error"] =
                    "No se pudo guardar el gasto. Revisa la conexión, la tabla y los datos de la base de datos.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar un gasto.");

                TempData["Error"] =
                    "Ocurrió un error inesperado al registrar el gasto.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] =
                    "El gasto seleccionado no es válido.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                using (SqlConnection conexion = CrearConexion())
                {
                    conexion.Open();

                    string consulta = @"
                        DELETE FROM dbo.MovimientosFinancieros
                        WHERE Id = @Id AND Tipo = @Tipo";

                    using (SqlCommand comando =
                        new SqlCommand(consulta, conexion))
                    {
                        comando.Parameters.Add(
                            "@Id", SqlDbType.Int).Value = id;

                        comando.Parameters.Add(
                            "@Tipo", SqlDbType.NVarChar, 10).Value = "Gasto";

                        int filasEliminadas = comando.ExecuteNonQuery();

                        if (filasEliminadas > 0)
                        {
                            TempData["Mensaje"] =
                                "Gasto eliminado correctamente.";
                        }
                        else
                        {
                            TempData["Error"] =
                                "No se encontró el gasto seleccionado.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar el gasto con Id {Id}.",
                    id);

                TempData["Error"] =
                    "No se pudo eliminar el gasto.";
            }

            return RedirectToAction(nameof(Index));
        }

        private SqlConnection CrearConexion()
        {
            string? cadenaConexion =
                _configuration.GetConnectionString("NOVIXConnection");

            if (string.IsNullOrWhiteSpace(cadenaConexion))
            {
                throw new InvalidOperationException(
                    "No está configurada la cadena NOVIXConnection.");
            }

            return new SqlConnection(cadenaConexion);
        }
    }
}