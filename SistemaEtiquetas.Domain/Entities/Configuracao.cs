using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaEtiquetas.Domain.Entities
{
    public class Configuracao
    {
        public int Id { get; set; }

        public string StoreUrl { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTime TokenExpiration { get; set; }

        public int SyncIntervalSeconds { get; set; } = 60;
    }
}
