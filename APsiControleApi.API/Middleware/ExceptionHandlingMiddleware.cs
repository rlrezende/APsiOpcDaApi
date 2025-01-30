
using System.Net;
using Newtonsoft.Json;
using APsiControleApi.Application.Exceptions;

namespace APsiControleApi.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Continua para o próximo middleware
            }
            catch (Exception ex)
            {
                // Log detalhado da exceção
                _logger.LogError(ex, "Ocorreu uma exceção não tratada. Detalhes: {Mensagem}, Caminho: {Path}, Método: {Method}",
                    ex.Message,
                    context.Request.Path,
                    context.Request.Method);

                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Define um código de status apropriado, dependendo do tipo de exceção
            if (exception is NotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            // Retorna uma mensagem genérica, sem expor detalhes internos
            var result = JsonConvert.SerializeObject(new
            {
                message = "Ocorreu um erro no servidor. Por favor, tente novamente mais tarde.",
                statusCode = context.Response.StatusCode
            });

            return context.Response.WriteAsync(result);
        }
    }
}
