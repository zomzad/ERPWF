using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ERPWF
{
    internal class WorkflowModel
    {
        #region - Definitions -
        public enum EnumSigResultID
        {
            P,
            R,
            A
        }

        public enum EnumErrorFormLogFilePath
        {
            [Description(@"C:\USER\LOG.txt")]
            LOG_FILE_PATH,

            [Description(@"C:\USER\USER_ID.txt")]
            USER_FILE_PATH
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

        public class WFFlow
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

        public class Rec94
        {
            public int rec94Form { get; set; }
            public byte rec94NO { get; set; }
            public string rec94Fsts { get; set; }
            public string rec94Stfn { get; set; }
            public string stfnCname { get; set; }
        }

        public class LogRecm93
        {
            public string rec93_mstfn { get; set; }
            public string rec93_stfn { get; set; }
            public int lrec93_form { get; set; }
            public DateTime lrec93_date { get; set; }
            public string lrec93_fsts { get; set; }
            public bool lrec93_hidden { get; set; }
            public string lrec93_bgcolor { get; set; }
            public string lrec93_mstfn { get; set; }
            public DateTime lrec93_mdate { get; set; }
            public string lrec93_desc { get; set; }
        }
        #endregion

        #region - Property -
        public List<LogInfo> LogInfoList { get; private set; }
        public List<Rec94> Rec94List { get; set; }
        public List<Rec94> SignedUserList { get; private set; }
        
        public List<LogRecm93> LogRecm93List { get; set; }
        public List<SignForm> SignFormList { get; set; }
        public ErpWFLogNode ErpWFLogNodeInfo { get; set; }
        public WFFlow WFFlowData { get; set; }
        #endregion

        #region - Private -
        protected readonly EntityWorkflow _connUSerpStr;
        protected readonly EntityWorkflow _entityERP;
        private bool _forceEnd;
        private bool _firstTimeSig;
        #endregion

        public WorkflowModel()
        {
            _connUSerpStr = new EntityWorkflow(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString);
            _entityERP = new EntityWorkflow(ConfigurationManager.ConnectionStrings["UERPConnection"].ConnectionString);

            WFFlowData = new WFFlow
            {
                SysID = "PUBAP",
                FlowID = "SignForm",
                FlowVer = "001",
                Subject = "簽核單",
                UpdUserID = "APIService.ERP.WorkFlowService"
            };
        }

        #region - 取得聯絡單簽核單所有必需資料 -
        /// <summary>
        /// 取得聯絡單簽核單所有必需資料
        /// </summary>
        public void GetAllNecessarySignData()
        {
            DataSet signInfoDs = _entityERP.GetSerpSignFormInfoList();

            SignFormList = signInfoDs.Tables[0].ToList<SignForm>().ToList();
            Rec94List = signInfoDs.Tables[1].ToList<Rec94>().ToList();
            var rec93List = signInfoDs.Tables[2].ToList<recm93>();
            var logRecm93List = signInfoDs.Tables[3].ToList<LogRecm93>();

            #region - 所有簽核單LOG記錄檔清單 -
            LogRecm93List =
                (from log in logRecm93List
                 join signForm in rec93List
                     on log.lrec93_form equals signForm.rec93_form into s
                 from signForm in s.DefaultIfEmpty()
                 select new LogRecm93
                 {
                     rec93_mstfn = signForm.rec93_mstfn,
                     rec93_stfn = signForm.rec93_stfn,
                     lrec93_form = log.lrec93_form,
                     lrec93_date = log.lrec93_date,
                     lrec93_fsts = log.lrec93_fsts,
                     lrec93_hidden = log.lrec93_hidden,
                     lrec93_bgcolor = log.lrec93_bgcolor,
                     lrec93_mstfn = log.lrec93_mstfn,
                     lrec93_mdate = log.lrec93_mdate,
                     lrec93_desc = log.lrec93_desc
                 }).ToList();
            #endregion

            EditSerpwfData();
        }
        #endregion

        #region - 編輯SERP工作流程資料 -
        /// <summary>
        /// 編輯SERP工作流程資料
        /// </summary>
        private void EditSerpwfData()
        {
            var executeNum = 1;

            foreach (var sign in SignFormList)
            {
                Console.WriteLine(executeNum++);

                switch (sign.FSTS)
                {
                    case "F":
                        ConvertToEndWFForm(sign);
                        break;
                    default:
                        ConvertToWFForm(sign);
                        break;
                }

                Console.Clear();
            }
        }
        #endregion

        protected void ConvertToEndWFForm(SignForm sign)
        {
            GetSpecificFormNOLogInfoList(sign.Rec93Form.ToString());

            foreach (var log in LogInfoList.Select((value, index) => new { Value = value, Index = index }))
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
                            (ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.APPLY) || ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.NEWFORM)))
                        {
                            if (EditNewWFFlow(sign) == false)
                            {
                                _forceEnd = true;
                                break;
                            }

                            AddSignForm(sign);
                            CheckFileAndUpload(sign.SignFormNewUserID, sign.Rec93Form.ToString());
                        }
                        else
                        {
                            AddRemark("001", user, "ApplySignForm");
                        }
                        break;

                    case "5":
                        AddRemark("002", user, "ProcessSignForm");
                        break;

                    case "6":
                    case "7":
                    case "A":
                    case "B":
                    case "F":
                        AddRemark("003", user, "ApplySignForm");
                        break;
                }

            }

            if (_forceEnd == false)
            {
                EditWFENDFlow(GetProcessUserID("F"));
            }

            _forceEnd = false;
        }

        #region - 聯絡單資料表轉換 -
        /// <summary>
        /// 聯絡單資料表轉換
        /// </summary>
        /// <param name="sign"></param>
        public void ConvertToWFForm(SignForm sign)
        {
            GetSpecificFormNOLogInfoList(sign.Rec93Form.ToString());

            foreach (var log in LogInfoList.Select((value, index) => new { Value = value, Index = index }))
            {
                SetErpWFLogNodeInfo(log.Value);

                switch (log.Value.lrec93_fsts)
                {
                    case null:
                        if (ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.APPLY) ||
                            ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.NEWFORM))
                        {
                            //申請或立單
                            if (EditNewWFFlow(sign) == false)
                            {
                                _forceEnd = true;
                                break;
                            }

                            AddSignForm(sign);
                            CheckFileAndUpload(sign.SignFormNewUserID, sign.Rec93Form.ToString());
                        }
                        else
                        {
                            //修改簽核名單且為紀錄最後一筆
                            if (ErpWFLogNodeInfo.Desc == GetEnumDescription(EnumLogDescType.MODIFYSigList) &&
                                log.Index + 1 == LogInfoList.Count)
                            {
                                if (_firstTimeSig == false &&
                                    SetWFSignature() == false)
                                {
                                    WriteErrorFormLog($"設定簽核名單失敗:{sign.Rec93Form} / {sign.SignFormNewUserID}{Environment.NewLine}");
                                    _forceEnd = true;
                                }
                            }
                            else
                            {
                                var user = string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.SigUserID)
                                    ? ConvertUserIDLength(WFFlowData.NewUserID)
                                    : ConvertUserIDLength(ErpWFLogNodeInfo.SigUserID);
                                AddRemark(GetRunTimeWFFlow(), user, "ApplySignForm");
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
                            SetWFSignature() == false)
                        {
                            WriteErrorFormLog($"設定簽核名單失敗:{sign.Rec93Form} / {sign.SignFormNewUserID}{Environment.NewLine}");
                            _forceEnd = true;
                            break;
                        }
                        //非結案節點且(無下一筆紀錄 OR 當前和下一筆紀錄簽核關卡相同)
                        if (ErpWFLogNodeInfo.SigCategory != "F" &&
                            (log.Index + 1 == LogInfoList.Count || (ErpWFLogNodeInfo.SigCategory == LogInfoList[log.Index + 1].lrec93_fsts)))
                        {
                            AddWFRemark(ErpWFLogNodeInfo.SigCategory);
                        }
                        else
                        {
                            string sigUserID;

                            if (string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.SigUserID))
                            {
                                sigUserID = GetSigRemoveUserID(sign, log.Value.lrec93_mstfn);
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

                            if (EditWFSignature(sigResultID, sigUserID))
                            {
                                if (sigResultID.Equals(EnumSigResultID.A.ToString()) &&
                                    (log.Value.lrec93_fsts.Equals("4")
                                     || log.Value.lrec93_fsts.Equals("5")
                                     || log.Value.lrec93_fsts.Equals("F")))
                                {
                                    if (log.Value.lrec93_fsts.Equals("F"))
                                    {
                                        EditToEndNode();
                                    }
                                    else
                                    {
                                        var erpNodeNum = (log.Value.lrec93_fsts.Equals("F")) ? string.Empty : LogInfoList[log.Index + 1].lrec93_fsts;
                                        NextToNode(erpNodeNum, ErpWFLogNodeInfo.SigUserID);
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
                            (log.Index + 1 == LogInfoList.Count && (log.Value.lrec93_fsts != LogInfoList[log.Index + 1].lrec93_fsts)))
                        {//簽核身分A & 當前和下一筆紀錄簽核關卡不同
                            NextToNode(LogInfoList[log.Index + 1].lrec93_fsts, ErpWFLogNodeInfo.SigUserID);

                            if (EditWFNodeProcessUserID(LogInfoList[log.Index + 1].lrec93_fsts))
                            {
                                if (SetWFSignature() == false)
                                {
                                    WriteErrorFormLog($"設定簽核名單失敗:{sign.Rec93Form} / {sign.SignFormNewUserID}{Environment.NewLine}");
                                    _forceEnd = true;
                                }
                            }
                        }
                        else
                        {
                            AddWFRemark(LogInfoList[log.Index + 1].lrec93_fsts);
                        }
                        break;
                }

                if (_forceEnd)
                {
                    break;
                }

                _forceEnd = false;
            }
        }
        #endregion

        #region - 取得指定單號LOG資訊清單 -
        /// <summary>
        /// 取得指定單號LOG資訊清單
        /// </summary>
        /// <param name="signFromNo"></param>
        private void GetSpecificFormNOLogInfoList(string signFromNo)
        {
            SignedUserList = Rec94List.Where(r => r.rec94Form.ToString() == signFromNo)
                .OrderBy(s => s.rec94Fsts).ThenBy(s => s.rec94NO).ToList();//SignedUserList

            var logRecm93List = LogRecm93List.Where(r => r.lrec93_form.ToString() == signFromNo);

            LogInfoList = (from data in logRecm93List
                           let hasUser = SignedUserList.Any(sign => data.lrec93_mstfn.Contains(sign.stfnCname))
                           select new LogInfo
                           {
                               Applicant = data.rec93_stfn,
                               SignedUser = hasUser ? SignedUserList.First(sign => data.lrec93_mstfn.Contains(sign.stfnCname)).rec94Stfn : string.Empty,
                               lrec93_form = data.lrec93_form,
                               lrec93_date = data.lrec93_date,
                               lrec93_fsts = data.lrec93_fsts,
                               lrec93_hidden = data.lrec93_hidden,
                               lrec93_bgcolor = data.lrec93_bgcolor,
                               lrec93_mstfn = data.lrec93_mstfn,
                               lrec93_mdate = data.lrec93_mdate,
                               lrec93_desc = data.lrec93_desc
                           }).ToList();
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

        #region - 員工編號4碼轉6碼 -
        /// <summary>
        /// 員工編號4碼轉6碼
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        protected string ConvertUserIDLength(string userID)
        {
            string newUserID = string.Empty;

            if (string.IsNullOrWhiteSpace(userID) == false)
            {
                newUserID = (userID.Length == 6) ? userID : (userID.Substring(0, 1) == "Z") ? $"ZZ{userID}" : $"00{userID}";
            }

            return newUserID;
        }
        #endregion

        #region - 增加註記 -
        /// <summary>
        /// 增加註記
        /// </summary>
        /// <param name="nodeNum"></param>
        /// <param name="userID"></param>
        /// <param name="nodeID"></param>
        private void AddRemark(string nodeNum, string userID, string nodeID)
        {
            EntityWorkflow.AddRemarkPara para = new EntityWorkflow.AddRemarkPara
            {
                WFNo = WFFlowData.WFNo,
                NodeNum = nodeNum,
                SysID = WFFlowData.SysID,
                FlowID = WFFlowData.FlowID,
                FlowVer = WFFlowData.FlowVer,
                WFNodeID = nodeID,
                NodeNO = nodeNum,
                RemarkUserID = userID,
                UpdUserID = userID,
                Remark = string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.Desc) ? DBNull.Value.ToString() : ErpWFLogNodeInfo.Desc
            };

            _connUSerpStr.AddRemark(para);
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

        #region - 新增工作流程 -
        /// <summary>
        /// 新增工作流程
        /// </summary>
        /// <param name="sign"></param>
        private bool EditNewWFFlow(SignForm sign)
        {
            try
            {
                EntityWorkflow.NewWFFlowPara para = new EntityWorkflow.NewWFFlowPara
                {
                    SysID = WFFlowData.SysID,
                    FlowID = WFFlowData.FlowID,
                    FlowVer = WFFlowData.FlowVer,
                    Subject = WFFlowData.Subject,
                    UserID = ConvertUserIDLength(WFFlowData.NewUserID)
                };
                var wfFlowData = _connUSerpStr.EditNewWFFlow(para);

                if (string.IsNullOrWhiteSpace(wfFlowData.WFNo))
                {
                    WriteErrorFormLog($"單號:{sign.Rec93Form} / {sign.SignFormNewUserID} - 此人帳號停用 新增WF失敗 {wfFlowData.Result}{Environment.NewLine}");
                    WriteErrorUserLog($"{sign.SignFormNewUserID}{Environment.NewLine}");
                    return false;
                }
                WFFlowData.WFNo = wfFlowData.WFNo;

                return true;
            }
            catch (Exception ex)
            {
                WriteErrorFormLog($"單號:{sign.Rec93Form} / {sign.SignFormNewUserID} - {ex.Message}{Environment.NewLine}");
                WriteErrorUserLog($"{sign.SignFormNewUserID}{Environment.NewLine}");
            }

            return false;
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

        #region - 寫入錯誤聯絡單紀錄 -
        /// <summary>
        /// 寫入錯誤聯絡單紀錄
        /// </summary>
        /// <param name="logStr"></param>
        public void WriteErrorUserLog(string logStr)
        {
            string filePath = GetEnumDescription(EnumErrorFormLogFilePath.USER_FILE_PATH);
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

        #region - 取得節點處理人 -
        /// <summary>
        /// 取得節點處理人
        /// </summary>
        /// <param name="erpNodeNum"></param>
        private string GetProcessUserID(string erpNodeNum)
        {
            string processUserID = SignedUserList
                .Where(f => f.rec94Fsts == erpNodeNum)
                .Select(n => n.rec94Stfn).First();

            return ConvertUserIDLength(processUserID);
        }
        #endregion

        #region - 結案單_結束節點 -
        /// <summary>
        /// 結案單_結束節點
        /// </summary>
        /// <param name="userID"></param>
        public void EditWFENDFlow(string userID)
        {
            EntityWorkflow.WFENDFlowPara para = new EntityWorkflow.WFENDFlowPara
            {
                WFNo = WFFlowData.WFNo,
                UserID = userID,
                UpdUserID = userID
            };

            _connUSerpStr.EditWFENDFlow(para);
        }
        #endregion

        #region - 新增聯絡單 -
        /// <summary>
        /// 新增聯絡單
        /// </summary>
        public void AddSignForm(SignForm sign)
        {
            SignForm para = new SignForm
            {
                SignFormNO = string.IsNullOrWhiteSpace(sign.SignFormNO) ? DBNull.Value.ToString() : sign.SignFormNO,
                SignFormWFNO = string.IsNullOrWhiteSpace(WFFlowData.WFNo) ? DBNull.Value.ToString() : WFFlowData.WFNo,
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
            };

            _connUSerpStr.AddSignForm(para);
        }
        #endregion

        #region - 檢查文件並上傳 -
        /// <summary>
        /// 檢查文件並上傳
        /// </summary>
        public bool CheckFileAndUpload(string userID, string formNO)
        {
            try
            {
                EntityWorkflow.WFFilePara para = new EntityWorkflow.WFFilePara
                {
                    WFNo = formNO
                };
                var wfFileList = _entityERP.CheckWFFile(para);

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

                        EntityWorkflow.AddDocumentPara docPara = new EntityWorkflow.AddDocumentPara
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
                        _connUSerpStr.AddDocument(docPara);
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

        #region - 設定WF簽核名單 -
        /// <summary>
        /// 設定WF簽核名單
        /// </summary>
        /// <returns></returns>
        private bool SetWFSignature()
        {
            _firstTimeSig = true;

            try
            {
                var applySignFormSigUserList = GetNodeSigUserList();

                if (applySignFormSigUserList != null &&
                    applySignFormSigUserList.Any())
                {
                    EntityWorkflow.SetWFSignaturePara para = new EntityWorkflow.SetWFSignaturePara
                    {
                        WFNo = WFFlowData.WFNo,
                        IsStartSig = true,
                        UpdUserID = ConvertUserIDLength(WFFlowData.UpdUserID),
                        WFSigList = applySignFormSigUserList
                    };

                    return _connUSerpStr.SetWFSignature(para).Result == "Y";
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

        #region - 取得目前節點 -
        /// <summary>
        /// 取得目前節點
        /// </summary>
        /// <returns></returns>
        private string GetRunTimeWFFlow()
        {
            EntityWorkflow.RunTimeWfFlowPara para = new EntityWorkflow.RunTimeWfFlowPara
            {
                WFNo = WFFlowData.WFNo
            };

            var nodeNo = _connUSerpStr.GetRunTimeWFFlow(para);

            return nodeNo;
        }
        #endregion

        #region - 增加WF註記 -
        /// <summary>
        /// 增加WF註記
        /// </summary>
        /// <param name="erpNodeNum"></param>
        private void AddWFRemark(string erpNodeNum)
        {
            var userID = GetProcessUserID(erpNodeNum);

            EntityWorkflow.AddWFRemarkPara para = new EntityWorkflow.AddWFRemarkPara
            {
                WFNo = WFFlowData.WFNo,
                UserID = userID,
                UpdUserID = userID,
                Remark = string.IsNullOrWhiteSpace(ErpWFLogNodeInfo.Desc) ? DBNull.Value.ToString() : ErpWFLogNodeInfo.Desc
            };

            _connUSerpStr.AddWFRemark(para);
        }
        #endregion

        #region - 取得員工編號(針對原本在簽核名單內但被刪除者) -
        /// <summary>
        /// 取得員工編號(針對原本在簽核名單內但被刪除者)
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="userNM"></param>
        /// <returns></returns>
        private string GetSigRemoveUserID(SignForm sign, string userNM)
        {
            EntityWorkflow.SigRemoveUserIDPara para = new EntityWorkflow.SigRemoveUserIDPara
            {
                UserNM = userNM
            };

            var userInfo = _entityERP.GetSigRemoveUserID(para);

            if (userInfo.Any())
            {
                return userInfo.First().STFN;
            }

            WriteErrorFormLog($"單號:{sign.Rec93Form} / {sign.SignFormNewUserID} - 無此人{Environment.NewLine}");
            WriteErrorUserLog($"{sign.SignFormNewUserID}{Environment.NewLine}");
            return string.Empty;
        }
        #endregion

        #region - 簽核 -
        /// <summary>
        /// 簽核
        /// </summary>
        /// <param name="sigResultID"></param>
        /// <param name="sigUserID"></param>
        private bool EditWFSignature(string sigResultID, string sigUserID)
        {
            var resultList = new List<string> { "NotProcessNode", "NotSignUser" };
            EntityWorkflow.WFSignaturePara para = new EntityWorkflow.WFSignaturePara
            {
                WFNo = WFFlowData.WFNo,
                NodeNO = GetRunTimeWFFlow(),
                UserID = ConvertUserIDLength(sigUserID),
                SigResultID = sigResultID
            };

            return resultList.Contains(_connUSerpStr.EditWFSignature(para).Result) == false;
        }
        #endregion

        #region - 移至下一節點 -
        /// <summary>
        /// 移至下一節點
        /// </summary>
        /// <param name="erpNodeNum"></param>
        /// <param name="sigUerID"></param>
        private void NextToNode(string erpNodeNum, string sigUerID)
        {
            EntityWorkflow.NextToNodePara para = new EntityWorkflow.NextToNodePara
            {
                NewUserID = DBNull.Value.ToString(),
                WFNo = string.IsNullOrWhiteSpace(WFFlowData.WFNo) ? DBNull.Value.ToString() : WFFlowData.WFNo,
                UserID = string.IsNullOrWhiteSpace(sigUerID) ? DBNull.Value.ToString() : ConvertUserIDLength(sigUerID),
                UpdUserID = WFFlowData.UpdUserID,
                NodeUserParaList = new List<EntityWorkflow.NodeNewUserPara>
                {
                    new EntityWorkflow.NodeNewUserPara { NewUserID = string.IsNullOrWhiteSpace(erpNodeNum) ? ConvertUserIDLength(sigUerID) : GetProcessUserID(erpNodeNum) }
                }
            };

            _connUSerpStr.NextToProcessNode(para);
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

            EntityWorkflow.EditWFNodeProcessUserIDPara para = new EntityWorkflow.EditWFNodeProcessUserIDPara
            {
                WFNo = WFFlowData.WFNo,
                UserID = userID,
                UpdUserID = userID,
                NewUserID = userID
            };

            return _connUSerpStr.EditWFNodeProcessUserID(para).Result == "Success";
        }
        #endregion

        #region - 結束節點 -
        /// <summary>
        /// 結束節點
        /// </summary>
        private void EditToEndNode()
        {
            var userID = GetProcessUserID("F");
            EntityWorkflow.ToEndNodePara para = new EntityWorkflow.ToEndNodePara
            {
                WFNo = WFFlowData.WFNo,
                NodeNO = GetRunTimeWFFlow(),
                UserID = userID,
                UpdUserID = userID
            };

            _connUSerpStr.EditToEndNode(para);
        }
        #endregion

        #region - 取得結點簽核名單 -
        /// <summary>
        ///  取得結點簽核名單
        /// </summary>
        private List<EntityWorkflow.SetSigValue> GetNodeSigUserList()
        {
            var sigStep = 1;
            var addSigStep = 5;
            var onceAppearedList = new List<string>();
            bool isSignStep = new List<string> { null, "1", "2" }.Contains(ErpWFLogNodeInfo.SigCategory);

            var sigUserList = SignedUserList
                .Where(f => (isSignStep)
                    ? (Regex.IsMatch(f.rec94Fsts, @"[0-9]$") && int.Parse(f.rec94Fsts) < 5)
                    : (Regex.IsMatch(f.rec94Fsts, @"[0-9]$") && new List<string> { "6", "7" }.Contains(f.rec94Fsts))
                      || new List<string> { "B", "F" }.Contains(f.rec94Fsts)).ToList();

            var unitSigUser = sigUserList.Where(n => n.rec94Fsts == "2").Select(e => e.rec94Stfn).LastOrDefault();
            var processSigUser = sigUserList.Where(n => n.rec94Fsts == "4").Select(e => e.rec94Stfn).LastOrDefault();

            var result = sigUserList.Select(n =>
            {
                var sigSeq =
                    (Regex.IsMatch(n.rec94Fsts, @"[0-9]$"))
                        ? n.rec94Fsts.PadLeft(3, '0')
                        : new List<string> { "2", "4" }[new List<string> { "B", "F" }.IndexOf(n.rec94Fsts)].PadLeft(3, '0');
                var userID = n.rec94Stfn;

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

                return new EntityWorkflow.SetSigValue
                {
                    SigStep = sigStep++,
                    SigUserID = ConvertUserIDLength(userID),
                    WFSigSeq = sigSeq
                };
            }).Where(d => string.IsNullOrWhiteSpace(d.WFSigSeq) == false).ToList();

            return result.Any() ? result : new List<EntityWorkflow.SetSigValue>();
        }
        #endregion
    }
}
