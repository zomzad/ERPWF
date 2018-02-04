using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace ERPWF
{
    internal class ModelTEST
    {
        #region - Definitions -
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

        public class ErpWFLogRow
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
        public List<Rec94> Rec94List { get; private set; }
        public List<Rec94> SignedUserList { get; private set; }
        
        public List<LogRecm93> LogRecm93List { get; private set; }
        public List<SignForm> SignFormList { get; private set; }
        public ErpWFLogRow ErpWFLogRowData { get; set; }
        public WFFlow WFFlowData { get; set; }
        #endregion

        #region - Private -
        private readonly EntityTEST _connUSerpStr;
        private readonly EntityTEST _entityERP;
        private bool _forceEnd;
        #endregion

        public ModelTEST()
        {
            _connUSerpStr = new EntityTEST(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString);
            _entityERP = new EntityTEST(ConfigurationManager.ConnectionStrings["UERPConnection"].ConnectionString);

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
            DataSet signInfoDS = _entityERP.GetSerpSignFormInfoList();

            SignFormList = signInfoDS.Tables[0].ToList<SignForm>().ToList();
            Rec94List = signInfoDS.Tables[1].ToList<Rec94>().ToList();
            var rec93List = signInfoDS.Tables[2].ToList<recm93>();
            var logRecm93List = signInfoDS.Tables[3].ToList<LogRecm93>();

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

                        break;
                }

                Console.Clear();
            }
        }
        #endregion

        protected void ConvertToEndWFForm(SignForm sign)
        {
            GetLogInfoList(sign.Rec93Form.ToString()); //取得LOG紀錄 LogInfoList

            foreach (var log in LogInfoList.Select((value, index) => new { Value = value, Index = index }))
            {
                SetErpWFLogRowData(log.Value);

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
                            (ErpWFLogRowData.Desc == GetEnumDescription(EnumLogDescType.APPLY) || ErpWFLogRowData.Desc == GetEnumDescription(EnumLogDescType.NEWFORM)))
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

        private void GetLogInfoList(string signFromNo)
        {
            SignedUserList = Rec94List.Where(r => r.rec94Form.ToString() == signFromNo).ToList();
            var logRecm93List = LogRecm93List.Where(r => r.lrec93_form.ToString() == signFromNo);

            LogInfoList = (from data in logRecm93List
                           let hasUser = SignedUserList.Any(sign => data.lrec93_mstfn.Contains(sign.stfnCname))
                           select new LogInfo
                           {
                               Applicant = data.rec93_stfn,
                               SignedUser = hasUser ? SignedUserList.First(sign => data.lrec93_mstfn.Contains(sign.stfnCname)).rec94Stfn :string.Empty,
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

        #region - 設定ERP簽核紀錄節點資訊 -
        /// <summary>
        /// 設定ERP簽核紀錄節點資訊
        /// </summary>
        /// <param name="log"></param>
        protected void SetErpWFLogRowData(LogInfo log)
        {
            ErpWFLogRowData = new ErpWFLogRow
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
        private string ConvertUserIDLength(string userID)
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
            EntityTEST.AddRemarkPara para = new EntityTEST.AddRemarkPara
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
                Remark = string.IsNullOrWhiteSpace(ErpWFLogRowData.Desc) ? DBNull.Value.ToString() : ErpWFLogRowData.Desc
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
        private string GetEnumDescription(Enum value)
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
                EntityTEST.NewWFFlowPara para = new EntityTEST.NewWFFlowPara
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
            EntityTEST.WFENDFlowPara para = new EntityTEST.WFENDFlowPara
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
                EntityTEST.WFFilePara para = new EntityTEST.WFFilePara
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

                        EntityTEST.AddDocumentPara docPara = new EntityTEST.AddDocumentPara
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
    }
}
