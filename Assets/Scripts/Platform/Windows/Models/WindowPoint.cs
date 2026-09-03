namespace Platform.Windows.Models
{
    public struct WindowPoint
    {
        public int X;

        public int Y;


        public WindowPoint(
            int x,
            int y)
        {
            X = x;

            Y = y;
        }


        public override string ToString()
        {
            return
                $"({X}, {Y})";
        }
    }
}