using System.Xml.Serialization;

namespace Darkages.Compression
{
    public class CompressableObject
    {
        [XmlIgnore] public byte[] DeflatedData { get; set; }
        [XmlIgnore] protected string Filename { get; set; }
        [XmlIgnore] public byte[] InflatedData { get; set; }

        public static T Load<T>(string filename, bool deflated = true)
            where T : CompressableObject, new()
        {
            var result = new T();

            if (deflated)
            {
                result.DeflatedData = File.ReadAllBytes(filename);
                result.Decompress();
            }
            else
            {
                result.InflatedData = File.ReadAllBytes(filename);
                result.Compress();
            }

            result.Filename = filename;

            var stream = new MemoryStream(result.InflatedData);
            result.Load(stream);

            return result;
        }

        public void Compress()
        {
            DeflatedData = CompressionProvider.Deflate(InflatedData);
        }

        public void Decompress()
        {
            InflatedData = CompressionProvider.Inflate(DeflatedData);
        }

        protected virtual void Load(MemoryStream stream) { }

        public virtual Stream Save(MemoryStream stream) => stream;
    }
}