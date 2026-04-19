using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEtiquetas.Domain.Entities
{
    public class Configuracao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string StoreUrl { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime TokenExpiration { get; set; }

        public int SyncIntervalSeconds { get; set; } = 60;

        // Configurações de Impressora
        public string ImpressoraIp { get; set; } = "127.0.0.1";

        public int ImpressoraPorta { get; set; } = 9100;
    }
}

