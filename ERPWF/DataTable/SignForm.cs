using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPWF
{
    public class SignForm
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column(TypeName = "INT")]
        public int Rec93Form { get; set; }

        [Column(TypeName = "CHAR")]
        [StringLength(14)]
        public string SignFormNO { get; set; }

        [Column(TypeName = "CHAR")]
        [StringLength(14)]
        public string SignFormWFNO { get; set; }

        [Column(TypeName = "CHAR")]
        [StringLength(1)]
        public string SignFormType { get; set; }

        [Column(TypeName = "BIT")]
        public bool IsDisable { get; set; }

        [Column(TypeName = "NVARCHAR")]
        [StringLength(100)]
        public string SignFormSubject { get; set; }

        [Column(TypeName = "NVARCHAR")]
        [StringLength(500)]
        public string SignFormReason { get; set; }

        [Column(TypeName = "NVARCHAR")]
        [StringLength(2000)]
        public string SignFormProcess { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(4)]
        public string SignFormOrderYear { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(10)]
        public string SignFormOrderNO { get; set; }

        [Column(TypeName = "TINYINT")]
        public byte? SignFormItem { get; set; }

        [Column(TypeName = "TINYINT")]
        public byte? SignFormERPWork { get; set; }

        [Column(TypeName = "CHAR")]
        [StringLength(1)]
        public string SignFormBU { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(6)]
        public string SignFormPeerComp { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(6)]
        public string SignFormUserID { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(6)]
        public string SignFormNewUserID { get; set; }

        [Column(TypeName = "DATETIME")]
        public DateTime SignFormNewDT { get; set; }

        [Column(TypeName = "VARCHAR")]
        public string UpdUserID { get; set; }

        [Column(TypeName = "DATETIME")]
        public DateTime UPDDT { get; set; }

        [Column(TypeName = "CHAR")]
        public string FSTS { get; set; }
    }
}
