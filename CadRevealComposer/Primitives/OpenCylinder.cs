namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public class OpenCylinder : APrimitive
    {
        [JsonProperty("color")]
        public int[] Color { get; set; }

        [JsonProperty("diagonal")]
        public float Diagonal { get; set; }

        [JsonProperty("center_x")]
        public float CenterX { get; set; }

        [JsonProperty("center_y")]
        public float CenterY { get; set; }

        [JsonProperty("center_z")]
        public float CenterZ { get; set; }

        [JsonProperty("center_axis")]
        public float[] CenterAxis { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }
    }
}