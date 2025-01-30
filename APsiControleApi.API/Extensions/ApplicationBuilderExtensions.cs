namespace APsiControleApi.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void ConfigurePipeline(this WebApplication app)
        {
            // Configurações de desenvolvimento
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Middleware padrão
            app.UseHttpsRedirection();
            app.UseAuthentication(); // Adiciona autenticação JWT
            app.UseAuthorization();

        }
    }
}
