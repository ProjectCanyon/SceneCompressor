using System.Numerics;

namespace SceneCompressor.Cli
{
    public struct Step
    {
        public float Timestep;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Compressed;

        public Step(float timestep, Vector3 position, Quaternion rotation, bool compressed)
        {
            Timestep = timestep;
            Position = position;
            Rotation = rotation;
            Compressed = compressed;
        }
    }
}