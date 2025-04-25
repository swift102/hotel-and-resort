using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_and_resort.ErrorHandling
{
    public class GlobalExceptionHandler : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            context.Result = new ObjectResult(new
            {
                Message = "An error occurred.",
                Exception = context.Exception.Message
            })
            {
                StatusCode = 500
            };
        }
    }
}
