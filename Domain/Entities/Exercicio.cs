using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoveUp.Entities
{
public class Exercicio
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nome { get; set; } = string.Empty;
        public string GrupoMuscular { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Propriedade de navegação
        public ICollection<TreinoExercicio> TreinoExercicios { get; set; } = new List<TreinoExercicio>();
    }
}