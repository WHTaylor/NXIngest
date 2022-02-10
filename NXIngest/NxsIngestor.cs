using NXIngest.Mapping;
using NXIngest.Output;

namespace NXIngest
{
    public class NxsIngestor
    {
        public void IngestNexusFile(
            string nexusFilePath,
            string mappingFilePath,
            string outputPath="default.xml")
        {
            var reader = new MappingReader(mappingFilePath);
            var valueCalc = new ValueResolver(nexusFilePath);
            var builder = new XmlBuilder(valueCalc);
            foreach (var c in reader)
            {
                builder.Execute(c);
            }
            builder.Save(outputPath);
        }
    }
}
