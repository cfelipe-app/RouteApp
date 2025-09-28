using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteApp.Shared.Responses
{
    public class ActionResponse<T>
    {
        public bool WasSuccess { get; set; }

        public string? Message { get; set; }

        public T? Result { get; set; }

        public static ActionResponse<T> Ok(T result) => new() { WasSuccess = true, Result = result };

        public static ActionResponse<T> Fail(string msg) => new() { WasSuccess = false, Message = msg };
    }
}