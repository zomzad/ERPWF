namespace ERPWF
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class recm93
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int rec93_form { get; set; }

        public bool rec93_sts { get; set; }

        public bool rec93_bsts { get; set; }

        [Required]
        [StringLength(1)]
        public string rec93_fsts { get; set; }

        [Required]
        [StringLength(1)]
        public string rec93_fsts1 { get; set; }

        [Required]
        [StringLength(8)]
        public string rec93_date { get; set; }

        public DateTime? rec93_date2 { get; set; }

        [StringLength(8)]
        public string rec93_pdate { get; set; }

        public short rec93_formno { get; set; }

        [Required]
        [StringLength(50)]
        public string rec93_title { get; set; }

        [Required]
        [StringLength(2)]
        public string rec93_needlion { get; set; }

        [StringLength(20)]
        public string rec93_istfn { get; set; }

        [Required]
        [StringLength(2)]
        public string rec93_prof { get; set; }

        [StringLength(20)]
        public string rec93_stfn { get; set; }

        [Required]
        [StringLength(10)]
        public string rec93_stfnname { get; set; }

        [StringLength(2)]
        public string rec93_pprof { get; set; }

        [StringLength(4)]
        public string rec93_pteam { get; set; }

        [StringLength(20)]
        public string rec93_pstfn { get; set; }

        [StringLength(20)]
        public string rec93_pstfn2 { get; set; }

        [StringLength(12)]
        public string rec93_mstfn { get; set; }

        public DateTime? rec93_mdate { get; set; }
    }
}
