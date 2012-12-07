
using System.Drawing;

namespace BengiLED_for_C_Power
{
    public class ColorAtPixel
    {
        private Color pixelColor = Color.Black;
        private Point pixelPoint;

        public Color PixelColor
        {
            get { return pixelColor; }
            set { pixelColor = value; }
        }

        public Point PixelPoint
        {
            get { return pixelPoint; }
            set { pixelPoint = value; }
        }

        public ColorAtPixel(Color color, int x, int y)
        {
            PixelColor = color;
            PixelPoint = new Point(x, y);
        }
    }
}
