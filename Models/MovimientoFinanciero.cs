using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NOVIX.Models
{
    public class MovimientoFinanciero
    {
        public int Id { get; set; }

        [BindNever]
        public string Tipo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(
            200,
            ErrorMessage = "La descripción no puede superar los 200 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona una categoría.")]
        [StringLength(
            100,
            ErrorMessage = "La categoría no puede superar los 100 caracteres.")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Range(
            typeof(decimal),
            "0.01",
            "9999999999.99",
            ErrorMessage = "El monto debe ser mayor que cero.")]
        public decimal Monto { get; set; }
    }
}