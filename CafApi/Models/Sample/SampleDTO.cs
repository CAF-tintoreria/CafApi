namespace CafApi.Models.Sample
{
    public class SampleDTO
    {
        public string ClotheType { get; set; }
        public decimal KilogramWeight { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class CreateSampleDTO : SampleDTO
    { 
            public int ClientID { get; set; }
            public int SampleStatusID { get; set; }
    }

    public class UpdateSampleDTO : CreateSampleDTO
    {
        public int SampleID { get; set; }
    }

    public class SampleDetailDTO : SampleDTO
    {
        public Cliente Client {get; set; }
    }
}
