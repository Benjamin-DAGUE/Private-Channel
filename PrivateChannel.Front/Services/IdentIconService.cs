using Google.Protobuf.WellKnownTypes;
using System.Collections.Concurrent;
using System.Text;

namespace PrivateChannel.Front.Services;

public class IdentIconService
{
    #region Fields
    
    private (byte A, byte R, byte G, byte B) _BaseColor = (0xff, 0x77, 0x6b, 0xe7);
    private int _Variation = 50;

    #endregion

    #region Methods

    public string GetIconFromId(Guid channel)
    {
        int size = 64;
        var hash = channel.ToByteArray();
        int squareSize = size / 8;

        StringBuilder svg = new StringBuilder();
        svg.AppendLine($"<svg width='{size}' height='{size}' xmlns='http://www.w3.org/2000/svg'>");

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int index = (y * 8 + x) % hash.Length;
                byte b = hash[index];

                var color = AdjustColor(b);
                svg.AppendLine($"<rect x='{x * squareSize}' y='{y * squareSize}' width='{squareSize}' height='{squareSize}' fill='#{color.R:X2}{color.G:X2}{color.B:X2}' />");
            }
        }

        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    private (byte R, byte G, byte B) AdjustColor(byte adjustment)
    {
        int factor = (adjustment % (_Variation * 2)) - _Variation;
        return (
            Clamp(_BaseColor.R + factor),
            Clamp(_BaseColor.G + factor),
            Clamp(_BaseColor.B + factor)
        );
    }

    private byte Clamp(int value) => (byte)Math.Max(0, Math.Min(255, value));

    #endregion

}
