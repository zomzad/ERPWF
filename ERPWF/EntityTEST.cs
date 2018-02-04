using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace ERPWF
{
    public class EntityTEST
    {
        private readonly string _conn;

        public EntityTEST(string connStr)
        {
            _conn = connStr;
        }

        public DataSet GetSerpSignFormInfoList()
        {
            DataSet ds = new DataSet();

            var commandText = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                //取得聯絡單簽核單
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
                " ORDER BY SignFormNO DESC;",

                //取得取得OPAGM20 & 索引
                "SELECT stfn_stfn",
                "     , stfn_cname",
                "  INTO #IdxOpagm20",
                "  FROM opagm20",

                //包含姓名的簽核清單 並建立索引
                "SELECT stfn_cname AS stfnCname",
                "     , rec94_fsts AS rec94Fsts",
                "     , rec94_stfn AS rec94Stfn",
                "     , rec94_form AS rec94Form",
                "  INTO #REC94",
                "  FROM recm94",
                "  JOIN #IdxOpagm20",
                "    ON stfn_stfn = rec94_stfn",
                " GROUP BY stfn_cname,rec94_stfn,rec94_form,rec94_fsts;",
                "SELECT * FROM #REC94;",

                //簽核單限定在聯絡單
                "SELECT *",
                "  INTO #REC93",
                "  FROM recm93",
                " WHERE rec93_formno = '2'",
                "SELECT * FROM #REC93;",

                //抓出Log紀錄中，單號是聯絡單的部分
                "SELECT * ",
                "  INTO #LOGRECM93",
                "  FROM logrecm93",
                "  WHERE logrecm93.lrec93_form IN (SELECT rec93_form FROM #REC93)",
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
    }
}