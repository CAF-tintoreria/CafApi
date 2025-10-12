using CafApi.Models.SampleState;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafApi.Models.Sample
{
    public class SampleDTO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MuestraID { get; set; }
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
        public Client Client {get; set; }
        public SampleStatus Status { get; set; }
        public List<Process> Processes{ get; set; }
    }
}
