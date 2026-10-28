using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace APsiOpcDaApi.API.Services;
public sealed class ConnectionSecretSchemaFilter:ISchemaFilter
{
    public void Apply(OpenApiSchema schema,SchemaFilterContext context)
    {
        if(context.Type!=typeof(APsiOpcDaApi.Application.DTOs.OpcServerDTO))return;
        foreach(var property in schema.Properties)
            if(new[]{"password","connectionString","errorMessage"}.Contains(property.Key,StringComparer.OrdinalIgnoreCase))property.Value.WriteOnly=true;
    }
}
