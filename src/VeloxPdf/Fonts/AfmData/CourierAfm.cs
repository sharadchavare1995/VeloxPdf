namespace VeloxPdf.Fonts.AfmData;

/// <summary>AFM character widths for Courier (monospaced — all printable chars = 600).</summary>
public static class CourierAfm
{
    public static readonly float[] Widths;

    static CourierAfm()
    {
        Widths = new float[256];
        // Monospaced: all printable chars (32-255) have width 600
        for (int i = 32; i < 256; i++)
            Widths[i] = 600f;
        // Control chars (0-31) stay 0
        Widths[0] = 0f;
    }
}
