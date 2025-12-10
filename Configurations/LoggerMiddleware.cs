using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace FusionComms.Configurations
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggerMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            // Capture the request body
            var requestBody = await CaptureRequestBody(context.Request);
            context.Items["RequestBody"] = requestBody;

            // Buffer the response body
            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await _next(context);
                stopwatch.Stop();

                // Capture the response body
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyContent = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);

                Log.Information(
                    "HTTP {RequestHost} {RequestMethod} {RequestPath} {RequestQuery} responded {StatusCode} in {ElapsedMilliseconds} ms" +
                    "\nRequest Body: {RequestBody}\n Request Headers: {RequestHeaders} \nResponse Body: {ResponseBody} \n Response Headers: {ResponseHeaders}\n {TimeStamp}",
                    context.Request.Host,
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.Query,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    requestBody,
                    context.Request.Headers,
                    responseBodyContent,
                    context.Response.Headers,
                    DateTimeOffset.UtcNow);
            }
        }

        private async Task<string> CaptureRequestBody(HttpRequest request)
        {
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return requestBody;
        }
    }
}