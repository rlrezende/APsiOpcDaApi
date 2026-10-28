namespace APsiOpcDaApi.Infrastructure;
public interface IQuotaSaveBoundary { int Save(APsiOpcDaApiContext context, Func<int> persist); }
