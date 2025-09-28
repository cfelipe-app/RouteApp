using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.DTOs
{
    //public class PaginationDTO
    //{
    //    public int Id { get; set; }

    //    public int Page { get; set; } = 1;

    //    public int RecordsNumber { get; set; } = 10;
    //}

    public class PaginationDTO
    {
        public int Page { get; set; } = 1;

        private int _recordsNumber = 10;

        public int RecordsNumber
        {
            get => _recordsNumber;
            set => _recordsNumber = value <= 0 ? 10 : (value > 200 ? 200 : value);
        }

        public string? Term { get; set; }       // búsqueda opcional
        public string? SortBy { get; set; }     // propiedad: Name, Code, CreatedAt
        public string SortDir { get; set; } = "asc"; // asc | desc
    }
}