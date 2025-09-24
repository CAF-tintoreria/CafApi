namespace CafApi.Models.Sample
{
    public class Sample
    {
        public int ID { get; set; }
        public string ClotheType { get; set; }
        public decimal KilogramWeight { get; set; }
        public int ProcessID { get; set; }
        public int SampleStatusID { get; set; }
        public DateTime ModifiedAt { get; set; }

        public Client Client { get; set; }

    }
}
