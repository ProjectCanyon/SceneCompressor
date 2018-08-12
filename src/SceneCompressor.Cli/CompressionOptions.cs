using System.IO;

namespace SceneCompressor.Cli
{
    public class CompressionOptions
    {
        public FileInfo Source { get; set; }
        public FileInfo Target { get; set; }
        public int Passes { get; set; }
        public bool Verbose { get; set; }
        public bool Force { get; set; }
    }
}