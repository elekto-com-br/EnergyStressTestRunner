using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace VoltElekto.Collections.Generic
{
    /// <summary>
    ///     The enum helper
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        ///     Gets the values.
        /// </summary>
        /// <typeparam name="TEnumType">The type of the num type.</typeparam>
        /// <returns></returns>
        private static IEnumerable<TEnumType> GetValues<TEnumType>()
        {
            return (TEnumType[])Enum.GetValues(typeof(TEnumType));
        }

        /// <summary>
        /// Devolve a descrição de um item de enumeração (atributo Description)
        /// </summary>
        /// <typeparam name="TEnumType">O tipo de enumeração</typeparam>
        /// <param name="enumItem">O elemento da enumeração para o qual obter a descrição</param>
        /// <returns>A descrição. Caso o atributo não esteja definido, será o nome do elemento.</returns>
        public static string GetDescription<TEnumType>(this TEnumType enumItem)
        {
            var name = enumItem.ToString();
            var fi = enumItem.GetType().GetField(name);
            var description = name;
            if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes)
            {
                description = !attributes.Any() ? name : attributes[0].Description;
            }

            return description;
        }

        /// <summary>
        ///     Devolve os itens da enumeração e suas descrições
        /// </summary>
        /// <typeparam name="TEnumType">The type of the num type.</typeparam>
        /// <returns></returns>
        public static IEnumerable<EnumDescription<TEnumType>> GetEnumDescriptions<TEnumType>()
        {
            return from value in GetValues<TEnumType>()
                select new EnumDescription<TEnumType>(value);
        }
    }

    public class EnumDescription<TEnumType>
    {
        public TEnumType Value { get; }
        public string Description { get; }

        public EnumDescription(TEnumType value, string description = null)
        {
            Value = value;
            Description = string.IsNullOrWhiteSpace(description) ? value.GetDescription() : description;
        }

        public override string ToString()
        {
            return Description;
        }
    }

}