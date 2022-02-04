namespace NXIngest
{
    public class NxsIngestor
    {
        public void Test()
        {
            const string m = "C:/FBS/Other/IngestExternalXmls/mapping_neutron.xml";
            const string nxs = "C:/Users/rop61488/projects/work/LiveIngestEndToEndTests/test-data/simple-creation-tests/MER58962.nxs";
            var reader = new MappingReader(m);
            var valueCalc = new ValueResolver(nxs);
            var builder = new XmlBuilder(valueCalc);
            foreach (var c in reader)
            {
                builder.Execute(c);
            }
            builder.Save("C:/Users/rop61488/test.xml");
        }
    }
}
