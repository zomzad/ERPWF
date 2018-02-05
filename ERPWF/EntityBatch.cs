using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERPWF
{
    class EntityBatch
    {
        private readonly string _conn;

        public EntityBatch(string connStr)
        {
            _conn = connStr;
        }

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
            public string SignFormNo { get; set; }
        }

        public List<NewWFFlow> WFNoList = new List<NewWFFlow>();

        public List<NewWFFlow> EditNewWFFlow(List<EntityWorkflow.NewWFFlowPara> newWFFlowParaList)
        {
            DataTable wfNewFlowInfo = new DataTable();

            var commandNewFlow = new StringBuilder(string.Join(Environment.NewLine, new object[]
            {
                "SET NOCOUNT ON;",

                "DECLARE @RETURN_DATA TABLE(",
                "WFNo CHAR(14),",
                "SignFormNo CHAR(8),",
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
                "             , @SIGN_FORM_NO AS SignFormNo",
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
                "    END;",
                "    SELECT* FROM @RETURN_DATA;"
            }));

            SqlConnection conn = new SqlConnection(_conn);
            SqlCommand comm = new SqlCommand(commandNewFlow.ToString(), conn);
            conn.Open();

            foreach (var wf in newWFFlowParaList.Select((value, index) => new { Value = value, Index = index }))
            {
                comm.Parameters.Clear();
                comm.Parameters.AddWithValue("@SYS_ID", wf.Value.SysID);
                comm.Parameters.AddWithValue("@FLOW_ID", wf.Value.FlowID);
                comm.Parameters.AddWithValue("@FLOW_VER", wf.Value.FlowVer);
                comm.Parameters.AddWithValue("@LOT", "NULL");
                comm.Parameters.AddWithValue("@SUBJECT", wf.Value.Subject);
                comm.Parameters.AddWithValue("@NODE_NO", "001");
                comm.Parameters.AddWithValue("@USER_ID", wf.Value.UserID);
                comm.Parameters.AddWithValue("@UPD_USER_ID", wf.Value.UserID);
                comm.Parameters.AddWithValue("@SIGN_FORM_NO", wf.Value.SignFormNo);

                SqlDataAdapter adapter = new SqlDataAdapter(comm);
                adapter.Fill(wfNewFlowInfo);

                WFNoList.Add(new NewWFFlow
                {
                    WFNo = wfNewFlowInfo.Rows[wf.Index].Field<string>("WFNo"),
                    SignFormNo = wfNewFlowInfo.Rows[wf.Index].Field<string>("SignFormNo")
                });
            }
            conn.Dispose();
            comm.Dispose();

            return WFNoList;
        }
        #endregion
    }
}
