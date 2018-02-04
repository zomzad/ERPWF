using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERPWF
{
    public class LogInfo
    {
        public string Applicant { get; set; }
        public string SignedUser { get; set; }
        public int lrec93_form { get; set; }
        public DateTime lrec93_date { get; set; }
        public string lrec93_fsts { get; set; }
        public bool lrec93_hidden { get; set; }
        public string lrec93_bgcolor { get; set; }
        public string lrec93_mstfn { get; set; }
        public DateTime lrec93_mdate { get; set; }
        public string lrec93_desc { get; set; }
    }
}
