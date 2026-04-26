using System;

namespace SistemaEtiquetas.Domain
{
    // Conversões de fuso horário centralizadas. O sistema mantém todos os
    // timestamps em UTC no banco (timestamptz do Postgres) e converte para
    // America/Sao_Paulo apenas na borda de exibição/entrada de utilizador.
    public static class TimeZoneBrasil
    {
        // Tenta IANA primeiro (Linux/Mac/Render — produção) e cai para o ID
        // do Windows como fallback (devs locais e UI Razor empacotada).
        public static readonly TimeZoneInfo Brasilia = ResolverFuso();

        private static TimeZoneInfo ResolverFuso()
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById("America/Sao_Paulo", out var iana))
                return iana;
            if (TimeZoneInfo.TryFindSystemTimeZoneById("E. South America Standard Time", out var win))
                return win;
            // Último recurso: cria zona fixa UTC-3. Não respeita DST mas
            // evita NRE em ambientes sem tzdata configurada.
            return TimeZoneInfo.CreateCustomTimeZone(
                "BRT-3",
                TimeSpan.FromHours(-3),
                "Horário de Brasília",
                "Horário de Brasília");
        }

        // Recebe um DateTime que representa um instante em horário de Brasília
        // (Kind tipicamente Unspecified, vindo de form de UI ou JSON sem zona)
        // e devolve o instante UTC equivalente.
        public static DateTime DeBrasiliaParaUtc(DateTime brasiliaLocal)
        {
            var sem = DateTime.SpecifyKind(brasiliaLocal, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(sem, Brasilia);
        }

        // Converte um instante UTC para horário de Brasília. O resultado é
        // Unspecified (já não tem sentido falar em "Kind" depois da conversão)
        // — usar apenas para formatação imediata.
        public static DateTime DeUtcParaBrasilia(DateTime utc)
        {
            var src = utc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
                : utc.ToUniversalTime();
            return TimeZoneInfo.ConvertTimeFromUtc(src, Brasilia);
        }

        // Normaliza um DateTime "qualquer" para UTC, interpretando Unspecified
        // como horário de Brasília (que é o caso típico do nosso sistema:
        // payload de webhook sem zona, form HTML, etc).
        public static DateTime ParaUtcConsiderandoBrasilia(DateTime entrada)
        {
            return entrada.Kind switch
            {
                DateTimeKind.Utc => entrada,
                DateTimeKind.Local => entrada.ToUniversalTime(),
                _ => DeBrasiliaParaUtc(entrada),
            };
        }
    }
}
