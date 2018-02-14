using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ERPWF
{
    internal class EntityBatch
    {
        private readonly string _conn;

        public EntityBatch(string connStr)
        {
            _conn = connStr;
        }

        /// <summary>
        /// 取得SERP簽核單、ERP簽核名單、ERP聯絡單、EPR聯絡單LOG
        /// </summary>
        /// <returns></returns>
        public DataSet GetSerpSignFormInfoList()
        {
            DataSet ds = new DataSet();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                //取得聯絡單簽核單
                "SELECT rec93_form AS Rec93Form",
                "     , REPLICATE('0', 6) + CONVERT(VARCHAR,CM93.rec93_form) AS SignFormNO",
                "     , CM93.rec93_sts AS IsDisable",
                "     , CM93.rec93_title AS SignFormSubject",
                "     , CM96A.r96a_data1 AS SignFormReason",
                "     , CM96A.r96a_data2 AS SignFormProcess",
                "     , CM96A.r96a_char1 AS SignFormOrderYear",
                "     , CM96A.r96a_char2 AS SignFormOrderNO",
                "     , CM96A.r96a_int1 AS SignFormItem",
                "     , CM96A.r96a_int2 AS SignFormERPWork",
                "     , CM93.rec93_needlion AS SignFormBU",
                "     , CM96A.r96a_char4 AS SignFormPeerComp",
                "     , CM96A.r96a_char3 AS SignFormUserID",
                "     , CM93.rec93_stfn AS SignFormNewUserID",
                "     , CONVERT(datetime,CM93.rec93_date) AS SignFormNewDT",
                "     , CM93.rec93_stfn AS UpdUserID",
                "     , CM93.rec93_mdate AS UPDDT",
                "     , CM93.rec93_fsts AS FSTS",
                "  FROM recm93 CM93",
                "  JOIN recm96a CM96A",
                "    ON CM96A.r96a_form = CM93.rec93_form",
                " WHERE CM93.rec93_formno = '2'",
                "   AND (rec93_sts = '1' OR rec93_fsts = 'F')",
                //"   AND CM93.rec93_form = '10110293'",//指定單號
                " ORDER BY SignFormNO DESC;",

                //包含姓名的簽核清單 並建立索引
                "SELECT stfn_stfn",
                "     , stfn_cname",
                "  INTO #IdxOpagm20",
                "  FROM opagm20",

                "SELECT stfn_cname AS stfnCname",
                "     , rec94_fsts AS rec94Fsts",
                "     , rec94_stfn AS rec94Stfn",
                "     , rec94_form AS rec94Form",
                "     , rec94_no AS rec94NO",
                "  INTO #REC94",
                "  FROM recm94",
                "  JOIN #IdxOpagm20",
                "    ON stfn_stfn = rec94_stfn",
                //" WHERE recm94.rec94_form = '10110293'",//指定單號
                " GROUP BY stfn_cname,rec94_stfn,rec94_form,rec94_fsts,rec94_no",
                " ORDER BY rec94_fsts,rec94_no;",
                "SELECT * FROM #REC94;",

                //簽核單限定在聯絡單
                "SELECT *",
                "  INTO #REC93",
                "  FROM recm93",
                " WHERE rec93_formno = '2'",
                "SELECT * FROM #REC93;",

                //抓出Log紀錄中，單號是聯絡單的部分
                "SELECT lrec93_form AS Lrec93Form",
                "     , lrec93_date AS Lrec93Date",
                "     , lrec93_fsts AS Lrec93Fsts",
                "     , lrec93_hidden AS Lrec93Hidden",
                "     , lrec93_bgcolor AS Lrec93Bgcolor",
                "     , lrec93_mstfn AS Lrec93Mstfn",
                "     , lrec93_mdate AS Lrec93Mdate",
                "     , lrec93_desc AS Lrec93Desc",
                "  INTO #LOGRECM93",
                "  FROM logrecm93",
                "  WHERE logrecm93.lrec93_form IN (SELECT rec93_form FROM #REC93)",
                //"    AND logrecm93.lrec93_form = '10110293'", //指定單號
                "SELECT * FROM #LOGRECM93"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(ds);
                }
            }

            return ds;
        }

        #region - 編輯新工作流程 -
        /// <summary>
        /// 編輯新工作流程
        /// </summary>
        public class NewWFFlowPara
        {
            public string WFNo { get; set; }
            public string SysID { get; set; }
            public string FlowID { get; set; }
            public string FlowVer { get; set; }
            public string Subject { get; set; }
            public string Lot { get; set; }
            public string UserID { get; set; }
            public string EndUserID { get; set; }
            public string NowDT { get; set; }
            public string DTEnd { get; set; }
            public string ResultID { get; set; }
            public string NodeID { get; set; }
            public string UpdUserID { get; set; }
            public DateTime UPDDT { get; set; }
        }

        public class NewWFFlow
        {
            public string WFNo { get; set; }
            public string SignFormNo { get; set; }
        }

        public List<NewWFFlow> WFNoList { get; set; }

        public List<NewWFFlow> EditNewWFFlow(List<NewWFFlowPara> newWFFlowParaList,DataTable newFlowDT)
        {
            DataTable wfNewFlowInfo = new DataTable();
            WFNoList = new List<NewWFFlow>();

            #region - TVP批次新增聯絡單 -
            var newFlowCmd = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SET NOCOUNT ON;",

                "DECLARE @RESULT VARCHAR(50) = 'Success';",
                "DECLARE @ERROR_LINE INT;",
                "DECLARE @ERROR_NUMBER INT;",
                "DECLARE @ERROR_MESSAGE NVARCHAR(4000);",
                "DECLARE @TODAY_YEAR CHAR(4) = CAST(YEAR(GETDATE()) AS CHAR);",
                "DECLARE @TODAY_YMD CHAR(8) = dbo.FN_GET_SYSDATE(NULL);",
                "DECLARE @IS_START_SIG CHAR(1) = NULL;",
                "DECLARE @WF_NO CHAR(14);",
                "DECLARE @WF_NODE_ID VARCHAR(50) = 'ApplySignForm';",
                "DECLARE @REMARK_NO CHAR(3);",
                "DECLARE @SYS_ID VARCHAR(12) = 'PUBAP';",
                "DECLARE @FLOW_ID VARCHAR(50) = 'SignForm';",
                "DECLARE @FLOW_VER CHAR(3) = '001';",

                "BEGIN TRANSACTION",
                "    BEGIN TRY",
                //新增工作流程
                "INSERT INTO WF_FLOW SELECT * FROM @TVPNewFlow;",

                ////新增作業節點
                "INSERT INTO WF_NODE",
                "SELECT WF_NO",
                "     , '001'",
                "     , SYS_ID",
                "     , WF_FLOW_ID",
                "     , WF_FLOW_VER",
                "     , @WF_NODE_ID",
                "     , NEW_USER_ID",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , DT_BEGIN",
                "     , NULL",
                "     , 'P'",
                "     , NULL",
                "     , @IS_START_SIG",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , UPD_USER_ID",
                "     , UPD_DT",
                " FROM @TVPNewFlow;",

                ////新增節點侯選處理人名單
                "INSERT INTO WF_NODE_NEW_USER",
                "SELECT WF_NO",
                "     , '001'",
                "     , SYS_ID",
                "     , WF_FLOW_ID",
                "     , WF_FLOW_VER",
                "     , @WF_NODE_ID",
                "     , NEW_USER_ID",
                "     , UPD_USER_ID",
                "     , UPD_DT",
                " FROM @TVPNewFlow;",

                ////新增備註
                "INSERT INTO dbo.WF_REMARK",
                "SELECT WF_NO",
                "     , '001'",
                "     , '001'",
                "     , SYS_ID",
                "     , WF_FLOW_ID",
                "     , WF_FLOW_VER",
                "     , @WF_NODE_ID",
                "     , 'P'",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NULL",
                "     , NEW_USER_ID",
                "     , DT_BEGIN",
                "     , NULL",
                "     , UPD_USER_ID",
                "     , UPD_DT",
                "  FROM @TVPNewFlow;",
                "   SET @RESULT = 'Success';",
                "       COMMIT;",
                "       END TRY",
                "       BEGIN CATCH",
                "           SET @RESULT = 'Failure';",
                "       SET @ERROR_LINE = ERROR_LINE();",
                "       SET @ERROR_NUMBER = ERROR_NUMBER();",
                "       SET @ERROR_MESSAGE = ERROR_MESSAGE();",
                "       ROLLBACK TRANSACTION;",
                "       END CATCH;",
                "       IF @RESULT = 'Success'",
                "    BEGIN",
                "        SELECT WF_NO AS WFNo",
                "             , WF_NO AS SignFormNo",
                "          FROM @TVPNewFlow;",
                "      END",
                "     ELSE",
                "    BEGIN",
                "        INSERT INTO @RETURN_DATA (Result, ErrorLine, ErrorMessage, ErrorNumber)",
                "        SELECT @RESULT, @ERROR_LINE, @ERROR_MESSAGE, @ERROR_NUMBER;",
                "    END;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand(
                @"IF type_id('[dbo].[NEWFLOW_TYPE]') IS NOT NULL
                    DROP TYPE [dbo].[NEWFLOW_TYPE];

                    CREATE TYPE NEWFLOW_TYPE AS TABLE
                    (
                        WF_NO VARCHAR(14) NOT NULL,
                    	SYS_ID VARCHAR(12) NOT NULL,
                    	WF_FLOW_ID VARCHAR(50) NOT NULL,
                    	WF_FLOW_VER CHAR(3) NOT NULL,
                    	WF_SUBJECT NVARCHAR(150) NOT NULL,
                        LOT NVARCHAR(50),
                        NEW_USER_ID VARCHAR(20) NOT NULL,
                        END_USER_ID VARCHAR(20),
                        DT_BEGIN CHAR(17) NOT NULL,
                        DT_END CHAR(17),
                        RESULT_ID VARCHAR(20) NOT NULL,
                        NODE_NO CHAR(3),
                    	UPD_USER_ID VARCHAR(20),
                        UPD_DT DATETIME
                    );", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand(
                    newFlowCmd.ToString()
                    , connection))
                {
                    SqlParameter tvp = cmd.Parameters.Add("@TVPNewFlow", SqlDbType.Structured);
                    tvp.Value = newFlowDT;
                    tvp.TypeName = "NEWFLOW_TYPE";
                    //SqlParameter[] parameters =
                    //{
                    //    new SqlParameter("@TVPNewFlow", SqlDbType.Structured) { Value = newFlowDT, TypeName = "NEWFLOW_TYPE" }
                    //};

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(wfNewFlowInfo);

                    if (wfNewFlowInfo.Rows.Count > 0)
                    {
                        WFNoList = (from DataRow dr in wfNewFlowInfo.Rows
                                     select new NewWFFlow
                                     {
                                         WFNo = dr.Field<string>("WFNo"),
                                         SignFormNo = dr.Field<string>("SignFormNo")
                                     }).ToList();
                    }
                }
            }
            #endregion

            return WFNoList;
        }
        #endregion

        #region - 結案用-增加註記 -
        public class AddRemarkPara
        {
            public string WFNo { get; set; }
            public string RemarkNO { get; set; }
            public string NodeNum { get; set; }
            public string SysID { get; set; }
            public string FlowID { get; set; }
            public string FlowVer { get; set; }
            public string WFNodeID { get; set; }
            public string NodeNO { get; set; }
            public string RemarkUserID { get; set; }
            public string UpdUserID { get; set; }
            public string Remark { get; set; }
        }

        public void AddRemark(Dictionary<string, List<AddRemarkPara>> addRemarkParaDic)
        {
            SqlConnection conn = new SqlConnection(_conn);
            conn.Open();

            var commandAddRemark = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "DECLARE @REMARK_NO CHAR(3);",
                "DECLARE @NEW_USER_ID VARCHAR(20) = NULL;",
                "DECLARE @NOW_DATETIME CHAR(17) = dbo.FN_GET_SYSDATE(NULL) + dbo.FN_GET_SYSTIME(NULL);"
            }));

            foreach (var reamarkInfo in addRemarkParaDic)
            {
                #region - 取得REMARK基礎編號 -
                commandAddRemark.AppendLine(string.Join(Environment.NewLine, new object[]
                {
                    " SELECT @REMARK_NO = MAX(REMARK_NO)",
                    "   FROM WF_REMARK",
                    "  WHERE WF_NO =" + reamarkInfo.Key + ";",
                    "SET @REMARK_NO = RIGHT('00' + CAST(ISNULL(CAST(@REMARK_NO AS INT), 0) + 1 AS VARCHAR), 3);"
                }));
                #endregion

                foreach (var remark in reamarkInfo.Value)
                {
                    commandAddRemark.AppendLine(string.Join(Environment.NewLine, new object[]
                    {
                        " INSERT INTO WF_REMARK",
                        "      ( WF_NO",
                        "      , NODE_NO",
                        "      , REMARK_NO",
                        "      , SYS_ID",
                        "      , WF_FLOW_ID",
                        "      , WF_FLOW_VER",
                        "      , WF_NODE_ID",
                        "      , NODE_RESULT_ID",
                        "      , BACK_WF_NODE_ID",
                        "      , SIG_STEP",
                        "      , WF_SIG_SEQ",
                        "      , SIG_DATE",
                        "      , SIG_RESULT_ID",
                        "      , DOC_NO",
                        "      , WF_DOC_SEQ",
                        "      , DOC_DATE",
                        "      , DOC_IS_DELETE",
                        "      , REMARK_USER_ID",
                        "      , REMARK_DATE",
                        "      , REMARK",
                        "      , UPD_USER_ID",
                        "      , UPD_DT",
                        "      ) ",
                        " VALUES",
                        "      ('" + remark.WFNo + "'",
                        "      , '" + remark.NodeNO + "'",
                        "      , @REMARK_NO",
                        "      , '" + remark.SysID + "'",
                        "      , '" + remark.FlowID + "'",
                        "      , '" + remark.FlowVer + "'",
                        "      , '" + remark.WFNodeID + "'",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , NULL",
                        "      , '" + remark.RemarkUserID + "'",
                        "      , @NOW_DATETIME",
                        "      , '" + (string.IsNullOrWhiteSpace(remark.Remark) ? "NULL" : remark.Remark) + "'",
                        "      , '" + remark.UpdUserID + "'",
                        "      , GETDATE()",
                        "      );",
                        "SET @REMARK_NO = RIGHT('00' + CAST(CAST(@REMARK_NO AS INT) + 1 AS VARCHAR),3);"
                    }));
                }
            }

            SqlCommand comm = new SqlCommand(commandAddRemark.ToString(), conn) { CommandTimeout = 120 };

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            comm.ExecuteNonQuery();

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            conn.Dispose();
            comm.Dispose();
        }
        #endregion

        #region - 檢查文件 -
        public class WFFilePara
        {
            public string WFNo { get; set; }
        }

        public class WFFile
        {
            public string FilePath { get; set; }
        }

        public List<WFFile> CheckWFFile(WFFilePara para)
        {
            DataTable tableRow = new DataTable();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SELECT rec95_file AS FilePath",
                "  FROM recm95",
                " WHERE rec95_form = @WF_NO"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(tableRow);
                }
            }
            return tableRow.ToList<WFFile>().ToList();
        }
        #endregion

        #region - 新增文件 -
        public class AddDocumentPara
        {
            public string WFNo { get; set; }
            public string NodeNO { get; set; }
            public string WFDocSeq { get; set; }
            public string DocUserID { get; set; }
            public string DocFileNM { get; set; }
            public string DocEncodeNM { get; set; }
            public string DocPath { get; set; }
            public string DocLocalPath { get; set; }
            public string UpdUserID { get; set; }
            public string Remark { get; set; }
        }

        public class AddDocumentResult
        {
            public string Result;
        }

        public AddDocumentResult AddDocument(AddDocumentPara para)
        {
            DataTable tableRow = new DataTable();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "EXECUTE dbo.SP_WF_ADD_DOCUMENT @WF_NO, @NODE_NO, @WF_DOC_SEQ" +
                ", @DOC_USER_ID, @DOC_FILE_NAME, @DOC_ENCODE_NAME, @DOC_PATH, @DOC_LOCAL_PATH" +
                ", @UPD_USER_ID, @REMARK;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@NODE_NO", para.NodeNO);
                    cmd.Parameters.AddWithValue("@WF_DOC_SEQ", para.WFDocSeq);
                    cmd.Parameters.AddWithValue("@DOC_USER_ID", para.DocUserID);
                    cmd.Parameters.AddWithValue("@DOC_FILE_NAME", para.DocFileNM);
                    cmd.Parameters.AddWithValue("@DOC_ENCODE_NAME", para.DocEncodeNM);
                    cmd.Parameters.AddWithValue("@DOC_PATH", para.DocPath);
                    cmd.Parameters.AddWithValue("@DOC_LOCAL_PATH", para.DocLocalPath);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    cmd.Parameters.AddWithValue("@REMARK", para.Remark);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(tableRow);
                }
            }
            return tableRow.ToList<AddDocumentResult>().ToList().SingleOrDefault();
        }
        #endregion

        #region - 新增聯絡單 -
        /// <summary>
        /// 新增聯絡單
        /// </summary>
        /// <returns></returns>
        public bool AddSignForm(List<SignForm> signFormParaList)
        {
            var commandAddSignForm = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "        INSERT INTO ZD223_SIGN_FORM",
                "             ( SING_FORM_NO",
                "             , SING_FORM_WFNO",
                "             , SIGN_FORM_TYPE",
                "             , IS_DISABLE",
                "             , SIGN_FORM_SUBJECT",
                "             , SIGN_FORM_REASON",
                "             , SIGN_FORM_PROCESS",
                "             , SIGN_FORM_ORDER_YEAR",
                "             , SIGN_FORM_ORDER_NO",
                "             , SIGN_FORM_ITEM",
                "             , SIGN_FORM_ERPWORK",
                "             , SIGN_FORM_BU",
                "             , SIGN_PEER_COMP",
                "             , SING_FORM_USER_ID",
                "             , SING_FORM_NEW_USER_ID",
                "             , SING_FORM_NEW_DT",
                "             , UPD_USER_ID",
                "             , UPD_DT",
                "             )",
                "        VALUES",
                "             ( @SING_FORM_NO",
                "             , @SING_FORM_WFNO",
                "             , @SIGN_FORM_TYPE",
                "             , @IS_DISABLE",
                "             , @SIGN_FORM_SUBJECT",
                "             , @SIGN_FORM_REASON",
                "             , @SIGN_FORM_PROCESS",
                "             , @SIGN_FORM_ORDER_YEAR",
                "             , @SIGN_FORM_ORDER_NO",
                "             , @SIGN_FORM_ITEM",
                "             , @SIGN_FORM_ERPWORK",
                "             , @SIGN_FORM_BU",
                "             , @SIGN_PEER_COMP",
                "             , @SING_FORM_USER_ID",
                "             , @SING_FORM_NEW_USER_ID",
                "             , @SING_FORM_NEW_DT",
                "             , @UPD_USER_ID",
                "             , GETDATE())"
            }));

            SqlConnection conn = new SqlConnection(_conn);
            SqlCommand cmd = new SqlCommand(commandAddSignForm.ToString(), conn);
            conn.Open();

            foreach (var sign in signFormParaList.Select((value, index) => new { Value = value, Index = index }))
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@SING_FORM_NO", sign.Value.SignFormNO);
                cmd.Parameters.AddWithValue("@SING_FORM_WFNO", sign.Value.SignFormWFNO);
                cmd.Parameters.AddWithValue("@SIGN_FORM_TYPE", sign.Value.SignFormType);
                cmd.Parameters.AddWithValue("@IS_DISABLE", sign.Value.IsDisable ? "Y" : "N");
                cmd.Parameters.AddWithValue("@SIGN_FORM_SUBJECT", sign.Value.SignFormSubject);
                cmd.Parameters.AddWithValue("@SIGN_FORM_REASON", sign.Value.SignFormReason);
                cmd.Parameters.AddWithValue("@SIGN_FORM_PROCESS", sign.Value.SignFormProcess);
                cmd.Parameters.AddWithValue("@SIGN_FORM_ORDER_YEAR", sign.Value.SignFormOrderYear);
                cmd.Parameters.AddWithValue("@SIGN_FORM_ORDER_NO", sign.Value.SignFormOrderNO);
                cmd.Parameters.AddWithValue("@SIGN_FORM_ITEM", sign.Value.SignFormItem);
                cmd.Parameters.AddWithValue("@SIGN_FORM_ERPWORK", sign.Value.SignFormERPWork);
                cmd.Parameters.AddWithValue("@SIGN_FORM_BU", sign.Value.SignFormBU);
                cmd.Parameters.AddWithValue("@SIGN_PEER_COMP", sign.Value.SignFormPeerComp);
                cmd.Parameters.AddWithValue("@SING_FORM_USER_ID", sign.Value.SignFormUserID);
                cmd.Parameters.AddWithValue("@SING_FORM_NEW_USER_ID", sign.Value.SignFormNewUserID);
                cmd.Parameters.AddWithValue("@SING_FORM_NEW_DT", sign.Value.SignFormNewDT);
                cmd.Parameters.AddWithValue("@UPD_USER_ID", sign.Value.UpdUserID);
                var exeNum = cmd.ExecuteNonQuery();

                if ((exeNum > 0) == false)
                {
                    return false;
                }
            }
            conn.Dispose();
            cmd.Dispose();

            return true;
        }
        #endregion

        #region - 取得員工編號 -
        public class UserIDPara
        {
            public string UserNM { get; set; }
        }

        public class UserID
        {
            public string STFN { get; set; }
        }

        public List<UserID> GetUserID(UserIDPara para)
        {
            DataTable tableRow = new DataTable();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SELECT stfn_stfn AS STFN",
                "  FROM opagm20 OPGM",
                " WHERE (CHARINDEX(stfn_pname,@USER_NM_STR) > 0 OR CHARINDEX(stfn_cname,@USER_NM_STR) > 0)",
                "   AND (SELECT prof_prof FROM ispfm00",
                " WHERE prof_dname = SUBSTRING(@USER_NM_STR, 1, CASE WHEN (CHARINDEX(OPGM.stfn_pname, @USER_NM_STR) - 1) < 0 THEN 0 ELSE (CHARINDEX(OPGM.stfn_pname, @USER_NM_STR) - 1) END)) = stfn_prof"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@USER_NM_STR", para.UserNM);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(tableRow);
                }
            }
            return tableRow.ToList<UserID>().ToList();
        }
        #endregion

        #region - 編輯簽核名單 -
        /// <summary>
        /// 編輯簽核名單
        /// </summary>
        public class SetWFSignaturePara
        {
            public string WFNo { get; set; }
            public bool IsStartSig { get; set; }
            public string UpdUserID { get; set; }
            public List<SetSigValue> WFSigList { get; set; }
        }

        public class SetSigValue
        {
            public int SigStep { get; set; }
            public string WFSigSeq { get; set; }
            public string SigUserID { get; set; }
            public string UpdUserID { get; set; }
        }

        public class SetWFSignatureResult
        {
            public string Result { get; set; }
        }

        public void SetWFSignature(List<SetWFSignaturePara> setWFSignatureParaList)
        {
            StringBuilder command = new StringBuilder();

            foreach (var wfPara in setWFSignatureParaList)
            {
                foreach (SetSigValue nodeNewUserPara in wfPara.WFSigList)
                {
                    command.AppendLine(string.Join(Environment.NewLine, new object[]
                    {
                        " INSERT INTO @WF_SIG_LIST",
                        "      ( SIG_STEP",
                        "      , WF_SIG_SEQ",
                        "      , SIG_USER_ID",
                        "      , UPD_USER_ID",
                        "      ) ",
                        " VALUES",
                        "      ( '" + nodeNewUserPara.SigStep + "'",
                        "      , '" + nodeNewUserPara.WFSigSeq + "'",
                        "      , '" + nodeNewUserPara.SigUserID + "'",
                        "      , '" + wfPara.UpdUserID + "'",
                        "      );"
                    }));

                    var commandSetSignature = new StringBuilder(string.Join(Environment.NewLine, new object[]
                    {
                        "DECLARE @WF_SIG_LIST WF_SIG_TYPE",
                        command.ToString(),
                        "EXECUTE dbo.SP_WF_SET_SIGNATURE @WF_NO, @IS_START_SIG, @UPD_USER_ID, @WF_SIG_LIST;"
                    }));

                    SqlConnection conn = new SqlConnection(_conn);
                    SqlCommand cmd = new SqlCommand(commandSetSignature.ToString(), conn);
                    conn.Open();

                    foreach (var wfSign in setWFSignatureParaList.Select((value, index) => new { Value = value, Index = index }))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@WF_NO", wfSign.Value.WFNo);
                        cmd.Parameters.AddWithValue("@IS_START_SIG", wfSign.Value.IsStartSig);
                        cmd.Parameters.AddWithValue("@UPD_USER_ID", wfSign.Value.UpdUserID);
                        cmd.ExecuteNonQuery();
                    }
                    conn.Dispose();
                    cmd.Dispose();
                }
            }
        }
        #endregion

        #region - 取得目前結點 -
        /// <summary>
        /// 取得目前結點
        /// </summary>
        public class RunTimeWfFlowPara
        {
            public string WFNo { get; set; }
        }

        public string GetRunTimeWFFlow(RunTimeWfFlowPara para)
        {
            DataTable wfSigInfo = new DataTable();
            var commandSignature = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SELECT NODE_NO",
                "  FROM WF_FLOW",
                " WHERE WF_NO = @WF_NO"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandSignature.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(wfSigInfo);
                }
            }

            if (wfSigInfo.Rows.Count == 1)
            {
                return wfSigInfo.Rows[0].Field<string>("NODE_NO");
            }

            return string.Empty;
        }
        #endregion

        #region - 增加註記 -
        public class AddWFRemarkPara
        {
            public string WFNo { get; set; }
            public string UserID { get; set; }
            public string UpdUserID { get; set; }
            public string Remark { get; set; }
        }

        public void AddWFRemark(List<AddWFRemarkPara> addWFRemarkParaList)
        {
            int exeNum;
            var commandAddRemark = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "DECLARE @REMARK_NO CHAR(3);",
                "DECLARE @NODE_NO CHAR(3) = NULL;",
                "DECLARE @SYS_ID VARCHAR(6) = NULL;",
                "DECLARE @WF_FLOW_ID VARCHAR(50) = NULL;",
                "DECLARE @WF_FLOW_VER VARCHAR(50) = NULL;",
                "DECLARE @WF_NODE_ID VARCHAR(50) = NULL;",
                "DECLARE @NEW_USER_ID VARCHAR(20) = NULL;",

                "DECLARE @NOW_DATETIME CHAR(17) = dbo.FN_GET_SYSDATE(NULL) + dbo.FN_GET_SYSTIME(NULL);",
                " SELECT @REMARK_NO = MAX(REMARK_NO)",
                "   FROM WF_REMARK",
                "  WHERE WF_NO = @WF_NO;",

                " SELECT @NODE_NO = D.NODE_NO",
                "      , @SYS_ID = D.SYS_ID",
                "      , @WF_FLOW_ID = D.WF_FLOW_ID",
                "      , @WF_FLOW_VER = D.WF_FLOW_VER",
                "      , @WF_NODE_ID = D.WF_NODE_ID",
                "      , @NEW_USER_ID = D.NEW_USER_ID",
                "  FROM dbo.FNTB_GET_WF_NODE(@WF_NO) D",

                "SET @REMARK_NO = RIGHT('00' + CAST(ISNULL(CAST(@REMARK_NO AS INT), 0) + 1 AS VARCHAR), 3)",

                " INSERT INTO WF_REMARK",
                "      ( WF_NO",
                "      , NODE_NO",
                "      , REMARK_NO",
                "      , SYS_ID",
                "      , WF_FLOW_ID",
                "      , WF_FLOW_VER",
                "      , WF_NODE_ID",
                "      , NODE_RESULT_ID",
                "      , BACK_WF_NODE_ID",
                "      , SIG_STEP",
                "      , WF_SIG_SEQ",
                "      , SIG_DATE",
                "      , SIG_RESULT_ID",
                "      , DOC_NO",
                "      , WF_DOC_SEQ",
                "      , DOC_DATE",
                "      , DOC_IS_DELETE",
                "      , REMARK_USER_ID",
                "      , REMARK_DATE",
                "      , REMARK",
                "      , UPD_USER_ID",
                "      , UPD_DT",
                "      ) ",
                " VALUES",
                "      ( @WF_NO",
                "      , @NODE_NO",
                "      , @REMARK_NO",
                "      , @SYS_ID",
                "      , @WF_FLOW_ID",
                "      , @WF_FLOW_VER",
                "      , @WF_NODE_ID",
                "      , 'P'",
                "      , NULL",
                "      , NULL",
                "      , NULL",
                "      , NULL",
                "      , NULL",
                "      , NULL",
                "      , NULL",
                "      , NULL",
                "      , NULL",
                "      , @USER_ID",
                "      , @NOW_DATETIME",
                "      , @REMARK",
                "      , @UPD_USER_ID",
                "      , GETDATE()",
                "      )"
            }));

            SqlConnection conn = new SqlConnection(_conn);
            SqlCommand cmd = new SqlCommand(commandAddRemark.ToString(), conn);
            conn.Open();

            foreach (var wfRemark in addWFRemarkParaList.Select((value, index) => new { Value = value, Index = index }))
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@WF_NO", wfRemark.Value.WFNo);
                cmd.Parameters.AddWithValue("@USER_ID", wfRemark.Value.UserID);
                cmd.Parameters.AddWithValue("@UPD_USER_ID", wfRemark.Value.UpdUserID);
                cmd.Parameters.AddWithValue("@REMARK", wfRemark.Value.Remark);
            }
            conn.Dispose();
            cmd.Dispose();
        }
        #endregion

        #region - 簽核 -
        /// <summary>
        /// 簽核
        /// </summary>
        public class WFSignaturePara
        {
            public string WFNo { get; set; }
            public string NodeNO { get; set; }
            public string UserID { get; set; }
            public string SigResultID { get; set; }
        }

        public class WFSignature
        {
            public string Result { get; set; }
        }

        public WFSignature EditWFSignature(List<WFSignaturePara> wfSignatureParaList)
        {
            DataTable wfSigInfo = new DataTable();

            var commandSignature = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "EXECUTE dbo.SP_WF_SIGNATURE @WF_NO, @NODE_NO, @USER_ID, @SIG_RESULT_ID, @SIG_COMMENT, @UPD_USER_ID;"
            }));

            SqlConnection conn = new SqlConnection(_conn);
            SqlCommand cmd = new SqlCommand(commandSignature.ToString(), conn);
            conn.Open();

            foreach (var signature in wfSignatureParaList.Select((value, index) => new { Value = value, Index = index }))
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@WF_NO", signature.Value.WFNo);
                cmd.Parameters.AddWithValue("@NODE_NO", signature.Value.NodeNO);
                cmd.Parameters.AddWithValue("@USER_ID", signature.Value.UserID);
                cmd.Parameters.AddWithValue("@SIG_RESULT_ID", signature.Value.SigResultID);
                cmd.Parameters.AddWithValue("@SIG_COMMENT", "NULL");
                cmd.Parameters.AddWithValue("@UPD_USER_ID", "NULL");
            }
            conn.Dispose();
            cmd.Dispose();

            return wfSigInfo.ToList<WFSignature>().SingleOrDefault();
        }
        #endregion

        #region - 結束節點 -
        public class ToEndNodePara
        {
            public string WFNo { get; set; }
            public string NodeNO { get; set; }
            public string UserID { get; set; }
            public string UpdUserID { get; set; }
        }

        public class ToEndNode
        {
            public string Result { get; set; }
        }

        public ToEndNode EditToEndNode(List<ToEndNodePara> toEndNodeParaList)
        {
            DataTable editToEndNodeInfo = new DataTable();

            var commandToEndNode = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "DECLARE @RESULT CHAR(1) = 'N'; ",
                "DECLARE @ERROR_LINE INT;",
                "DECLARE @ERROR_MESSAGE NVARCHAR(4000);",

                "DECLARE @SYS_ID VARCHAR(6) = NULL; ",
                "DECLARE @WF_FLOW_ID VARCHAR(50) = NULL; ",
                "DECLARE @WF_FLOW_VER VARCHAR(50) = NULL; ",
                "DECLARE @WF_NODE_ID VARCHAR(50) = NULL; ",
                "DECLARE @REMARK_NO CHAR(3); ",

                "DECLARE @NOW_DATETIME CHAR(17) = dbo.FN_GET_SYSDATE(NULL) + dbo.FN_GET_SYSTIME(NULL); ",

                "SELECT @REMARK_NO = MAX(REMARK_NO) ",
                "  FROM WF_REMARK ",
                " WHERE WF_NO = @WF_NO;",

                "SELECT @SYS_ID = D.SYS_ID ",
                "     , @WF_FLOW_ID = D.WF_FLOW_ID ",
                "     , @WF_FLOW_VER = D.WF_FLOW_VER ",
                "     , @WF_NODE_ID = D.WF_NODE_ID ",
                "  FROM dbo.FNTB_GET_WF_NODE(@WF_NO) D ",

                "BEGIN TRANSACTION ",
                "    BEGIN TRY ",
                "        UPDATE WF_NODE ",
                "           SET END_USER_ID = @USER_ID ",
                "             , DT_END = @NOW_DATETIME ",
                "             , RESULT_ID = 'F'",
                "             , UPD_USER_ID = @UPD_USER_ID ",
                "             , UPD_DT = GETDATE() ",
                "         WHERE WF_NO = @WF_NO ",
                "           AND NODE_NO = @NODE_NO; ",

                "        UPDATE WF_FLOW ",
                "           SET END_USER_ID = @USER_ID",
                "             , DT_END = @NOW_DATETIME",
                "             , RESULT_ID = 'F'",
                "             , NODE_NO = NULL ",
                "             , UPD_USER_ID = @UPD_USER_ID",
                "             , UPD_DT = GETDATE() ",
                "         WHERE WF_NO = @WF_NO; ",

                #region - 新增備註 -
                "        SET @REMARK_NO = RIGHT('00' + CAST(ISNULL(CAST(@REMARK_NO AS INT), 0) + 1 AS VARCHAR), 3) ",
                "        INSERT INTO dbo.WF_REMARK (",
                "               WF_NO, NODE_NO, REMARK_NO, SYS_ID, WF_FLOW_ID, WF_FLOW_VER, WF_NODE_ID, NODE_RESULT_ID, BACK_WF_NODE_ID",
                "             , SIG_STEP, WF_SIG_SEQ, SIG_DATE, SIG_RESULT_ID",
                "             , DOC_NO, WF_DOC_SEQ, DOC_DATE, DOC_IS_DELETE",
                "             , REMARK_USER_ID, REMARK_DATE, REMARK",
                "             , UPD_USER_ID, UPD_DT",
                "        ) VALUES (",
                "               @WF_NO, @NODE_NO, @REMARK_NO, @SYS_ID, @WF_FLOW_ID, @WF_FLOW_VER, @WF_NODE_ID, 'F', NULL",
                "             , NULL, NULL, NULL, NULL",
                "             , NULL, NULL, NULL, NULL",
                "             , @USER_ID, @NOW_DATETIME, NULL",
                "             , @UPD_USER_ID, GETDATE()",
                "        )",
                #endregion

                "        SET @RESULT = 'Y'; ",
                "        COMMIT; ",
                "    END TRY ",
                "    BEGIN CATCH ",
                "        SET @RESULT = 'N';",
                "        SET @ERROR_LINE = ERROR_LINE();",
                "        SET @ERROR_MESSAGE = ERROR_MESSAGE();",
                "        ROLLBACK TRANSACTION; ",
                "    END CATCH; ",

                "IF @RESULT='Y' ",
                "BEGIN ",
                "    SELECT @WF_NO AS WF_NO ",
                "         , NULL AS NODE_NO ",
                "         , SYS_ID ",
                "         , WF_FLOW_ID AS FLOW_ID ",
                "         , WF_FLOW_VER AS FLOW_VER ",
                "         , NULL AS NODE_ID ",
                "         , 'E' AS NODE_TYPE ",
                "         , NULL AS FUN_SYS_ID ",
                "         , NULL AS SUB_SYS_ID ",
                "         , NULL AS FUN_CONTROLLER_ID ",
                "         , NULL AS FUN_ACTION_NAME ",
                "         , @NOW_DATETIME AS DTEnd ",
                "         , @RESULT AS Result ",
                "      FROM WF_NODE ",
                "     WHERE WF_NO = @WF_NO ",
                "       AND NODE_NO = @NODE_NO;  ",
                "END; ",
                "ELSE ",
                "BEGIN ",
                "    SELECT @RESULT AS Result, @ERROR_LINE AS ErrorLine, @ERROR_MESSAGE AS ErrorMessage; ",
                "END; "
            }));


            SqlConnection conn = new SqlConnection(_conn);
            SqlCommand cmd = new SqlCommand(commandToEndNode.ToString(), conn);
            conn.Open();

            foreach (var end in toEndNodeParaList.Select((value, index) => new { Value = value, Index = index }))
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@WF_NO", end.Value.WFNo);
                cmd.Parameters.AddWithValue("@NODE_NO", end.Value.NodeNO);
                cmd.Parameters.AddWithValue("@USER_ID", end.Value.UserID);
                cmd.Parameters.AddWithValue("@UPD_USER_ID", end.Value.UpdUserID);
            }
            conn.Dispose();
            cmd.Dispose();

            return editToEndNodeInfo.ToList<ToEndNode>().SingleOrDefault();
        }
        #endregion

        #region - 移至下一結點 -
        /// <summary>
        /// 移至下一結點
        /// </summary>
        public class NextToNodePara
        {
            public string NewUserID { get; set; }
            public string UserID { get; set; }
            public string UpdUserID { get; set; }
            public string WFNo { get; set; }
            public List<NodeNewUserPara> NodeUserParaList { get; set; }
        }

        public class NodeNewUserPara
        {
            public string NewUserID;
        }

        public bool NextToProcessNode(NextToNodePara para)
        {
            DataTable tableRow = new DataTable();
            StringBuilder command = new StringBuilder();

            foreach (NodeNewUserPara NodeUserPara in para.NodeUserParaList)
            {
                command.Append(string.Join(Environment.NewLine, new object[]
                {
                    " INSERT INTO @USER_LIST",
                    "      ( USER_ID",
                    "      ) ",
                    " VALUES",
                    "      ( '" + NodeUserPara.NewUserID + "'",
                    "      );"
                }));
            }

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "DECLARE @USER_LIST USER_TYPE;",
                command.ToString(),
                "EXECUTE dbo.SP_WF_NEXT_TO_PROCESS_NODE @WF_NO, @USER_ID, @NEW_USER_ID, @UPD_USER_ID, @USER_LIST;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@USER_ID", para.UserID);
                    cmd.Parameters.AddWithValue("@NEW_USER_ID", para.NewUserID);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(tableRow);
                }
            }

            return true;
        }
        #endregion

        #region - 成為當節點處理人 -
        public class EditWFNodeProcessUserIDPara
        {
            public string WFNo { get; set; }
            public string UserID { get; set; }
            public string NewUserID { get; set; }
            public string UpdUserID { get; set; }
        }

        public class EditWFNodeProcessUserIDResult
        {
            public string Result { get; set; }
        }

        public EditWFNodeProcessUserIDResult EditWFNodeProcessUserID(EditWFNodeProcessUserIDPara para)
        {
            DataTable wFNodeProcessUserIDInfo = new DataTable();

            var commandSignature = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "EXECUTE dbo.SP_WF_EDIT_NODE @WF_NO, @USER_ID, @NEW_USER_ID, @UPD_USER_ID;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandSignature.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@USER_ID", para.UserID);
                    cmd.Parameters.AddWithValue("@NEW_USER_ID", para.NewUserID);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(wFNodeProcessUserIDInfo);
                }
            }

            return wFNodeProcessUserIDInfo.ToList<EditWFNodeProcessUserIDResult>().SingleOrDefault();
        }
        #endregion
    }
}
