namespace IEP_Projekat.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("bid")]
    public partial class bid
    {
        [Key]
        public long IDBid { get; set; }

        [Required]
        [StringLength(128)]
        public string Id { get; set; }

        [Required]
        public long IDAuc { get; set; }

        [Required]
        public long tokens { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime bidTime { get; set; }

        public virtual AspNetUser AspNetUser { get; set; }

        public virtual auction auction { get; set; }
    }
}
