using APsiControleApi.Application.Interfaces;

namespace APsiControleApi.Application.Services
{
    public class SimuladorLeituraService : ISimuladorLeituraService
    {
        private readonly ILeituraService _leituraService;
        private readonly INotificadorSimulacao _notificador;

        public SimuladorLeituraService(ILeituraService leituraService, INotificadorSimulacao notificador)
        {
            _leituraService = leituraService;
            _notificador = notificador;
        }

        public async Task IniciarSimulacaoAsync(List<Guid> tagIds, Guid unidadeId)
        {
            var fim = DateTimeOffset.Parse("2024-11-12 01:42:02.000 -0300").UtcDateTime;
            var inicio = fim.AddMinutes(-30);

            var leituras = await _leituraService.ObterLeiturasPorPeriodoETagsAsync(unidadeId, inicio, fim, tagIds);
            var random = new Random();

            var leiturasPorTag = leituras
                .GroupBy(l => l.TagId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Valor).ToList()
                );

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var simulacoes = new List<object>();

                    foreach (var tagId in tagIds)
                    {
                        if (!leiturasPorTag.ContainsKey(tagId)) continue;

                        var baseValores = leiturasPorTag[tagId];
                        var media = baseValores.Average();
                        var desvio = Math.Sqrt(baseValores.Select(v => Math.Pow(v - media, 2)).Average());

                        var valorSimulado = Math.Round(media + desvio * (random.NextDouble() - 0.5) * 2, 2);

                        simulacoes.Add(new
                        {
                            TagId = tagId,
                            Valor = valorSimulado,
                            Timestamp = DateTime.UtcNow
                        });

                        Console.WriteLine($"[Simulacao Historica] TagId: {tagId}, Valor: {valorSimulado}, Timestamp: {DateTime.UtcNow:HH:mm:ss.fff}");
                    }

                    Console.WriteLine($"➡️ Enviando {simulacoes.Count} valores simulados para SignalR");
                    await _notificador.NotificarSimulacaoAsync(simulacoes);
                    await Task.Delay(1000);
                }
            });
        }
       public async Task IniciarSimulacaoPIDComRespostaAoDegrauAsync(
            double k, double tau, double theta,
            Guid? tagKp = null,
            Guid? tagKi = null,
            Guid? tagKd = null,
            List<Guid>? outrasTags = null,
            Guid unidadeId = default,
            double? valorInicial = null)
        {
            var valorInicialReal = valorInicial ?? 0.0;
            var dataInicio = DateTime.UtcNow;

            var fim = DateTimeOffset.Parse("2024-11-12 01:42:02.000 -0300").UtcDateTime;
            var inicio = fim.AddMinutes(-30);

            var todasTags = new List<Guid>();
            if (tagKp.HasValue) todasTags.Add(tagKp.Value);
            if (tagKi.HasValue) todasTags.Add(tagKi.Value);
            if (tagKd.HasValue) todasTags.Add(tagKd.Value);
            if (outrasTags != null) todasTags.AddRange(outrasTags);

            var leituras = await _leituraService.ObterLeiturasPorPeriodoETagsAsync(unidadeId, inicio, fim, todasTags);
            var random = new Random();

            var leiturasPorTag = leituras
                .Where(l => todasTags.Contains(l.TagId))
                .GroupBy(l => l.TagId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Valor).ToList()
                );

            var tagKpLocal = tagKp;
            var tagKiLocal = tagKi;
            var tagKdLocal = tagKd;
            var outrasTagsLocal = outrasTags != null ? new List<Guid>(outrasTags) : new List<Guid>();
            var unidadeIdLocal = unidadeId;
            var kLocal = k;
            var tauLocal = tau;
            var thetaLocal = theta;
            var valorInicialLocal = valorInicialReal;

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var agora = DateTime.UtcNow;
                    var tempoDecorrido = (agora - dataInicio).TotalSeconds;

                    double calcularRespostaDegrau(double t)
                    {
                        if (t < thetaLocal)
                            return valorInicialLocal;
                        return valorInicialLocal + kLocal * (1 - Math.Exp(-(t - thetaLocal) / tauLocal));
                    }

                    var simulacoes = new List<object>();

                    var valorSimulado = Math.Round(calcularRespostaDegrau(tempoDecorrido), 4);

                    if (tagKpLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKpLocal.Value, Valor = valorSimulado, Timestamp = agora });

                    if (tagKiLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKiLocal.Value, Valor = valorSimulado, Timestamp = agora });

                    if (tagKdLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKdLocal.Value, Valor = valorSimulado, Timestamp = agora });

                    if (outrasTagsLocal != null)
                    {
                        foreach (var tagId in outrasTagsLocal)
                        {
                            if (!leiturasPorTag.ContainsKey(tagId)) continue;

                            var baseValores = leiturasPorTag[tagId];
                            var media = baseValores.Average();
                            var desvio = Math.Sqrt(baseValores.Select(v => Math.Pow(v - media, 2)).Average());

                            var valorHistorico = Math.Round(media + desvio * (random.NextDouble() - 0.5) * 2, 2);

                            simulacoes.Add(new
                            {
                                TagId = tagId,
                                Valor = valorHistorico,
                                Timestamp = agora
                            });
                        }
                    }

                    Console.WriteLine($"➡️ Enviando {simulacoes.Count} valores simulados para SignalR");
                    await _notificador.NotificarSimulacaoAsync(simulacoes);
                    await Task.Delay(1000);
                }
            });
        }


        public async Task IniciarSimulacaoPIDOscilacaoSustentadaAsync(
            double ku, double pu,
            Guid? tagKp = null,
            Guid? tagKi = null,
            Guid? tagKd = null,
            Guid unidadeId = default)
        {
            var dataInicio = DateTime.UtcNow;
            var random = new Random();

            // Calcula os ganhos segundo Ziegler-Nichols Oscilação Sustentada
            double kp = 0.6 * ku;
            double ki = 2 * kp / pu;
            double kd = kp * pu / 8;

            var tagKpLocal = tagKp;
            var tagKiLocal = tagKi;
            var tagKdLocal = tagKd;
            var unidadeIdLocal = unidadeId;
            var kpLocal = kp;
            var kiLocal = ki;
            var kdLocal = kd;
            var puLocal = pu;

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var agora = DateTime.UtcNow;
                    var tempoDecorrido = (agora - dataInicio).TotalSeconds;

                    // Gerar sinal oscilante (modelo de senoide)
                    double sinalOscilante = 0.8 * Math.Sin(2 * Math.PI * tempoDecorrido / puLocal);

                    var simulacoes = new List<object>();

                    if (tagKpLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKpLocal.Value, Valor = Math.Round(sinalOscilante * kpLocal, 4), Timestamp = agora });

                    if (tagKiLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKiLocal.Value, Valor = Math.Round(sinalOscilante * kiLocal, 4), Timestamp = agora });

                    if (tagKdLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKdLocal.Value, Valor = Math.Round(sinalOscilante * kdLocal, 4), Timestamp = agora });

                    Console.WriteLine($"➡️ Enviando {simulacoes.Count} valores simulados para SignalR");
                    await _notificador.NotificarSimulacaoAsync(simulacoes);
                    await Task.Delay(1000);
                }
            });
        }


        public async Task IniciarSimulacaoPIDSinteseDiretaAsync(
            double k, double tau, double theta, double taud,
            Guid? tagKp = null,
            Guid? tagKi = null,
            Guid? tagKd = null,
            Guid unidadeId = default)
        {
            var dataInicio = DateTime.UtcNow;

            // Calcula ganhos PID usando Síntese Direta
            double kp = tau / (k * (taud + theta));
            double ki = 1 / (taud + theta);
            double kd = (taud * theta) / (taud + theta);

            var tagKpLocal = tagKp;
            var tagKiLocal = tagKi;
            var tagKdLocal = tagKd;
            var unidadeIdLocal = unidadeId;

            var kpLocal = kp;
            var kiLocal = ki;
            var kdLocal = kd;
            var taudLocal = taud;
            var thetaLocal = theta;

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var agora = DateTime.UtcNow;
                    var tempoDecorrido = (agora - dataInicio).TotalSeconds;

                    double calcularRespostaDegrau(double t)
                    {
                        if (t < thetaLocal)
                            return 0.0;
                        return 1.0 * (1 - Math.Exp(-(t - thetaLocal) / taudLocal));
                    }

                    var simulacoes = new List<object>();

                    var valorSimulado = Math.Round(calcularRespostaDegrau(tempoDecorrido), 4);

                    if (tagKpLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKpLocal.Value, Valor = Math.Round(valorSimulado * kpLocal, 4), Timestamp = agora });

                    if (tagKiLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKiLocal.Value, Valor = Math.Round(valorSimulado * kiLocal, 4), Timestamp = agora });

                    if (tagKdLocal.HasValue)
                        simulacoes.Add(new { TagId = tagKdLocal.Value, Valor = Math.Round(valorSimulado * kdLocal, 4), Timestamp = agora });

                    Console.WriteLine($"➡️ Enviando {simulacoes.Count} valores simulados para SignalR");
                    await _notificador.NotificarSimulacaoAsync(simulacoes);
                    await Task.Delay(1000);
                }
            });
        }

    }
}
