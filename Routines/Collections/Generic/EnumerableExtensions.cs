using System.Collections.Generic;
using System.Linq;

namespace VoltElekto.Collections.Generic
{
    /// <summary>
    ///     Classe utilitária com operações utilizando Enumeráveis (Arrays, Listas, etc).
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Retorna uma versão certamente não-nula do original.
        /// </summary>
        /// <remarks>
        /// Use quando os dados vierem de fonte externa (chamada de cliente do WS) e existe a chance de ser enviado um nulo ao invés de coleção vazia.
        /// </remarks>
        public static IEnumerable<T> Normalize<T>(this IEnumerable<T> original)
        {
            return original ?? Enumerable.Empty<T>();
        }
    }
}