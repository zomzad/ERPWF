using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace ERPWF
{
    public class Entity
    {
        private readonly string _conn;

        public Entity(string connStr)
        {
            _conn = connStr;
        }

        public class ERPLogPara
        {
            public List<string> FormList { get; set; }
        }

        #region - 取得SERP簽核單清單 -
        public class SerpSignFormListPara
        {
            public string IsDisableUserID { get; set; }
        }

        /// <summary>
        /// 取得SERP簽核單清單
        /// </summary>
        /// <returns></returns>
        public List<SignForm> GetSerpSignFormList()
        {
            DataTable tableRow = new DataTable();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SELECT rec93_form AS Rec93Form",
                "     , ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2)",
                "             + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8)) AS SignFormNO",
                "     , ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2)",
                "             + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8)) AS SignFormWFNO",
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
                "   AND CM93.rec93_form = '14110986'",
                " ORDER BY SignFormNO DESC;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(tableRow);
                }
            }

            return tableRow.ToList<SignForm>().ToList();
        }
        #endregion

        #region - 新增聯絡單 -
        /// <summary>
        /// 新增聯絡單
        /// </summary>
        /// <returns></returns>
        public bool AddSignForm(SignForm para)
        {
            int exeNum;
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

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandAddSignForm.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@SING_FORM_NO", para.SignFormNO);
                    cmd.Parameters.AddWithValue("@SING_FORM_WFNO", para.SignFormWFNO);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_TYPE", para.SignFormType);
                    cmd.Parameters.AddWithValue("@IS_DISABLE", para.IsDisable ? "Y" : "N");
                    cmd.Parameters.AddWithValue("@SIGN_FORM_SUBJECT", para.SignFormSubject);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_REASON", para.SignFormReason);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_PROCESS", para.SignFormProcess);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_ORDER_YEAR", para.SignFormOrderYear);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_ORDER_NO", para.SignFormOrderNO);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_ITEM", para.SignFormItem);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_ERPWORK", para.SignFormERPWork);
                    cmd.Parameters.AddWithValue("@SIGN_FORM_BU", para.SignFormBU);
                    cmd.Parameters.AddWithValue("@SIGN_PEER_COMP", para.SignFormPeerComp);
                    cmd.Parameters.AddWithValue("@SING_FORM_USER_ID", para.SignFormUserID);
                    cmd.Parameters.AddWithValue("@SING_FORM_NEW_USER_ID", para.SignFormNewUserID);
                    cmd.Parameters.AddWithValue("@SING_FORM_NEW_DT", para.SignFormNewDT);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    exeNum = cmd.ExecuteNonQuery();
                }
            }

            return exeNum > 0;
        }
        #endregion

        #region - 取得電子簽核LOG檔 -
        /// <summary>
        /// 取得電子簽核LOG檔
        /// </summary>
        /// <returns></returns>
        public class ERPSigLogPara
        {
            public string Rec93Form { get; set; }
        }

        public class ERPSigLog
        {
            public string rec93_stfn { get; set; }
            public string IsRemark { get; set; }
            public string stfn_stfn { get; set; }
            public int lrec93_form { get; set; }
            public DateTime lrec93_date { get; set; }
            public string lrec93_fsts { get; set; }
            public bool lrec93_hidden { get; set; }
            public string lrec93_bgcolor { get; set; }
            public string lrec93_mstfn { get; set; }
            public DateTime lrec93_mdate { get; set; }
            public string lrec93_desc { get; set; }
        }

        public DataTable GetERPSigLog(ERPSigLogPara para)
        {
            var erpSigLog = new DataTable();
            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                //"SELECT rec93_stfn",
                //"     , CASE WHEN recm94.rec94_stfn IS NULL THEN 'N' ELSE 'Y' END AS IsRemark",
                //"     , OPGM20.stfn_stfn AS stfn_stfn",
                //"     , lrec93_form",
                //"     , lrec93_date",
                //"     , lrec93_fsts",
                //"     , lrec93_hidden",
                //"     , lrec93_bgcolor",
                //"     , lrec93_mstfn",
                //"     , lrec93_mdate",
                //"     , lrec93_desc",
                //"  FROM logrecm93",
                //"  JOIN recm93",
                //"    ON lrec93_form = rec93_form",
                //"  LEFT JOIN opagm20 AS OPGM20",
                //"    ON CHARINDEX(OPGM20.stfn_cname,lrec93_mstfn) > 0",
                //"   AND (SELECT prof_prof FROM ispfm00",
                //"         WHERE prof_dname = SUBSTRING(lrec93_mstfn, 1, CHARINDEX(STFN_CNAME, lrec93_mstfn) - 1)) = OPGM20.stfn_prof",
                //"  LEFT JOIN recm94",
                //"    ON rec94_form = rec93_form",
                //"   AND (rec94_fsts + rec94_stfn) = (lrec93_fsts + OPGM20.stfn_stfn)",
                //" WHERE lrec93_form = @lrec93_form"
                "SELECT stfn_cname",
                "     , rec94_stfn",
                "  INTO #REC94",
                "  FROM recm94",
                "  JOIN OPAGM20",
                "    ON stfn_stfn = rec94_stfn",
                " WHERE rec94_form = @lrec93_form",
                " GROUP BY stfn_cname,rec94_stfn;",

                "SELECT rec93_stfn",
                "     , R.rec94_stfn AS stfn_stfn",
                "     , lrec93_form",
                "     , lrec93_date",
                "     , lrec93_fsts",
                "     , lrec93_hidden",
                "     , lrec93_bgcolor",
                "     , lrec93_mstfn",
                "     , lrec93_mdate",
                "     , lrec93_desc",
                "  FROM logrecm93",
                "  JOIN recm93",
                "    ON lrec93_form = rec93_form",
                "  LEFT JOIN #REC94 R",
                "    ON CHARINDEX(R.stfn_cname,lrec93_mstfn) > 0",
                " WHERE lrec93_form = @lrec93_form;",
                "DROP TABLE #REC94;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@lrec93_form", para.Rec93Form);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(erpSigLog);
                }
            }

            return erpSigLog;
        }
        #endregion

        #region - 編輯新工作流程 -
        /// <summary>
        /// 編輯新工作流程
        /// </summary>
        public class NewWFFlowPara
        {

            public string SysID { get; set; }

            public string UserID { get; set; }
            public string FlowID { get; set; }
            public string FlowVer { get; set; }
            public string Subject { get; set; }
        }

        public class NewWFFlow
        {
            public string WFNo { get; set; }
            public string NodeNo { get; set; }
            public string SysID { get; set; }
            public string FlowID { get; set; }
            public string FlowVer { get; set; }
            public string NodeID { get; set; }
            public string NodeType { get; set; }
            public string FunSysID { get; set; }
            public string SubSysID { get; set; }
            public string FunControllerID { get; set; }
            public string FunActionName { get; set; }
            public string DTBegin { get; set; }
            public string ResultID { get; set; }
            public string Result { get; set; }
        }

        public NewWFFlow EditNewWFFlow(NewWFFlowPara para)
        {
            DataTable wfNewFlowInfo = new DataTable();
            NewWFFlow newWFFlow = new NewWFFlow();

            var commandNewFlow = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                //"EXECUTE dbo.SP_WF_NEW_FLOW @SYS_ID, @FLOW_ID, @FLOW_VER, @LOT, @SUBJECT, @NODE_NO, @USER_ID, @UPD_USER_ID;"
                "SET NOCOUNT ON;",

                "DECLARE @RETURN_DATA TABLE(",
                "WFNo CHAR(14),",
                "NodeNo CHAR(3),",
                "SysID VARCHAR(12),",
                "FlowID VARCHAR(50),",
                "FlowVer CHAR(3),",
                "NodeID VARCHAR(50),",
                "NodeType VARCHAR(20),",
                "FunSysID VARCHAR(12),",
                "SubSysID VARCHAR(12),",
                "FunControllerID VARCHAR(20),",
                "FunActionName VARCHAR(50),",
                "DTBegin CHAR(17),",
                "ResultID CHAR(11),",
                "Result VARCHAR(50),",
                "ErrorLine INT,",
                "ErrorNumber INT,",
                "ErrorMessage NVARCHAR(4000)",
                ");",

                "DECLARE @RESULT VARCHAR(50) = 'Success';",
                "DECLARE @ERROR_LINE INT;",
                "DECLARE @ERROR_NUMBER INT;",
                "DECLARE @ERROR_MESSAGE NVARCHAR(4000);",
                "DECLARE @TODAY_YEAR CHAR(4) = CAST(YEAR(GETDATE()) AS CHAR);",
                "DECLARE @TODAY_YMD CHAR(8) = dbo.FN_GET_SYSDATE(NULL);",
                "DECLARE @NOW_DATETIME CHAR(17) = @TODAY_YMD + dbo.FN_GET_SYSTIME(NULL);",
                "DECLARE @IS_START_SIG CHAR(1) = NULL;",
                "DECLARE @WF_NO CHAR(14);",
                "DECLARE @WF_NODE_ID VARCHAR(50);",
                "DECLARE @REMARK_NO CHAR(3);",

                "SELECT @WF_NO = @TODAY_YEAR + RIGHT('000000000' + CAST(ISNULL(CAST(SUBSTRING(MAX(WF_NO), 5, 10) AS BIGINT), 0) + 1 AS VARCHAR), 10)",
                "  FROM WF_FLOW",
                " WHERE WF_NO > @TODAY_YEAR + '0000000000'",
                "   AND WF_NO < @TODAY_YEAR + '9999999999';",

                //取得起始節點
                "SELECT DISTINCT @WF_NODE_ID = N.WF_NODE_ID",
                "  FROM SYS_SYSTEM_WF_FLOW F",
                "  JOIN SYS_SYSTEM_WF_NODE N",
                "    ON F.SYS_ID = N.SYS_ID",
                "   AND F.WF_FLOW_ID = N.WF_FLOW_ID",
                "   AND F.WF_FLOW_VER = N.WF_FLOW_VER",
                "   AND N.IS_FIRST = 'Y'",
                "  JOIN SYS_SYSTEM_ROLE_FLOW R",
                "    ON F.SYS_ID = R.SYS_ID",
                "   AND F.WF_FLOW_ID = R.WF_FLOW_ID",
                "   AND F.WF_FLOW_VER = R.WF_FLOW_VER",
                " WHERE F.SYS_ID = @SYS_ID",
                "   AND F.WF_FLOW_ID = @FLOW_ID",
                "   AND F.WF_FLOW_VER = @FLOW_VER",
                "   AND F.ENABLE_DATE <= @TODAY_YMD",
                "   AND ISNULL(F.DISABLE_DATE, '99999999') > @TODAY_YMD;",

                "IF @WF_NODE_ID IS NULL",
                "    BEGIN",
                //請確認工作流程，啟用日期、停用日期、是否有起始節點
                "        SET @RESULT = 'CheckWFLifeCycle';",
                "    END;",

                //是否簽核節點
                "    IF EXISTS(SELECT *",
                "                FROM SYS_SYSTEM_WF_SIG",
                "               WHERE SYS_ID = @SYS_ID",
                "                 AND WF_FLOW_ID = @FLOW_ID",
                "                 AND WF_FLOW_VER = @FLOW_VER",
                "                 AND WF_NODE_ID = @WF_NODE_ID) ",
                "	    SET @IS_START_SIG = 'N';",

                "IF @RESULT = 'Success' AND",
                "SUBSTRING(@WF_NO, 5, 10) <> '0000000000' AND @WF_NODE_ID IS NOT NULL",
                "    BEGIN",
                "        SELECT @REMARK_NO = MAX(REMARK_NO)",
                "          FROM WF_REMARK",
                "         WHERE WF_NO = @WF_NO;",
                "            BEGIN TRANSACTION",
                "                BEGIN TRY",
                //新增工作流程
                "        INSERT INTO WF_FLOW VALUES (",
                "                    @WF_NO, @SYS_ID, @FLOW_ID, @FLOW_VER",
                "                  , @SUBJECT, @LOT",
                "                  , @USER_ID, NULL, @NOW_DATETIME, NULL, 'P', @NODE_NO",
                "                  , @UPD_USER_ID, GETDATE()",
                "	     );",

                //新增作業節點
                "        INSERT INTO WF_NODE VALUES (",
                "                    @WF_NO, @NODE_NO, @SYS_ID, @FLOW_ID, @FLOW_VER, @WF_NODE_ID",
                "                  , @USER_ID, NULL, NULL, NULL, @NOW_DATETIME, NULL, 'P', NULL",
                "                  , @IS_START_SIG, NULL, NULL, NULL, NULL",
                "                  , @UPD_USER_ID, GETDATE()",
                "	     );",

                //新增節點侯選處理人名單
                "       INSERT INTO WF_NODE_NEW_USER VALUES(",
                "                    @WF_NO, @NODE_NO, @SYS_ID, @FLOW_ID, @FLOW_VER, @WF_NODE_ID, @USER_ID, @UPD_USER_ID, GETDATE()",
                "	    );",
                "                                                                                                                                                ",
                //新增備註
                "       SET @REMARK_NO = RIGHT('00' + CAST(ISNULL(CAST(@REMARK_NO AS INT), 0) + 1 AS VARCHAR), 3)",
                "       INSERT INTO dbo.WF_REMARK(",
                "              WF_NO, NODE_NO, REMARK_NO, SYS_ID, WF_FLOW_ID, WF_FLOW_VER, WF_NODE_ID, NODE_RESULT_ID, BACK_WF_NODE_ID",
                "            , SIG_STEP, WF_SIG_SEQ, SIG_DATE, SIG_RESULT_ID",
                "            , DOC_NO, WF_DOC_SEQ, DOC_DATE, DOC_IS_DELETE",
                "            , REMARK_USER_ID, REMARK_DATE, REMARK",
                "            , UPD_USER_ID, UPD_DT",
                "       ) VALUES(",
                "              @WF_NO, @NODE_NO, @REMARK_NO, @SYS_ID, @FLOW_ID, @FLOW_VER, @WF_NODE_ID, 'P', NULL",
                "            , NULL, NULL, NULL, NULL",
                "            , NULL, NULL, NULL, NULL",
                "            , @USER_ID, @NOW_DATETIME, NULL",
                "            , @UPD_USER_ID, GETDATE()",
                "       )",
                "           SET @RESULT = 'Success';",
                "       COMMIT;",
                "       END TRY",
                "       BEGIN CATCH",
                "           SET @RESULT = 'Failure';",
                "       SET @ERROR_LINE = ERROR_LINE();",
                "       SET @ERROR_NUMBER = ERROR_NUMBER();",
                "       SET @ERROR_MESSAGE = ERROR_MESSAGE();",
                "       ROLLBACK TRANSACTION;",
                "       END CATCH;",
                "       END;",
                "       IF @RESULT = 'Success'",
                "    BEGIN",
                "        INSERT INTO @RETURN_DATA",
                "        SELECT @WF_NO AS WFNo",
                "             , @NODE_NO AS NodeNo",
                "             , N.SYS_ID AS SysID",
                "	         , N.WF_FLOW_ID AS FlowID",
                "	         , N.WF_FLOW_VER AS FlowVer",
                "	         , N.WF_NODE_ID AS NodeID",
                "	         , N.NODE_TYPE AS NodeType",
                "	         , N.FUN_SYS_ID AS FunSysID",
                "	         , F.SUB_SYS_ID AS SubSysID",
                "	         , N.FUN_CONTROLLER_ID AS FunControllerID",
                "	         , N.FUN_ACTION_NAME AS FunActionName",
                "	         , @NOW_DATETIME AS DTBegin",
                "	         , 'P' AS ResultID",
                "             , @RESULT AS Result",
                "             , NULL",
                "             , NULL",
                "             , NULL",
                "          FROM SYS_SYSTEM_WF_NODE N",
                "          JOIN SYS_SYSTEM_FUN F",
                "            ON N.FUN_SYS_ID = F.SYS_ID",
                "           AND N.FUN_CONTROLLER_ID = F.FUN_CONTROLLER_ID",
                "           AND N.FUN_ACTION_NAME = F.FUN_ACTION_NAME",
                "         WHERE N.SYS_ID = @SYS_ID",
                "           AND N.WF_FLOW_ID = @FLOW_ID",
                "           AND N.WF_FLOW_VER = @FLOW_VER",
                "           AND N.WF_NODE_ID = @WF_NODE_ID;",
                "            END",
                "            ELSE",
                "    BEGIN",
                "        INSERT INTO @RETURN_DATA (Result, ErrorLine, ErrorMessage, ErrorNumber)",
                "        SELECT @RESULT, @ERROR_LINE, @ERROR_MESSAGE, @ERROR_NUMBER;",
                "            END;",
                "            SELECT* FROM @RETURN_DATA;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandNewFlow.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@SYS_ID", para.SysID);
                    cmd.Parameters.AddWithValue("@FLOW_ID", para.FlowID);
                    cmd.Parameters.AddWithValue("@FLOW_VER", para.FlowVer);
                    cmd.Parameters.AddWithValue("@LOT", "NULL");
                    cmd.Parameters.AddWithValue("@SUBJECT", para.Subject);
                    cmd.Parameters.AddWithValue("@NODE_NO", "001");
                    cmd.Parameters.AddWithValue("@USER_ID", para.UserID);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UserID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(wfNewFlowInfo);
                }
            }

            if (wfNewFlowInfo.Rows.Count > 0)
            {
                newWFFlow = (from DataRow dr in wfNewFlowInfo.Rows
                             select new NewWFFlow
                             {
                                 WFNo = dr.Field<string>("WFNo"),
                                 NodeNo = dr.Field<string>("NodeNo"),
                                 SysID = dr.Field<string>("SysID"),
                                 FlowID = dr.Field<string>("FlowID"),
                                 FlowVer = dr.Field<string>("FlowVer"),
                                 NodeID = dr.Field<string>("NodeID"),
                                 NodeType = dr.Field<string>("NodeType"),
                                 FunSysID = dr.Field<string>("FunSysID"),
                                 SubSysID = dr.Field<string>("SubSysID"),
                                 FunControllerID = dr.Field<string>("FunControllerID"),
                                 FunActionName = dr.Field<string>("FunActionName"),
                                 DTBegin = dr.Field<string>("DTBegin"),
                                 ResultID = dr.Field<string>("ResultID"),
                                 Result = dr.Field<string>("Result")
                             }).SingleOrDefault();
            }
            return newWFFlow;
        }
        #endregion

        #region - 取得結點簽核名單 -
        /// <summary>
        /// 取得結點簽核名單
        /// </summary>
        public class SetSigUserListPara
        {
            public string Rec94Form { get; set; }
        }

        public class SetSigUserList
        {
            public int rec94_form { get; set; }
            public byte rec94_no { get; set; }
            public bool rec94_bsts { get; set; }
            public byte rec94_97no { get; set; }
            public string rec94_fsts { get; set; }
            public string rec94_stfn { get; set; }
        }

        public List<SetSigUserList> GetSetSigUserList(SetSigUserListPara para)
        {
            DataTable tableRec94 = new DataTable();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SELECT rec94_form",
                "     , rec94_no",
                "     , rec94_bsts",
                "     , rec94_97no",
                "     , rec94_fsts",
                "     , rec94_stfn",
                "  FROM recm94",
                " WHERE rec94_form = @rec94_form",
                " ORDER BY rec94_fsts,rec94_no"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@rec94_form", para.Rec94Form);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(tableRec94);
                }
            }

            return tableRec94.ToList<SetSigUserList>().ToList();
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

        public SetWFSignatureResult SetWFSignature(SetWFSignaturePara para)
        {
            DataTable setWfSigInfo = new DataTable();
            StringBuilder command = new StringBuilder();

            foreach (SetSigValue nodeNewUserPara in para.WFSigList)
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
                    "      , '" + para.UpdUserID + "'",
                    "      );"
                }));
            }

            var commandSetSignature = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "DECLARE @WF_SIG_LIST WF_SIG_TYPE",
                command.ToString(),
                "EXECUTE dbo.SP_WF_SET_SIGNATURE @WF_NO, @IS_START_SIG, @UPD_USER_ID, @WF_SIG_LIST;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandSetSignature.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@IS_START_SIG", para.IsStartSig);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(setWfSigInfo);
                }
            }

            return setWfSigInfo.ToList<SetWFSignatureResult>().SingleOrDefault();
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

        public WFSignature EditWFSignature(WFSignaturePara para)
        {
            DataTable wfSigInfo = new DataTable();

            var commandSignature = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "EXECUTE dbo.SP_WF_SIGNATURE @WF_NO, @NODE_NO, @USER_ID, @SIG_RESULT_ID, @SIG_COMMENT, @UPD_USER_ID;"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandSignature.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@NODE_NO", para.NodeNO);
                    cmd.Parameters.AddWithValue("@USER_ID", para.UserID);
                    cmd.Parameters.AddWithValue("@SIG_RESULT_ID", para.SigResultID);
                    cmd.Parameters.AddWithValue("@SIG_COMMENT", "NULL");
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", "NULL");
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(wfSigInfo);
                }
            }

            return wfSigInfo.ToList<WFSignature>().SingleOrDefault();
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

        #region - 增加註記 -
        public class AddWFRemarkPara
        {
            public string WFNo { get; set; }
            public string UserID { get; set; }
            public string UpdUserID { get; set; }
            public string Remark { get; set; }
        }

        public void AddWFRemark(AddWFRemarkPara para)
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

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandAddRemark.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@USER_ID", para.UserID);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    cmd.Parameters.AddWithValue("@REMARK", para.Remark);
                    exeNum = cmd.ExecuteNonQuery();
                }
            }
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

        public ToEndNode EditToEndNode(ToEndNodePara para)
        {
            DataTable editToEndNodeInfo = new DataTable();

            var commandSignature = new StringBuilder(string.Join(Environment.NewLine, new object[]
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

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandSignature.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@NODE_NO", para.NodeNO);
                    cmd.Parameters.AddWithValue("@USER_ID", para.UserID);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(editToEndNodeInfo);
                }
            }

            return editToEndNodeInfo.ToList<ToEndNode>().SingleOrDefault();
        }
        #endregion

        #region - 結案用-增加註記 -
        public class AddRemarkPara
        {
            public string WFNo { get; set; }
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

        public void AddRemark(AddRemarkPara para)
        {
            var commandAddRemark = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "DECLARE @REMARK_NO CHAR(3);",
                "DECLARE @NEW_USER_ID VARCHAR(20) = NULL;",

                "DECLARE @NOW_DATETIME CHAR(17) = dbo.FN_GET_SYSDATE(NULL) + dbo.FN_GET_SYSTIME(NULL);",
                " SELECT @REMARK_NO = MAX(REMARK_NO)",
                "   FROM WF_REMARK",
                "  WHERE WF_NO = @WF_NO;",
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
                "      , @REMARK_USER_ID",
                "      , @NOW_DATETIME",
                "      , @REMARK",
                "      , @UPD_USER_ID",
                "      , GETDATE()",
                "      )"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandAddRemark.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@SYS_ID", para.SysID);
                    cmd.Parameters.AddWithValue("@WF_FLOW_ID", para.FlowID);
                    cmd.Parameters.AddWithValue("@WF_FLOW_VER", para.FlowVer);
                    cmd.Parameters.AddWithValue("@WF_NODE_ID", para.WFNodeID);
                    cmd.Parameters.AddWithValue("@NODE_NO", para.NodeNO);
                    cmd.Parameters.AddWithValue("@REMARK_USER_ID", para.RemarkUserID);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    cmd.Parameters.AddWithValue("@REMARK", para.Remark);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region - 結案單_結束節點 -
        public class WFENDFlowPara
        {
            public string WFNo { get; set; }
            public string UserID { get; set; }
            public string UpdUserID { get; set; }
        }

        public void EditWFENDFlow(WFENDFlowPara para)
        {
            var commandAddRemark = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "DECLARE @NOW_DATETIME CHAR(17) = dbo.FN_GET_SYSDATE(NULL) + dbo.FN_GET_SYSTIME(NULL); ",
                "UPDATE WF_FLOW ",
                "   SET END_USER_ID = @USER_ID",
                "     , DT_END = @NOW_DATETIME",
                "     , RESULT_ID = 'F'",
                "     , NODE_NO = NULL ",
                "     , UPD_USER_ID = @UPD_USER_ID",
                "     , UPD_DT = GETDATE() ",
                " WHERE WF_NO = @WF_NO; "
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandAddRemark.ToString(), connection))
                {
                    connection.Open();
                    cmd.Parameters.AddWithValue("@WF_NO", para.WFNo);
                    cmd.Parameters.AddWithValue("@USER_ID", para.UserID);
                    cmd.Parameters.AddWithValue("@UPD_USER_ID", para.UpdUserID);
                    cmd.ExecuteNonQuery();
                }
            }
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

        #region - 取得員工編號(針對原本在簽核名單內但被刪除者) -
        public class SigRemoveUserIDPara
        {
            public string UserNM { get; set; }
        }

        public class SigRemoveUserID
        {
            public string STFN { get; set; }
        }

        public List<SigRemoveUserID> GetSigRemoveUserID(SigRemoveUserIDPara para)
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
            return tableRow.ToList<SigRemoveUserID>().ToList();
        }
        #endregion

        #region MyRegion
        public class SerpDisableUser
        {
            public string UserID { get; set; }
        }

        public List<SerpDisableUser> GetSerpDisableUserList()
        {
            DataTable tableRow = new DataTable();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SELECT RAW_CM_USER.USER_ID AS UserID",
                "  FROM RAW_CM_USER",
                "  LEFT OUTER JOIN SYS_USER_MAIN",
                "    ON RAW_CM_USER.USER_ID = SYS_USER_MAIN.USER_ID",
                " WHERE IS_DISABLE IS NULL",
                "    OR IS_DISABLE = 'Y'"
            }));

            using (SqlConnection connection = new SqlConnection(_conn))
            {
                using (SqlCommand cmd = new SqlCommand(commandText.ToString(), connection))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(tableRow);
                }
            }
            return tableRow.ToList<SerpDisableUser>().ToList();
        }
        #endregion
    }
}
