using System.Text.Json;

namespace TEContigo.Shared.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Mapeo de excepciones de negocio → código HTTP. Si tus services
            // lanzan otro tipo de excepción de negocio en el futuro, agrégala
            // aquí en vez de dejar que caiga en el 500 genérico.
            var (statusCode, message) = exception switch
            {
                KeyNotFoundException =>
                    (StatusCodes.Status404NotFound, exception.Message),

                UnauthorizedAccessException =>
                    (StatusCodes.Status403Forbidden, exception.Message),

                ArgumentException =>
                    (StatusCodes.Status400BadRequest, exception.Message),

                InvalidOperationException =>
                    (StatusCodes.Status400BadRequest, exception.Message),

                _ =>
                    (StatusCodes.Status500InternalServerError,
                        "Ocurrió un error inesperado. Intenta de nuevo más tarde.")
            };

            // Solo logueamos con detalle los 500 — los 400/403/404 son
            // parte normal del flujo de negocio (usuario sin permiso,
            // recurso no encontrado, validación fallida), no bugs.
            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    exception,
                    "Error no controlado en {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                Success = false,
                Message = message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
