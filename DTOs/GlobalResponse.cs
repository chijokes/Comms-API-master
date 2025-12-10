using System.Collections.Generic;

namespace FusionComms.DTOs
{
    public class GlobalResponse<T>
    {
        public int Status { get; set; }
        public string StatusText { get; set; }
        public T Data { get; set; }
        public HashSet<ErrorItemModel> Errors { get; set; }

        public GlobalResponse()
        {
            Errors = new HashSet<ErrorItemModel>();
        }

        public static GlobalResponse<T> Fail(string errorMessage)
        {
            return new GlobalResponse<T> { StatusText = errorMessage };
        }
        public static GlobalResponse<T> Success(T data, string message)
        {
            return new GlobalResponse<T> { StatusText = message, Data = data };
        }
    }

    public class ErrorItemModel
    {
        public string Key { get; set; }
        public List<string> ErrorMessages { get; set; }
    }
}
