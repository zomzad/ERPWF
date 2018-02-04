namespace ERPWF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class recm96a
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int r96a_form { get; set; }

        public bool? r96a_bit1 { get; set; }

        public byte? r96a_int1 { get; set; }

        public byte? r96a_int2 { get; set; }

        public byte? r96a_int3 { get; set; }

        [StringLength(200)]
        public string r96a_data1 { get; set; }

        [StringLength(300)]
        public string r96a_data2 { get; set; }

        [StringLength(300)]
        public string r96a_data3 { get; set; }

        [StringLength(100)]
        public string r96a_data4 { get; set; }

        [StringLength(20)]
        public string r96a_data5 { get; set; }

        [StringLength(20)]
        public string r96a_data6 { get; set; }

        [StringLength(20)]
        public string r96a_data7 { get; set; }

        [StringLength(20)]
        public string r96a_data8 { get; set; }

        [StringLength(30)]
        public string r96a_data9 { get; set; }

        [StringLength(100)]
        public string r96a_data10 { get; set; }

        [StringLength(300)]
        public string r96a_data11 { get; set; }

        [StringLength(300)]
        public string r96a_data12 { get; set; }

        [StringLength(200)]
        public string r96a_data13 { get; set; }

        [StringLength(30)]
        public string r96a_data14 { get; set; }

        [StringLength(100)]
        public string r96a_data15 { get; set; }

        [StringLength(100)]
        public string r96a_data16 { get; set; }

        [StringLength(10)]
        public string r96a_char1 { get; set; }

        [StringLength(10)]
        public string r96a_char2 { get; set; }

        [StringLength(10)]
        public string r96a_char3 { get; set; }

        [StringLength(10)]
        public string r96a_char4 { get; set; }

        [StringLength(10)]
        public string r96a_char5 { get; set; }

        [StringLength(10)]
        public string r96a_char6 { get; set; }

        [StringLength(10)]
        public string r96a_char7 { get; set; }

        [StringLength(10)]
        public string r96a_char8 { get; set; }

        public DateTime? r96a_datetime1 { get; set; }

        public DateTime? r96a_datetime2 { get; set; }

        public DateTime? r96a_datetime3 { get; set; }
    }
}
