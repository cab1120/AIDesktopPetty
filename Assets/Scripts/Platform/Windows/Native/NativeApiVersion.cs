using System.Runtime.InteropServices;


namespace Platform.Windows.Native
{
    [StructLayout(
        LayoutKind.Sequential)]
    internal struct NativeApiVersion
    {
        public uint Major;

        public uint Minor;

        public uint Patch;


        public override string ToString()
        {
            return
                $"{Major}.{Minor}.{Patch}";
        }
    }
}