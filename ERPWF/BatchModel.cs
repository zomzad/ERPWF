using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ERPWF
{
    internal class BatchModel
    {
        #region - Definitions -
        public enum EnumSigResultID
        {
            P,
            R,
            A
        }

        public enum EnumLogDescType
        {
            [Description("申請")]
            APPLY,

            [Description("立單")]
            NEWFORM,

            [Description("退回!")]
            BACK,

            [Description("核准!")]
            PASS,

            [Description("暫不核!")]
            PAUSE,

            [Description("修改簽核名單")]
            MODIFYSigList,

            [Description("完成")]
            COMPLETE
        }

        protected enum EnumNewWFInfo
        {
            [Description("PUBAP")]
            SYS_ID,

            [Description("SignForm")]
            FLOW_ID,

            [Description("001")]
            FLOW_VER,

            [Description("簽核單")]
            SUBJECT,

            [Description("APIService.ERP.WorkFlowService")]
            UPD_USER_ID
        }

        public class RemarkData
        {
            public string WFNo { get; set; }
            public string NodeNO { get; set; }
            public string RemarkNO { get; set; }
            public string SysID { get; set; }
            public string WFFlowID { get; set; }
            public string WFFlowVer { get; set; }
            public string WFNodeID { get; set; }
            public string NodeResultID { get; set; }
            public string BackWFNodeID { get; set; }
            public int? SigStep { get; set; }
            public string WFSigSeq { get; set; }
            public string SigDate { get; set; }
            public string SigResultID { get; set; }
            public string DocNO { get; set; }
            public string WFDocSEQ { get; set; }
            public string DocDate { get; set; }
            public string DocIsDelete { get; set; }
            public string RemarkUserID { get; set; }
            public string RemarkDate { get; set; }
            public string Remark { get; set; }
            public string UpdUserID { get; set; }
            public DateTime UpdDT { get; set; }
        }

        protected class Recm94
        {
            public int Rec94Form { get; set; }
            public byte Rec94No { get; set; }
            public string Rec94Fsts { get; set; }
            public string Rec94Stfn { get; set; }
            public string StfnCname { get; set; }
        }

        protected class LogRecm93
        {
            public string Rec93Mstfn { get; set; }
            public string Rec93Stfn { get; set; }
            public int Lrec93Form { get; set; }
            public DateTime Lrec93Date { get; set; }
            public string Lrec93Fsts { get; set; }
            public bool Lrec93Hidden { get; set; }
            public string Lrec93Bgcolor { get; set; }
            public string Lrec93Mstfn { get; set; }
            public DateTime Lrec93Mdate { get; set; }
            public string Lrec93Desc { get; set; }
        }

        protected class WFFlow
        {
            public string WFNo { get; set; }
            public string NewUserID { get; set; }
            public string UpdUserID { get; set; }
            public string SysID { get; set; }
            public string FlowID { get; set; }
            public string FlowVer { get; set; }
            public string Subject { get; set; }
        }

        public class ErpWFLogNode
        {
            /// <summary>
            /// 描述
            /// </summary>
            public string Desc { get; set; }

            /// <summary>
            /// 簽核人員
            /// </summary>
            public string SigUserID { get; set; }

            /// <summary>
            /// 簽核身份別
            /// </summary>
            public string SigCategory { get; set; }
        }
        #endregion

        #region - Property -
        int remarkNoSource { get; set; }
        public ErpWFLogNode ErpWFLogNodeInfo { get; set; }
        protected WFFlow WFFlowData { get; set; }
        protected List<SignForm> AddSignFormParaList { get; private set; }
        protected List<SignForm> BatchSignFormList { get; set; }
        protected List<SignForm> SignFormList { get; set; }
        protected List<Recm94> Recm94List { get; set; }
        protected List<Recm94> SpecificFormNoRecm94List { get; set; }
        protected List<LogRecm93> LogRecm93List { get; set; }
        public List<LogInfo> SpecificFormNoERPLogInfoList { get; private set; }
        protected List<EntityBatch.NewWFFlowPara> NewWFFlowParaList { get; private set; }
        protected List<EntityBatch.AddRemarkPara> AddRemarkParaList { get; private set; }
        protected List<EntityBatch.SetWFSignaturePara> SetWFSignatureParaList { get; private set; }
        protected List<EntityBatch.AddWFRemarkPara> AddWFRemarkParaList { get; private set; }
        protected List<EntityBatch.WFSignaturePara> WFSignatureExecaraList { get; set; }
        protected List<EntityBatch.ToEndNodePara> ToEndNodeParaList { get; set; }
        protected List<EntityBatch.NextToNodePara> NextToNodeParaList { get; set; }
        protected Dictionary<string, List<Recm94>> Recm94Dic { get; set; }
        protected Dictionary<string, List<LogRecm93>> LogRecm93Dic { get; set; }
        #endregion

        #region - Private -
        private bool _forceEnd;
        private bool _firstTimeSig;
        private readonly EntityBatch _connUSerp;
        private readonly EntityBatch _connERP;
        #endregion

        #region - Constructor -
        public BatchModel()
        {
            _connUSerp = new EntityBatch(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString);
            _connERP = new EntityBatch(ConfigurationManager.ConnectionStrings["UERPConnection"].ConnectionString);

            WFFlowData = new WFFlow
            {
                SysID = GetEnumDescription(EnumNewWFInfo.SYS_ID),
                FlowID = GetEnumDescription(EnumNewWFInfo.FLOW_ID),
                FlowVer = GetEnumDescription(EnumNewWFInfo.FLOW_VER),
                Subject = GetEnumDescription(EnumNewWFInfo.SUBJECT),
                UpdUserID = GetEnumDescription(EnumNewWFInfo.UPD_USER_ID)
            };
        }
        #endregion

        #region - 取得聯絡單簽核單所有必需資料 -
        /// <summary>
        /// 取得聯絡單簽核單所有必需資料
        /// </summary>
        public void GetAllNecessarySignData()
        {
            Console.WriteLine("取得ERP所有資料中");
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            DataSet signInfoDs = _connERP.GetSerpSignFormInfoList();

            SignFormList = signInfoDs.Tables[0].ToList<SignForm>().ToList();
            Recm94List = signInfoDs.Tables[1].ToList<Recm94>().ToList();

            Recm94Dic = Recm94List
                .GroupBy(x => x.Rec94Form.ToString())
                .ToDictionary(x => x.Key, x => x.ToList());

            var recm93List = signInfoDs.Tables[2].ToList<recm93>();
            var logRecm93List = signInfoDs.Tables[3].ToList<LogRecm93>();

            #region - 所有簽核單LOG記錄檔清單 -
            LogRecm93List =
                (from log in logRecm93List
                 join signForm in recm93List
                     on log.Lrec93Form equals signForm.rec93_form into s
                 from sign in s.DefaultIfEmpty()
                 select new LogRecm93
                 {
                     Rec93Stfn = sign.rec93_stfn,
                     Lrec93Form = log.Lrec93Form,
                     Lrec93Date = log.Lrec93Date,
                     Lrec93Fsts = log.Lrec93Fsts,
                     Lrec93Hidden = log.Lrec93Hidden,
                     Lrec93Bgcolor = log.Lrec93Fsts,
                     Lrec93Mstfn = log.Lrec93Mstfn,
                     Lrec93Mdate = log.Lrec93Mdate,
                     Lrec93Desc = log.Lrec93Desc
                 }).ToList();

            LogRecm93Dic = LogRecm93List.GroupBy(x => x.Lrec93Form.ToString())
                .ToDictionary(x => x.Key, x => x.ToList());
            #endregion
            sw.Stop();
            Console.WriteLine("耗時:" + (sw.ElapsedMilliseconds) + "毫秒\n=====================================");

            EditSerpWFData();
        }
        #endregion

        #region - 編輯SERP工作流程資料 -
        /// <summary>
        /// 編輯SERP工作流程資料
        /// </summary>
        private void EditSerpWFData()
        {
            var batchCount = Convert.ToInt32(Math.Ceiling(SignFormList.Count / (double)1000));

            foreach (var index in Enumerable.Range(0, batchCount))
            {
                BatchSignFormList = SignFormList.Skip(index * 1000).Take(1000).ToList();

                #region - 新增WF -
                NewWFFlowParaList = new List<EntityBatch.NewWFFlowPara>();
                NewWFFlowParaList.AddRange(BatchSignFormList.Select(s => new EntityBatch.NewWFFlowPara
                {
                    SysID = WFFlowData.SysID,
                    FlowID = WFFlowData.FlowID,
                    FlowVer = WFFlowData.FlowVer,
                    Subject = WFFlowData.Subject,
                    UserID = ConvertUserIDLength(s.SignFormNewUserID),
                    SignFormNo = s.SignFormNO
                }));
                #endregion

                Console.WriteLine("新增WF單號");
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                var wfNoList = _connUSerp.EditNewWFFlow(NewWFFlowParaList);
                sw.Stop();
                Console.WriteLine("耗時:" + (sw.ElapsedMilliseconds) + "毫秒\n=====================================");

                if (BatchAddSerpSignForm(wfNoList))
                {
                    AddWFNodeInfo();
                }
            }
        }
        #endregion

        #region - 批次新增SERP簽核單 -
        protected bool BatchAddSerpSignForm(List<EntityBatch.NewWFFlow> wfNoList)
        {
            try
            {
                AddSignFormParaList = new List<SignForm>();
                AddSignFormParaList.AddRange(
                    from wf in wfNoList
                    join sign in BatchSignFormList on wf.SignFormNo equals sign.SignFormNO
                    select new SignForm
                    {
                        SignFormNO = string.IsNullOrWhiteSpace(sign.SignFormNO) ? DBNull.Value.ToString() : sign.SignFormNO,
                        SignFormWFNO = wf.WFNo,
                        Rec93Form = sign.Rec93Form,
                        FSTS = string.IsNullOrWhiteSpace(sign.FSTS) ? DBNull.Value.ToString() : sign.FSTS,
                        SignFormType = string.IsNullOrWhiteSpace(sign.SignFormType) ? DBNull.Value.ToString() : sign.SignFormType,
                        IsDisable = sign.IsDisable,
                        SignFormSubject = string.IsNullOrWhiteSpace(sign.SignFormSubject) ? DBNull.Value.ToString() : sign.SignFormSubject,
                        SignFormReason = string.IsNullOrWhiteSpace(sign.SignFormReason) ? DBNull.Value.ToString() : sign.SignFormReason,
                        SignFormProcess = string.IsNullOrWhiteSpace(sign.SignFormProcess) ? DBNull.Value.ToString() : sign.SignFormProcess,
                        SignFormOrderYear = string.IsNullOrWhiteSpace(sign.SignFormOrderYear) ? DBNull.Value.ToString() : sign.SignFormOrderYear,
                        SignFormOrderNO = string.IsNullOrWhiteSpace(sign.SignFormOrderNO) ? DBNull.Value.ToString() : sign.SignFormOrderNO,
                        SignFormItem = sign.SignFormItem.HasValue ? sign.SignFormItem : new byte(),
                        SignFormERPWork = sign.SignFormERPWork.HasValue ? sign.SignFormERPWork : new byte(),
                        SignFormBU = string.IsNullOrWhiteSpace(sign.SignFormBU) ? DBNull.Value.ToString() : sign.SignFormBU,
                        SignFormPeerComp = string.IsNullOrWhiteSpace(sign.SignFormPeerComp) ? DBNull.Value.ToString() : sign.SignFormPeerComp,
                        SignFormUserID = string.IsNullOrWhiteSpace(ConvertUserIDLength(sign.SignFormUserID)) ? DBNull.Value.ToString() : ConvertUserIDLength(sign.SignFormUserID),
                        SignFormNewUserID = string.IsNullOrWhiteSpace(ConvertUserIDLength(sign.SignFormNewUserID)) ? DBNull.Value.ToString() : ConvertUserIDLength(sign.SignFormNewUserID),
                        SignFormNewDT = sign.SignFormNewDT,
                        UpdUserID = string.IsNullOrWhiteSpace(ConvertUserIDLength(sign.UpdUserID)) ? DBNull.Value.ToString() : ConvertUserIDLength(sign.UpdUserID),
                        UPDDT = sign.UPDDT
                    });

                #region - TVP批次新增聯絡單 -

                #region - 建立SERP聯絡單DataTable資料 -
                var addSignFormDTList = AddSignFormParaList.Select(sign => new
                {
                    sign.SignFormNO,
                    sign.SignFormWFNO,
                    sign.SignFormType,
                    sign.IsDisable,
                    sign.SignFormSubject,
                    sign.SignFormReason,
                    sign.SignFormProcess,
                    sign.SignFormOrderYear,
                    sign.SignFormOrderNO,
                    sign.SignFormItem,
                    sign.SignFormERPWork,
                    sign.SignFormBU,
                    sign.SignFormPeerComp,
                    sign.SignFormUserID,
                    sign.SignFormNewUserID,
                    sign.SignFormNewDT,
                    sign.UpdUserID,
                    sign.UPDDT
                }).ToList();
                #endregion

                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(
                    @"IF type_id('[dbo].[SIGFORM_TYPE]') IS NOT NULL
                    DROP TYPE [dbo].[SIGFORM_TYPE];

                    CREATE TYPE SIGFORM_TYPE AS TABLE
                    (
                    	SING_FORM_NO CHAR(14) NOT NULL,
                    	SING_FORM_WFNO CHAR(14),
                    	SIGN_FORM_TYPE CHAR(1),
                    	IS_DISABLE CHAR(1),
                    	SIGN_FORM_SUBJECT NVARCHAR(100) NOT NULL,
                    	SIGN_FORM_REASON NVARCHAR(500) NOT NULL,
                    	SIGN_FORM_PROCESS NVARCHAR(2000) NOT NULL,
                    	SIGN_FORM_ORDER_YEAR VARCHAR(4),
                    	SIGN_FORM_ORDER_NO VARCHAR(10),
                    	SIGN_FORM_ITEM CHAR(3),
                    	SIGN_FORM_ERPWORK CHAR(3),
                    	SIGN_FORM_BU CHAR(2),
                    	SIGN_PEER_COMP VARCHAR(6),
                    	SING_FORM_USER_ID VARCHAR(6),
                    	SING_FORM_NEW_USER_ID VARCHAR(6) NOT NULL,
                    	SING_FORM_NEW_DT DATETIME NOT NULL,
                    	UPD_USER_ID VARCHAR(50) NOT NULL,
                    	UPD_DT DATETIME NOT NULL
                    );", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand("INSERT INTO ZD223_SIGN_FORM SELECT * FROM @TVPSignFormkData; DROP TYPE [dbo].[SIGFORM_TYPE]", connection))
                    {
                        SqlParameter tvp = cmd.Parameters.Add("@TVPSignFormkData", SqlDbType.Structured);
                        tvp.Value = ListToDatatable(addSignFormDTList);
                        tvp.TypeName = "SIGFORM_TYPE";
                        Console.WriteLine("新增聯絡單");
                        Stopwatch sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        cmd.ExecuteNonQuery();
                        sw.Stop();
                        Console.WriteLine("耗時:" + (sw.ElapsedMilliseconds) + "毫秒\n=====================================");
                    }
                }
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                WriteErrorFormLog($"{ex.Message}{Environment.NewLine}");

            }
            return false;
        }
        #endregion

        #region - 新增工作流程節點資訊 -
        /// <summary>
        /// 新增工作流程節點資訊
        /// </summary>
        public void AddWFNodeInfo()
        {
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            Console.WriteLine("依據LOG紀錄建立備註參數(1000張單)");
            AddRemarkParaList = new List<EntityBatch.AddRemarkPara>();
            SetWFSignatureParaList = new List<EntityBatch.SetWFSignaturePara>();
            AddWFRemarkParaList = new List<EntityBatch.AddWFRemarkPara>();

            foreach (var signForm in AddSignFormParaList)
            {
                switch (signForm.FSTS)
                {
                    case "F":
                        ERPEndContactDataTransfer(signForm);
                        break;
                    default:
                        //ERPContactDataTransfer(signForm);
                        break;
                }
            }
            sw.Stop();
            Console.WriteLine("耗時:" + (sw.ElapsedMilliseconds) + "毫秒\n=====================================");

            AddRemark();

            //SetWFSignature();
            //AddWFRemark();
            //EditWFSignature();
            //EditToEndNode();
        }
        #endregion

        #region - 新增註記 -
        /// <summary>
        /// 新增註記
        /// </summary>
        private void AddRemark()
        {
            var addRemarkList = AddRemarkParaList.Select(n =>
                new RemarkData
                {
                    WFNo = n.WFNo,
                    NodeNO = n.NodeNO,
                    RemarkNO = n.RemarkNO,
                    SysID = n.SysID,
                    WFFlowID = n.FlowID,
                    WFFlowVer = n.FlowVer,
                    WFNodeID = n.WFNodeID,
                    NodeResultID = null,
                    BackWFNodeID = null,
                    SigStep = null,
                    WFSigSeq = null,
                    SigDate = null,
                    SigResultID = null,
                    DocNO = null,
                    WFDocSEQ = null,
                    DocDate = null,
                    DocIsDelete = null,
                    RemarkUserID = n.RemarkUserID,
                    RemarkDate = DateTime.Now.ToString("yyyyMMddhhmmssfff"),
                    Remark = string.IsNullOrWhiteSpace(n.Remark) ? null : n.Remark,
                    UpdUserID = n.UpdUserID,
                    UpdDT = DateTime.Now
                });

            using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand(
                    @"IF type_id('[dbo].[REMARK_TYPE]') IS NOT NULL
                    DROP TYPE [dbo].[REMARK_TYPE];

                    CREATE TYPE REMARK_TYPE AS TABLE
                    (
	                    WF_NO CHAR(14),
	                    NODE_NO CHAR(3),
	                    REMARK_NO CHAR(3),
	                    SYS_ID VARCHAR(12),
	                    WF_FLOW_ID VARCHAR(50),
	                    WF_FLOW_VER CHAR(3),
	                    WF_NODE_ID VARCHAR(50),
	                    NODE_RESULT_ID VARCHAR(20),
	                    BACK_WF_NODE_ID VARCHAR(50),
	                    SIG_STEP INT,
	                    WF_SIG_SEQ CHAR(3),
	                    SIG_DATE CHAR(17),
	                    SIG_RESULT_ID VARCHAR(20),
	                    DOC_NO CHAR(3),
	                    WF_DOC_SEQ CHAR(3),
	                    DOC_DATE CHAR(17),
	                    DOC_IS_DELETE CHAR(1),
	                    REMARK_USER_ID VARCHAR(20),
	                    REMARK_DATE CHAR(17),
	                    REMARK NVARCHAR(4000),
	                    UPD_USER_ID VARCHAR(50),
	                    UPD_DT DATETIME
                    );", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand("INSERT INTO WF_REMARK SELECT * FROM @TVPRematkData; DROP TYPE [dbo].[REMARK_TYPE]", connection))
                {
                    SqlParameter tvp = cmd.Parameters.Add("@TVPRematkData", SqlDbType.Structured);
                    tvp.Value = ListToDatatable(addRemarkList);
                    tvp.TypeName = "REMARK_TYPE";

                    Console.WriteLine("新增備註");
                    Stopwatch sw = new Stopwatch();
                    sw.Reset();
                    sw.Start();
                    cmd.ExecuteNonQuery();
                    sw.Stop();
                    Console.WriteLine("耗時:" + (sw.ElapsedMilliseconds) + "毫秒\n=====================================");
                }
            }
        }
        #endregion

        #region - ERP結案聯絡單資料轉移 -
        protected void ERPEndContactDataTransfer(SignForm signForm)
        {
            remarkNoSource = 2;
            WFFlowData.WFNo = signForm.SignFormWFNO;
            GetSpecificFormNoERPLogInfoList(signForm.Rec93Form.ToString());

            foreach (var log in SpecificFormNoERPLogInfoList.Select((value, index) => new { Value = value, Index = index }))
            {
                SetErpWFLogNodeInfo(log.Value);

                var user = string.IsNullOrWhiteSpace(log.Value.SignedUser)
                    ? ConvertUserIDLength(log.Value.Applicant)
                    : ConvertUserIDLength(log.Value.SignedUser);

                switch (log.Value.lrec93_fsts)
                {
                    case null:
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                        if (log.Value.lrec93_fsts == null &&
                            (log.Value.lrec93_desc == GetEnumDescription(EnumLogDescType.APPLY) || log.Value.lrec93_desc == GetEnumDescription(EnumLogDescType.NEWFORM)))
                        {
                            CheckFileAndUpload(signForm.SignFormNewUserID, signForm.Rec93Form.ToString());
                        }
                        else
                        {
                            AddRemarkPara("001", user, "ApplySignForm");
                            remarkNoSource += 1;
                        }
                        break;

                    case "5":
                        AddRemarkPara("002", user, "ProcessSignForm");
                        remarkNoSource += 1;
                        break;

                    case "6":
                    case "7":
                    case "A":
                    case "B":
                    case "F":
                        AddRemarkPara("003", user, "ApplySignForm");
                        remarkNoSource += 1;
                        break;
                }
            }
        }
        #endregion

        #region - ERP聯絡單資料轉移 -
        protected void ERPContactDataTransfer(SignForm signForm)
        {
            WFFlowData.WFNo = signForm.SignFormWFNO;
            GetSpecificFormNoERPLogInfoList(signForm.Rec93Form.ToString());

            foreach (var log in SpecificFormNoERPLogInfoList.Select((value, index) => new { Value = value, Index = index }))
            {
                SetErpWFLogNodeInfo(log.Value);

                switch (log.Value.lrec93_fsts)
                {
                    case null:
                        if (ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.APPLY) ||
                            ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.NEWFORM))
                        {
                            CheckFileAndUpload(signForm.SignFormNewUserID, signForm.Rec93Form.ToString());
                        }
                        else
                        {
                            //修改簽核名單且為紀錄最後一筆
                            if (ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.MODIFYSigList) &&
                                log.Index + 1 == SpecificFormNoERPLogInfoList.Count)
                            {
                                if (_firstTimeSig == false &&
                                    AddWFSignaturePara() == false)
                                {
                                    WriteErrorFormLog($"新增簽核名單失敗:{signForm.Rec93Form} / {signForm.SignFormNewUserID}{Environment.NewLine}");
                                    _forceEnd = true;
                                }
                            }
                            else
                            {
                                var user = string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.SigUserID)
                                    ? ConvertUserIDLength(WFFlowData.NewUserID)
                                    : ConvertUserIDLength(ErpWFLogNodeInfo.SigUserID);
                                AddRemarkPara(GetRunTimeWFFlowNode(), user, "ApplySignForm");
                            }
                        }
                        break;

                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "B":
                    case "F":
                        var sigResultID =
                            (ErpWFLogNodeInfo.Desc.Contains(GetEnumDescription(EnumLogDescType.PASS))
                             || ErpWFLogNodeInfo.Desc.Contains(GetEnumDescription(EnumLogDescType.COMPLETE)))
                                ? EnumSigResultID.A.ToString()
                                : (ErpWFLogNodeInfo.Desc.Contains(GetEnumDescription(EnumLogDescType.BACK)))
                                    ? EnumSigResultID.R.ToString()
                                    : EnumSigResultID.P.ToString();

                        if (_firstTimeSig == false &&
                            AddWFSignaturePara() == false)
                        {
                            WriteErrorFormLog($"新增簽核名單失敗:{signForm} / {signForm.SignFormNewUserID}{Environment.NewLine}");
                            _forceEnd = true;
                            break;
                        }
                        //非結案節點且(無下一筆紀錄 OR 當前和下一筆紀錄簽核關卡相同)
                        if (ErpWFLogNodeInfo.SigCategory != "F" &&
                            (log.Index + 1 == SpecificFormNoERPLogInfoList.Count || (ErpWFLogNodeInfo.SigCategory == SpecificFormNoERPLogInfoList[log.Index + 1].lrec93_fsts)))
                        {
                            AddWFRemarkPara(ErpWFLogNodeInfo.SigCategory);
                        }
                        else
                        {
                            string sigUserID;

                            if (string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.SigUserID))
                            {
                                sigUserID = GetUserID(log.Value.lrec93_mstfn);
                                if (string.IsNullOrWhiteSpace(sigUserID))
                                {
                                    _forceEnd = true;
                                    break;
                                }
                            }
                            else
                            {
                                sigUserID = ErpWFLogNodeInfo.SigUserID;
                            }

                            if (AddWFSignatureExecPara(sigResultID, sigUserID))
                            {
                                if (sigResultID.Equals(EnumSigResultID.A.ToString()) &&
                                    (log.Value.lrec93_fsts.Equals("4")
                                     || log.Value.lrec93_fsts.Equals("5")
                                     || log.Value.lrec93_fsts.Equals("F")))
                                {
                                    if (log.Value.lrec93_fsts.Equals("F"))
                                    {
                                        AddEndNodePara();
                                    }
                                    else
                                    {
                                        var erpNodeNum = (log.Value.lrec93_fsts.Equals("F")) ? string.Empty : SpecificFormNoERPLogInfoList[log.Index + 1].lrec93_fsts;
                                        AddNextToNodePara(erpNodeNum, ErpWFLogNodeInfo.SigUserID);
                                    }
                                }
                            }
                        }
                        break;

                    case "5":
                    case "6":
                    case "7":
                    case "A":
                        if (log.Value.lrec93_fsts.Equals("A") &&
                            (log.Index + 1 == SpecificFormNoERPLogInfoList.Count && (log.Value.lrec93_fsts != SpecificFormNoERPLogInfoList[log.Index + 1].lrec93_fsts)))
                        {
                            //簽核身分A & 當前和下一筆紀錄簽核關卡不同
                            AddNextToNodePara(SpecificFormNoERPLogInfoList[log.Index + 1].lrec93_fsts, ErpWFLogNodeInfo.SigUserID);

                            if (EditWFNodeProcessUserID(SpecificFormNoERPLogInfoList[log.Index + 1].lrec93_fsts))
                            {
                                if (SetWFSignature() == false)
                                {
                                    WriteErrorFormLog($"設定簽核名單失敗:{signForm.Rec93Form} / {signForm.SignFormNewUserID}{Environment.NewLine}");
                                    _forceEnd = true;
                                }
                            }
                        }
                        else
                        {
                            AddWFRemarkPara(SpecificFormNoERPLogInfoList[log.Index + 1].lrec93_fsts);
                        }
                        break;
                }
            }
        }
        #endregion

        #region - 取得指定單號LOG資訊清單 -
        /// <summary>
        /// 取得指定單號LOG資訊清單(含紀錄處理人員編)
        /// </summary>
        /// <param name="signFromNo"></param>
        private void GetSpecificFormNoERPLogInfoList(string signFromNo)
        {
            SpecificFormNoRecm94List = Recm94Dic[signFromNo].OrderBy(s => s.Rec94Fsts).ThenBy(s => s.Rec94No).ToList();
            var logRecm93List = LogRecm93Dic[signFromNo];

            SpecificFormNoERPLogInfoList = (from data in logRecm93List
                                            let hasUser = SpecificFormNoRecm94List.Any(sign => data.Lrec93Mstfn.Contains(sign.StfnCname)) //處理紀錄人員是否存在於簽核名單中
                                            select new LogInfo
                                            {
                                                Applicant = data.Rec93Stfn,
                                                SignedUser = hasUser
                                                    ? SpecificFormNoRecm94List.First(sign => data.Lrec93Mstfn.Contains(sign.StfnCname)).Rec94Stfn
                                                    : GetUserID(data.Lrec93Mstfn),

                                                lrec93_form = data.Lrec93Form,
                                                lrec93_date = data.Lrec93Date,
                                                lrec93_fsts = data.Lrec93Fsts,
                                                lrec93_hidden = data.Lrec93Hidden,
                                                lrec93_bgcolor = data.Lrec93Bgcolor,
                                                lrec93_mstfn = data.Lrec93Mstfn,
                                                lrec93_mdate = data.Lrec93Mdate,
                                                lrec93_desc = data.Lrec93Desc
                                            }).ToList();
        }
        #endregion

        #region - 檢查文件並上傳 -
        /// <summary>
        /// 檢查文件並上傳
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="formNO"></param>
        /// <returns></returns>
        public bool CheckFileAndUpload(string userID, string formNO)
        {
            try
            {
                EntityBatch.WFFilePara para = new EntityBatch.WFFilePara
                {
                    WFNo = formNO
                };
                var wfFileList = _connERP.CheckWFFile(para);
                remarkNoSource += wfFileList.Count;

                foreach (var row in wfFileList)
                {
                    long contentLength;
                    string fileNM = row.FilePath.Split(new[] { "/17/" }, StringSplitOptions.RemoveEmptyEntries).Last();
                    string erpFilePath = $"{GetEnumDescription(Model.EnumERPFilePath.PATH)}{row.FilePath}";
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(erpFilePath);
                    request.Method = "HEAD";
                    request.Timeout = 20000;

                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            contentLength = response.ContentLength;
                        }
                    }
                    catch (Exception)
                    {
                        return true;
                    }

                    if (contentLength > 0)
                    {
                        byte[] file = new WebClient().DownloadData(erpFilePath);
                        string serverDir = @"\\localhost\APData\WFAP\WorkFlow\Document\";
                        string encodeName = $"{Guid.NewGuid().ToString("N")}{Guid.NewGuid().ToString("N").Substring(0, 16)}";
                        string docEncodeNM = $@"{WFFlowData.WFNo}.{encodeName}";
                        string SERPFilePath = $@"{serverDir}\{WFFlowData.WFNo}.{encodeName}";

                        if (Directory.Exists(serverDir) == false)
                        {
                            Directory.CreateDirectory(serverDir);
                        }

                        FileStream fs = new FileStream(SERPFilePath, FileMode.Create, FileAccess.Write);
                        fs.Write(file, 0, file.Length);
                        fs.Close();

                        EntityBatch.AddDocumentPara docPara = new EntityBatch.AddDocumentPara
                        {
                            WFNo = WFFlowData.WFNo,
                            NodeNO = "001",
                            WFDocSeq = "001",
                            DocUserID = userID,
                            DocFileNM = fileNM,
                            DocEncodeNM = docEncodeNM,
                            DocPath = $@"{serverDir}.{docEncodeNM}",
                            DocLocalPath = DBNull.Value.ToString(),
                            UpdUserID = userID,
                            Remark = string.Empty
                        };
                        _connUSerp.AddDocument(docPara);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}/{formNO}");
                Console.Read();
            }

            return true;
        }
        #endregion

        #region - 新增註記參數資料 -
        /// <summary>
        /// 增加註記
        /// </summary>
        /// <param name="nodeNum"></param>
        /// <param name="userID"></param>
        /// <param name="nodeID"></param>
        private void AddRemarkPara(string nodeNum, string userID, string nodeID)
        {
            AddRemarkParaList.Add(new EntityBatch.AddRemarkPara
            {
                WFNo = WFFlowData.WFNo,
                RemarkNO = Convert.ToString(remarkNoSource).PadLeft(3, '0'),
                NodeNum = nodeNum,
                SysID = WFFlowData.SysID,
                FlowID = WFFlowData.FlowID,
                FlowVer = WFFlowData.FlowVer,
                WFNodeID = nodeID,
                NodeNO = nodeNum,
                RemarkUserID = userID,
                UpdUserID = userID,
                Remark = string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.Desc) ? DBNull.Value.ToString() : ErpWFLogNodeInfo.Desc
            });
        }
        #endregion

        #region - 新增WF簽核名單參數資料 -
        /// <summary>
        /// 新增WF簽核名單參數資料
        /// </summary>
        /// <returns></returns>
        private bool AddWFSignaturePara()
        {
            _firstTimeSig = true;

            try
            {
                var applySignFormSigUserList = GetNodeSigUserList();

                if (applySignFormSigUserList != null &&
                    applySignFormSigUserList.Any())
                {
                    SetWFSignatureParaList.Add(new EntityBatch.SetWFSignaturePara
                    {
                        WFNo = WFFlowData.WFNo,
                        IsStartSig = true,
                        UpdUserID = ConvertUserIDLength(WFFlowData.UpdUserID),
                        WFSigList = applySignFormSigUserList
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} : {WFFlowData.WFNo}");
                Console.Read();
            }

            return false;
        }
        #endregion

        #region - 設定WF簽核名單TVP -
        /// <summary>
        /// 設定WF簽核名單
        /// </summary>
        /// <returns></returns>
        private bool SetWFSignatureTVP()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString))
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(
                    @"IF type_id('[dbo].[SETSIGNATURE_TYPE]') IS NOT NULL
                    DROP TYPE [dbo].[SETSIGNATURE_TYPE];

                    CREATE TYPE SETSIGNATURE_TYPE AS TABLE
                    (

                    );", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (SqlCommand cmd = new SqlCommand("INSERT INTO WF_SIG SELECT * FROM @TVPSignatureData; DROP TYPE [dbo].[SETSIGNATURE_TYPE]", connection))
                    {
                        SqlParameter tvp = cmd.Parameters.Add("@TVPSignatureData", SqlDbType.Structured);
                        tvp.Value = ListToDatatable(SetWFSignatureParaList);
                        tvp.TypeName = "SETSIGNATURE_TYPE";

                        Console.WriteLine("設定簽核名單");
                        Stopwatch sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        cmd.ExecuteNonQuery();
                        sw.Stop();
                        Console.WriteLine("耗時:" + (sw.ElapsedMilliseconds) + "毫秒\n=====================================");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} : {WFFlowData.WFNo}");
                Console.Read();
            }

            return false;
        }
        #endregion

        #region - 設定WF簽核名單 -
        /// <summary>
        /// 設定WF簽核名單
        /// </summary>
        /// <returns></returns>
        private bool SetWFSignature()
        {
            try
            {
                _connUSerp.SetWFSignature(SetWFSignatureParaList);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} : {WFFlowData.WFNo}");
                Console.Read();
            }

            return false;
        }
        #endregion

        #region - 新增WF註記參數 -
        /// <summary>
        /// 新增WF註記參數
        /// </summary>
        /// <param name="erpNodeNum"></param>
        private void AddWFRemarkPara(string erpNodeNum)
        {
            var userID = GetProcessUserID(erpNodeNum);

            AddWFRemarkParaList.Add(new EntityBatch.AddWFRemarkPara
            {
                WFNo = WFFlowData.WFNo,
                UserID = userID,
                UpdUserID = userID,
                Remark = string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.Desc) ? DBNull.Value.ToString() : ErpWFLogNodeInfo.Desc
            });
        }
        #endregion

        #region - 新增WF註記 -
        /// <summary>
        /// 新增WF註記
        /// </summary>
        private void AddWFRemark()
        {
            _connUSerp.AddWFRemark(AddWFRemarkParaList);
        }
        #endregion

        #region - 新增簽核參數資料 -
        /// <summary>
        /// 新增簽核參數資料
        /// </summary>
        /// <param name="sigResultID"></param>
        /// <param name="sigUserID"></param>
        /// <returns></returns>
        public bool AddWFSignatureExecPara(string sigResultID, string sigUserID)
        {
            try
            {
                WFSignatureExecaraList.Add(new EntityBatch.WFSignaturePara
                {
                    WFNo = WFFlowData.WFNo,
                    NodeNO = GetRunTimeWFFlowNode(),
                    UserID = ConvertUserIDLength(sigUserID),
                    SigResultID = sigResultID
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} : {WFFlowData.WFNo}");
                Console.Read();
            }

            return false;
        }
        #endregion

        #region - 簽核 -
        /// <summary>
        /// 簽核
        /// </summary>
        protected void EditWFSignature()
        {
            _connUSerp.EditWFSignature(WFSignatureExecaraList);
        }
        #endregion

        #region - 設定ERP簽核紀錄節點資訊(LOG描述、簽核者、簽核者身分代碼、單據申請者) -
        /// <summary>
        /// 設定ERP簽核紀錄節點資訊(LOG描述、簽核者、簽核者身分代碼、單據申請者)
        /// </summary>
        /// <param name="log"></param>
        protected void SetErpWFLogNodeInfo(LogInfo log)
        {
            ErpWFLogNodeInfo = new ErpWFLogNode
            {
                Desc = log.lrec93_desc,
                SigUserID = ConvertUserIDLength(log.SignedUser),
                SigCategory = log.lrec93_fsts
            };

            WFFlowData.NewUserID = log.Applicant;
        }
        #endregion

        #region - 取得節點處理人 -
        /// <summary>
        /// 取得節點處理人
        /// </summary>
        /// <param name="erpNodeNum"></param>
        private string GetProcessUserID(string erpNodeNum)
        {
            string processUserID = SpecificFormNoRecm94List
                .Where(f => f.Rec94Fsts == erpNodeNum)
                .Select(n => n.Rec94Stfn).First();

            return ConvertUserIDLength(processUserID);
        }
        #endregion

        #region - 取得結點簽核名單 -
        /// <summary>
        ///  取得結點簽核名單
        /// </summary>
        private List<EntityBatch.SetSigValue> GetNodeSigUserList()
        {
            var sigStep = 1;
            var addSigStep = 5;
            var onceAppearedList = new List<string>();
            bool isSignStep = new List<string> { null, "1", "2" }.Contains(ErpWFLogNodeInfo.SigCategory);

            var sigUserList = SpecificFormNoRecm94List
                .Where(f => (isSignStep)
                    ? (Regex.IsMatch(f.Rec94Fsts, @"[0-9]$") && int.Parse(f.Rec94Fsts) < 5)
                    : (Regex.IsMatch(f.Rec94Fsts, @"[0-9]$") && new List<string> { "6", "7" }.Contains(f.Rec94Fsts))
                      || new List<string> { "B", "F" }.Contains(f.Rec94Fsts)).ToList();

            var unitSigUser = sigUserList.Where(n => n.Rec94Fsts == "2").Select(e => e.Rec94Stfn).LastOrDefault();
            var processSigUser = sigUserList.Where(n => n.Rec94Fsts == "4").Select(e => e.Rec94Stfn).LastOrDefault();

            var result = sigUserList.Select(n =>
            {
                var sigSeq =
                    (Regex.IsMatch(n.Rec94Fsts, @"[0-9]$"))
                        ? n.Rec94Fsts.PadLeft(3, '0')
                        : new List<string> { "2", "4" }[new List<string> { "B", "F" }.IndexOf(n.Rec94Fsts)].PadLeft(3, '0');
                var userID = n.Rec94Stfn;

                switch (sigSeq)
                {
                    case "002":
                        if (onceAppearedList.Contains(sigSeq) == false)
                        {
                            userID = unitSigUser;
                            onceAppearedList.Add(sigSeq);
                        }
                        else
                        {
                            sigSeq = string.Empty;
                        }
                        break;
                    case "004":
                        if (onceAppearedList.Contains(sigSeq) == false)
                        {
                            userID = processSigUser;
                            onceAppearedList.Add(sigSeq);
                        }
                        else
                        {
                            sigSeq = string.Empty;
                        }
                        break;
                    default:
                        if (onceAppearedList.Contains(sigSeq) == false)
                        {
                            sigSeq = Convert.ToString(addSigStep++).PadLeft(3, '0');
                        }
                        onceAppearedList.Add(sigSeq);
                        break;
                }

                return new EntityBatch.SetSigValue
                {
                    SigStep = sigStep++,
                    SigUserID = ConvertUserIDLength(userID),
                    WFSigSeq = sigSeq
                };
            }).Where(d => string.IsNullOrWhiteSpace(d.WFSigSeq) == false).ToList();

            return result.Any() ? result : new List<EntityBatch.SetSigValue>();
        }
        #endregion

        #region - 取得目前節點 -
        /// <summary>
        /// 取得目前節點
        /// </summary>
        /// <returns></returns>
        private string GetRunTimeWFFlowNode()
        {
            EntityBatch.RunTimeWfFlowPara para = new EntityBatch.RunTimeWfFlowPara
            {
                WFNo = WFFlowData.WFNo
            };

            var nodeNo = _connUSerp.GetRunTimeWFFlow(para);

            return nodeNo;
        }
        #endregion

        #region - 員工編號4碼轉6碼 -
        /// <summary>
        /// 員工編號4碼轉6碼
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        protected string ConvertUserIDLength(string userID)
        {
            var newUserID = string.Empty;

            if (string.IsNullOrWhiteSpace(userID) == false)
            {
                newUserID = (userID.Length == 6) ? userID : (userID.Substring(0, 1) == "Z") ? $"ZZ{userID}" : $"00{userID}";
            }

            return newUserID;
        }
        #endregion

        #region - 增加結束節點參數 -
        /// <summary>
        /// 結束節點取得目前節點
        /// </summary>
        private void AddEndNodePara()
        {
            var userID = GetProcessUserID("F");
            ToEndNodeParaList.Add(new EntityBatch.ToEndNodePara
            {
                WFNo = WFFlowData.WFNo,
                NodeNO = GetRunTimeWFFlow(),
                UserID = userID,
                UpdUserID = userID
            });
        }
        #endregion

        #region - 結束節點 -
        protected void EditToEndNode()
        {
            _connUSerp.EditToEndNode(ToEndNodeParaList);
        }
        #endregion

        #region - 新增移至下一節點參數 -
        /// <summary>
        /// 新增移至下一節點參數
        /// </summary>
        /// <param name="erpNodeNum"></param>
        /// <param name="sigUerID"></param>
        protected void AddNextToNodePara(string erpNodeNum, string sigUerID)
        {
            NextToNodeParaList.Add(new EntityBatch.NextToNodePara
            {
                NewUserID = DBNull.Value.ToString(),
                WFNo = string.IsNullOrWhiteSpace(WFFlowData.WFNo) ? DBNull.Value.ToString() : WFFlowData.WFNo,
                UserID = string.IsNullOrWhiteSpace(sigUerID) ? DBNull.Value.ToString() : ConvertUserIDLength(sigUerID),
                UpdUserID = WFFlowData.UpdUserID,
                NodeUserParaList = new List<EntityBatch.NodeNewUserPara>
                {
                    new EntityBatch.NodeNewUserPara { NewUserID = string.IsNullOrWhiteSpace(erpNodeNum) ? ConvertUserIDLength(sigUerID) : GetProcessUserID(erpNodeNum) }
                }
            });
        }
        #endregion

        #region - 移至下一節點 -
        /// <summary>
        /// 移至下一節點
        /// </summary>
        private void NextToNode()
        {

        }
        #endregion

        #region - 取得目前節點 -
        /// <summary>
        /// 取得目前節點
        /// </summary>
        /// <returns></returns>
        private string GetRunTimeWFFlow()
        {
            EntityBatch.RunTimeWfFlowPara para = new EntityBatch.RunTimeWfFlowPara
            {
                WFNo = WFFlowData.WFNo
            };

            var nodeNo = _connUSerp.GetRunTimeWFFlow(para);

            return nodeNo;
        }
        #endregion

        #region - 成為節點處理人 -
        /// <summary>
        /// 成為節點處理人
        /// </summary>
        /// <param name="fsts"></param>
        /// <returns></returns>
        private bool EditWFNodeProcessUserID(string fsts)
        {
            var userID = GetProcessUserID(fsts);

            EntityBatch.EditWFNodeProcessUserIDPara para = new EntityBatch.EditWFNodeProcessUserIDPara
            {
                WFNo = WFFlowData.WFNo,
                UserID = userID,
                UpdUserID = userID,
                NewUserID = userID
            };

            return _connUSerp.EditWFNodeProcessUserID(para).Result == "Success";
        }
        #endregion

        #region - 取得員工編號 -
        /// <summary>
        /// 取得員工編號
        /// </summary>
        /// <param name="userNM"></param>
        /// <returns></returns>
        private string GetUserID(string userNM)
        {
            EntityBatch.UserIDPara para = new EntityBatch.UserIDPara
            {
                UserNM = userNM
            };

            var userInfo = _connERP.GetUserID(para);

            if (userInfo.Any())
            {
                return userInfo.First().STFN;
            }

            return string.Empty;
        }
        #endregion

        #region - 寫入錯誤聯絡單紀錄 -
        /// <summary>
        /// 寫入錯誤聯絡單紀錄
        /// </summary>
        /// <param name="logStr"></param>
        public void WriteErrorFormLog(string logStr)
        {
            string filePath = GetEnumDescription(Model.EnumErrorFormLogFilePath.LOG_FILE_PATH);
            string writeStr = logStr;

            FileInfo finfo = new FileInfo(filePath);
            if (finfo.Directory != null &&
                finfo.Directory.Exists == false)
            {
                finfo.Directory.Create();
            }

            FileStream fs = File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(writeStr);
            sw.Dispose();
            fs.Dispose();
        }
        #endregion

        #region - 取得列舉描述 -
        /// <summary>
        /// 取得列舉描述
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return (attributes.Length > 0) ? attributes[0].Description : value.ToString();
        }
        #endregion

        #region - List轉DataTable -
        private DataTable ListToDatatable<T>(IEnumerable<T> dataList)
        {
            var dt = new DataTable();

            var props = typeof(T).GetProperties();
            dt.Columns.AddRange(props.Select(p =>
                new DataColumn(p.Name,
                    (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        ? p.PropertyType.GetGenericArguments()[0]
                        : p.PropertyType)).ToArray());

            dataList.ToList().ForEach(remark => dt.LoadDataRow(props.Select(pi => pi.GetValue(remark, null)).ToArray(), true));

            return dt;
        }
        #endregion
    }
}