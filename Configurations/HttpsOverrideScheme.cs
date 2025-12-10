using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FusionComms.Configurations
{
    public class HttpSchemeOverride
    {
        private readonly RequestDelegate _next;

        public HttpSchemeOverride(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            httpContext.Request.Scheme = "https";

            return _next(httpContext);
        }
    }

    public static class HttpSchemeOverrideExtensions
    {
        /// <summary>
        /// Rewrites request schemes from http to https.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpSchemeOverride(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpSchemeOverride>();
        }
    }
}
