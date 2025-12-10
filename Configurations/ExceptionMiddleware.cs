using FusionComms.DTOs;
using FusionComms.Utilities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FusionComms.Configurations
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var response = context.Response;
                response.ContentType = "application/json";
                var responseModel = GlobalResponse<string>.Fail(exception.InnerException?.Message ?? exception.Message);
                switch (exception)
                {
                    case NotFoundException e:
                        // custom application error
                        response.StatusCode = StatusCodes.Status400BadRequest;
                        responseModel.Status = StatusCodes.Status400BadRequest;
                        break;
                    case KeyNotFoundException e:
                        // not found error
                        response.StatusCode = StatusCodes.Status404NotFound;
                        responseModel.Status= StatusCodes.Status404NotFound;
                        break;
                    default:
                        // unhandled error
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        responseModel.Status = StatusCodes.Status500InternalServerError;
                        break;
                }

                await response.WriteAsJsonAsync(responseModel);
            }
        }
    }
}
