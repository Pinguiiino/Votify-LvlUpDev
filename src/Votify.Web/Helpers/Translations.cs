namespace Votify.Web.Helpers;

public static class Translations
{
    public static readonly Dictionary<string, string> TipoMaterial = new()
    {
        { "Photo",    "Foto" },
        { "Video",    "Vídeo" },
        { "Document", "Documento" },
        { "Audio",    "Audio" },
        { "Other",    "Otro" }
    };

    public static readonly Dictionary<string, string> TipoProyecto = new()
    {
        { "AI",             "Inteligencia Artificial" },
        { "Sustainability", "Sostenibilidad" },
        { "General", "General" }
    };
}