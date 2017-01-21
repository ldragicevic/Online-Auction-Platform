namespace IEP_Projekat.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("order")]
    public partial class order
    {
        [Key]
        public long IDOrd { get; set; }

        [Required]
        [StringLength(128)]
        public string Id { get; set; }

        public int token_number { get; set; }

        public int package_price { get; set; }

        [Required]
        [StringLength(50)]
        public string status { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime create_date_time { get; set; }

        public virtual AspNetUser AspNetUser { get; set; }
    }
}
