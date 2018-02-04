namespace ERPWF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class opagm20
    {
        [Key]
        [StringLength(20)]
        public string stfn_stfn { get; set; }

        public bool stfn_sts { get; set; }

        [Required]
        [StringLength(6)]
        public string stfn_cname { get; set; }

        [StringLength(20)]
        public string stfn_pname { get; set; }

        [Required]
        [StringLength(2)]
        public string stfn_job1 { get; set; }

        [Required]
        [StringLength(2)]
        public string stfn_job2 { get; set; }

        public bool stfn_op { get; set; }

        [StringLength(40)]
        public string stfn_email { get; set; }

        [Required]
        [StringLength(10)]
        public string stfn_pswd { get; set; }

        [StringLength(8)]
        public string stfn_pswd_date { get; set; }

        public short stfn_pswd_errcnt { get; set; }

        public byte stfn_pswd_err { get; set; }

        public DateTime? stfn_pswd_errtime { get; set; }

        public DateTime? stfn_login_date { get; set; }

        [StringLength(15)]
        public string stfn_ip { get; set; }

        public bool stfn_ip_err { get; set; }

        [Required]
        [StringLength(2)]
        public string stfn_comp { get; set; }

        [Required]
        [StringLength(2)]
        public string stfn_prof { get; set; }

        [Required]
        [StringLength(2)]
        public string stfn_team { get; set; }

        [StringLength(4)]
        public string stfn_property { get; set; }

        [StringLength(6)]
        public string stfn_agent { get; set; }

        [StringLength(2)]
        public string stfn_rteam { get; set; }

        public bool stfn_right1 { get; set; }

        public bool stfn_right8 { get; set; }

        public bool stfn_0 { get; set; }

        public bool stfn_t { get; set; }

        public bool stfn_f { get; set; }

        public bool stfn_h { get; set; }

        public bool stfn_e { get; set; }

        public bool stfn_r { get; set; }

        public bool stfn_s { get; set; }

        public bool stfn_b { get; set; }

        public bool stfn_v { get; set; }

        public byte stfn_days { get; set; }

        public byte stfn_9days { get; set; }

        public byte? stfn_localdays { get; set; }

        public byte stfn_tkdays { get; set; }

        public byte stfn_tkdays2 { get; set; }

        [Required]
        [StringLength(1)]
        public string stfn_app0 { get; set; }

        [Required]
        [StringLength(1)]
        public string stfn_app1 { get; set; }

        public bool stfn_apptl { get; set; }

        public bool stfn_applg { get; set; }

        public bool stfn_ragent { get; set; }

        [StringLength(1)]
        public string stfn_weborder { get; set; }

        [StringLength(1)]
        public string stfn_weborder2 { get; set; }

        public short stfn_webpoint1 { get; set; }

        public short stfn_webpoint3 { get; set; }

        public bool? stfn_tcpoint { get; set; }

        [Required]
        [StringLength(1)]
        public string stfn_check_ip { get; set; }

        [StringLength(2)]
        public string stfn_comppub17 { get; set; }

        [StringLength(4)]
        public string stfn_gen31 { get; set; }
    }
}
