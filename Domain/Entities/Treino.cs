using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoveUp.Entities
{
public class Treino
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }

        public int DiaSemana { get; set; }

        public bool Ativo { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Propriedade de navegação
        public ICollection<TreinoExercicio> TreinoExercicios { get; set; } = new List<TreinoExercicio>();
    }
}
