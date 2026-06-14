namespace FinNex.Domain.Entities.HR
{
    // Əmr növü — mərkəzi əmr reyestrində/sayğacında hansı sahəyə aid olduğunu göstərir.
    // Hər növ öz müstəqil il-üzrə nömrələnməsinə malikdir.
    public enum EmrNovu
    {
        Mezuniyyet      = 1,   // K/M
        MaasDeyisikliyi = 2,   // MD
        IseQebul        = 3,   // İQ
        Xitam           = 4,   // X
        Ezamiyyet       = 5    // E
    }
}
