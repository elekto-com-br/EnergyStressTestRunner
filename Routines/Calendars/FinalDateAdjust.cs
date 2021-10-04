namespace VoltElekto.Calendars
{
    /// <summary>
    /// M�todos para calcular a data final de um prazo
    /// </summary>
    public enum FinalDateAdjust
    {
        /// <summary>
        /// Se terminar em dia n�o �til coloca no dia �til seguinte.
        /// </summary>
        Following = 1,

        /// <summary>
        /// Se terminar em dia n�o �til coloca no dia �til seguinte, a n�o ser que isso fa�a
        /// o m�s trocar, neste caso passa a ser o dia �til anterior
        /// </summary>
        ModifiedFollowing = 2,

        /// <summary>
        /// Nenhum ajuste
        /// </summary>
        None = 0
    }
}