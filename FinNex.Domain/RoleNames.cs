namespace FinNex.Domain;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string HR = "HR";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";
    public const string HR_View = "HR_View";
    public const string Rehber = "Rehber";
    public const string SobeReisi = "SobeReisi";
    public const string Muhasib = "Muhasib";

    // Kredit modulu — admin bu rolu olan istifadəçilər kimin kredit müraciətlərinə
    // baxa və ya komitə üzvü olduğunu idarə edir. Əsas Admin rolu da avtomatik
    // bu səlahiyyətə malikdir.
    public const string KreditAdmin = "KreditAdmin";
}
