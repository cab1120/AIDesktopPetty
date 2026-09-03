using System;

namespace Platform.Windows.Models
{
    [Serializable]
    public struct WindowLogicalSize
    {
        public int Width;

        public int Height;


        public WindowLogicalSize(int width , int height)
        {
            Width = width;

            Height = height;
        }


        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}