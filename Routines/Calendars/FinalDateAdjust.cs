namespace VoltElekto.Calendars
{
    /// <summary>
    /// Métodos para calcular a data final de um prazo
    /// </summary>
    public enum FinalDateAdjust
    {
        /// <summary>
        /// Se terminar em dia não útil coloca no dia útil seguinte.
        /// </summary>
        Following = 1,

        /// <summary>
        /// Se terminar em dia não útil coloca no dia útil seguinte, a não ser que isso faça
        /// o mês trocar, neste caso passa a ser o dia útil anterior
        /// </summary>
        ModifiedFollowing = 2,

        /// <summary>
        /// Nenhum ajuste
        /// </summary>
        None = 0
    }
}