using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoveUp.Entities
{
public class TreinoExercicio
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Chaves Estrangeiras
        public Guid TreinoId { get; set; }
        public Treino? Treino { get; set; }

        public Guid ExercicioId { get; set; }
        public Exercicio? Exercicio { get; set; }

        // Dados do treino
        public int Ordem { get; set; }
        public int Series { get; set; }
        public int Repeticoes { get; set; }
        public double CargaInicial { get; set; }
        public int TempoDescanso { get; set; } // Em segundos, ex: 60, 90
    }
}
