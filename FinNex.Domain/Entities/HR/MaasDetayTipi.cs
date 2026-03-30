namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Maaş detalının tipi.
    /// Hesablamada gəlir, tutulma və işəgötürən xərclərini ayırd etmək üçün.
    /// </summary>
    public enum MaasDetayTipi
    {
        /// <summary>Əsas maaş, bonus, məzuniyyət ödənişi kimi gəlirlər</summary>
        Gelir = 1,

        /// <summary>Gəlir vergisi, DSMF, sığorta kimi işçidən tutulanlar</summary>
        Tutulma = 2,

        /// <summary>
        /// DSMF (22%), işsizlik (0.5%) kimi şirkətin ödədiyi paylar.
        /// İşçidən tutulmur, yalnız hesabat üçün göstərilir.
        /// </summary>
        IsegoturenXerci = 3
    }
}