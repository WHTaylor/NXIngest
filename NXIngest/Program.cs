namespace NXIngest
{
    static class NXIngest
    {
        private const string vesuvio =
            "C:/Users/rop61488/projects/work/LiveIngestEndToEndTests/test-data/simple-creation-tests/VESUVIO00045929.nxs";

        private const string wish =
            "C:/Users/rop61488/projects/work/LiveIngestEndToEndTests/test-data/simple-creation-tests/WISH00049859.nxs";

        public static void Main(string[] args)
        {
            new NxsIngestor()
                .IngestNexusFile(
                    wish,
                    "C:/FBS/Other/IngestExternalXmls/mapping_neutron.xml",
                    "C:/Users/rop61488/test.xml"
                );
        }
    }
}
