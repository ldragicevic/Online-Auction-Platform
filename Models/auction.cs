namespace IEP_Projekat.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using System.Web;

    [Table("auction")]
    public partial class auction
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public auction()
        {
            bids = new HashSet<bid>();
        }

        [Key]
        public long IDAuc { get; set; }

        [Required]
        [StringLength(20)]
        public string product_name { get; set; }

        [Required]
        public long duration { get; set; }

        [Required]
        //[Range(0, long.MaxValue, ErrorMessage = "Price must be positive value.")]
        public long price { get; set; }

        [StringLength(10)]
        public string state { get; set; }

        public byte[] picture { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime create_date_time { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? open_date_time { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? close_date_time { get; set; }

        public string lastbidder{ get; set; }
        
        public long increment { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<bid> bids { get; set; }

        [NotMapped]
        public HttpPostedFileBase PictureToUpload { get; set; }
    }
}
