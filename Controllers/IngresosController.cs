using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using NOVIX.Models;

namespace NOVIX.Controllers
{
    public class IngresosController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IngresosController> _logger;

        public IngresosController(
            IConfiguration configuration,
            ILogger<IngresosController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            List<MovimientoFinanciero> ingresos =
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
                            "@Tipo", SqlDbType.NVarChar, 10).Value = "Ingreso";

                        using (SqlDataReader lector = comando.ExecuteReader())
                        {
                            while (lector.Read())
                            {
                                MovimientoFinanciero ingreso =
                                    new MovimientoFinanciero();

                                ingreso.Id =
                                    Convert.ToInt32(lector["Id"]);

                                ingreso.Fecha =
                                    Convert.ToDateTime(lector["Fecha"]);

                                ingreso.Descripcion =
                                    Convert.ToString(lector["Descripcion"]) ?? "";

                                ingreso.Categoria =
                                    Convert.ToString(lector["Categoria"]) ?? "";

                                ingreso.Monto =
                                    Convert.ToDecimal(lector["Monto"]);

                                ingreso.Tipo = "Ingreso";

                                ingresos.Add(ingreso);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los ingresos.");

                ViewBag.Error =
                    "No fue posible cargar los ingresos. Revisa la conexión y la tabla de movimientos.";
            }

            return View(ingresos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(MovimientoFinanciero ingreso)
        {
            ingreso.Tipo = "Ingreso";

            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Verifica la fecha, descripción, categoría y monto del ingreso.";

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
                            "@Fecha", SqlDbType.Date).Value = ingreso.Fecha.Date;

                        comando.Parameters.Add(
                            "@Descripcion", SqlDbType.NVarChar, 200).Value =
                            ingreso.Descripcion.Trim();

                        comando.Parameters.Add(
                            "@Tipo", SqlDbType.NVarChar, 10).Value = "Ingreso";

                        comando.Parameters.Add(
                            "@Categoria", SqlDbType.NVarChar, 100).Value =
                            ingreso.Categoria.Trim();

                        SqlParameter parametroMonto =
                            comando.Parameters.Add("@Monto", SqlDbType.Decimal);

                        parametroMonto.Precision = 12;
                        parametroMonto.Scale = 2;
                        parametroMonto.Value = ingreso.Monto;

                        comando.ExecuteNonQuery();
                    }
                }

                TempData["Mensaje"] =
                    "Ingreso registrado correctamente.";
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error SQL al registrar un ingreso.");

                TempData["Error"] =
                    "No se pudo guardar el ingreso. Revisa la conexión, la tabla y los datos de la base de datos.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar un ingreso.");

                TempData["Error"] =
                    "Ocurrió un error inesperado al registrar el ingreso.";
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
                    "El ingreso seleccionado no es válido.";

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
                            "@Tipo", SqlDbType.NVarChar, 10).Value = "Ingreso";

                        int filasEliminadas = comando.ExecuteNonQuery();

                        if (filasEliminadas > 0)
                        {
                            TempData["Mensaje"] =
                                "Ingreso eliminado correctamente.";
                        }
                        else
                        {
                            TempData["Error"] =
                                "No se encontró el ingreso seleccionado.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar el ingreso con Id {Id}.",
                    id);

                TempData["Error"] =
                    "No se pudo eliminar el ingreso.";
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