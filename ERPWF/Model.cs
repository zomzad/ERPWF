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
    public class Model
    {
        #region - Definition -
        public enum EnumERPFilePath
        {
            [Description(@"http://uerp.liontravel.com.tw/html2/form")]
            PATH
        }

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

        public class ErpWFLogRow
        {
            /// <summary>
            /// 當前紀錄
            /// </summary>
            public DataRow Current { get; set; }

            /// <summary>
            /// 下一筆紀錄
            /// </summary>
            public DataRow Next { get; set; }

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
        public string ERPFormNo { get; set; }
        public WFFlow WFFlowData { get; set; }
        public ErpWFLogRow ErpWFLogRowData { get; set; }
        public List<SignForm> SignFormList { get; set; }
        #endregion

        #region - Private -
        private bool _firstTimeSig;
        private bool _forceEnd;
        private DataTable _erpSigLogData;
        private readonly Entity _connUSerpStr;
        private readonly Entity _connErpStr;
        private List<Entity.SetSigUserList> _setSigUserList { get; set; }
        #endregion

        #region - Constructor -
        public Model()
        {
            _connUSerpStr = new Entity(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString);
            _connErpStr = new Entity(ConfigurationManager.ConnectionStrings["UERPConnection"].ConnectionString);
            WFFlowData = new WFFlow
            {
                SysID = "PUBAP",
                FlowID = "SignForm",
                FlowVer = "001",
                Subject = "簽核單",
                UpdUserID = "APIService.ERP.WorkFlowService"
            };
        }
        #endregion

        #region - 取得SERP簽核單清單 -
        /// <summary>
        /// 取得SERP簽核單清單
        /// </summary>
        public void GetSerpSignFormList()
        {
            try
            {
                SignFormList = _connErpStr.GetSerpSignFormList();

                if (SignFormList.Any())
                {
                    EditSerpwfData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}:{WFFlowData.WFNo}/{ERPFormNo}");
                Console.Read();
            }
        }
        #endregion

        #region - 取得SERP停用使用者清單 -
        /// <summary>
        /// 取得SERP停用使用者清單
        /// </summary>
        /// <returns></returns>
        protected string GetSerpDisableUserList()
        {
            var disableUserList =
                _connUSerpStr.GetSerpDisableUserList().Select(r => $"'{r.UserID}'").ToList();

            return disableUserList.Any() ? string.Join(",", disableUserList) : string.Empty;
        }
        #endregion

        #region - 編輯SERP工作流程資料 -
        /// <summary>
        /// 編輯SERP工作流程資料
        /// </summary>
        protected void EditSerpwfData()
        {
            var executeNum = 1;

            foreach (var sign in SignFormList)
            {
                Console.WriteLine(executeNum++);
                ERPFormNo = sign.Rec93Form.ToString();

                if (GetERPSigUserList(ERPFormNo) &&
                    GetERPSigLog(ERPFormNo))
                {
                    switch (sign.FSTS)
                    {
                        case "F":
                            ConvertToEndWFForm(sign);
                            break;
                        default:
                            ConvertToWFForm(sign);
                            break;
                    }
                }

                Console.Clear();
            }
        }
        #endregion

        #region - 寫入錯誤聯絡單紀錄 -
        /// <summary>
        /// 寫入錯誤聯絡單紀錄
        /// </summary>
        /// <param name="logStr"></param>
        public void WriteErrorFormLog(string logStr)
        {
            string filePath = GetEnumDescription(EnumErrorFormLogFilePath.LOG_FILE_PATH);
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

        #region - 結案聯絡單資料表轉換 -
        /// <summary>
        /// 結案聯絡單資料表轉換
        /// </summary>
        /// <param name="sign"></param>
        protected void ConvertToEndWFForm(SignForm sign)
        {
            for (var i = 0; i < _erpSigLogData.Rows.Count; i++)
            {
                SetErpWFLogRowData(_erpSigLogData, i);

                var user = string.IsNullOrWhiteSpace(ErpWFLogRowData.Current.Field<string>("stfn_stfn"))
                    ? ConvertUserIDLength(ErpWFLogRowData.Current.Field<string>("rec93_stfn"))
                    : ConvertUserIDLength(ErpWFLogRowData.Current.Field<string>("stfn_stfn"));

                switch (ErpWFLogRowData.Current.Field<string>("lrec93_fsts"))
                {
                    case null:
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                        if (ErpWFLogRowData.Current.Field<string>("lrec93_fsts") == null &&
                            (ErpWFLogRowData.Desc == GetEnumDescription(EnumLogDescType.APPLY) || ErpWFLogRowData.Desc == GetEnumDescription(EnumLogDescType.NEWFORM)))
                        {
                            if (EditNewWFFlow(sign) == false)
                            {
                                i = _erpSigLogData.Rows.Count;
                                _forceEnd = true;
                                break;
                            }

                            AddSignForm(sign);
                            CheckFileAndUpload(sign.SignFormNewUserID, ERPFormNo);
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
                GetERPSigUserList(ERPFormNo);
                EditWFENDFlow(GetProcessUserID("F"));
            }

            _forceEnd = false;
        }
        #endregion

        #region - 聯絡單資料表轉換 -
        /// <summary>
        /// 聯絡單資料表轉換
        /// </summary>
        /// <param name="sign"></param>
        public void ConvertToWFForm(SignForm sign)
        {
            for (var i = 0; i < _erpSigLogData.Rows.Count; i++)
            {
                SetErpWFLogRowData(_erpSigLogData, i);

                switch (ErpWFLogRowData.SigCategory)
                {
                    case null:
                        if (ErpWFLogRowData.Desc == GetEnumDescription(EnumLogDescType.APPLY) ||
                            ErpWFLogRowData.Desc == GetEnumDescription(EnumLogDescType.NEWFORM))
                        {
                            //申請或立單
                            if (EditNewWFFlow(sign) == false)
                            {
                                _forceEnd = true;
                                break;
                            }

                            AddSignForm(sign);
                            CheckFileAndUpload(sign.SignFormNewUserID, ERPFormNo);
                        }
                        else
                        {
                            //修改簽核名單且為紀錄最後一筆
                            if (ErpWFLogRowData.Desc == GetEnumDescription(EnumLogDescType.MODIFYSigList) &&
                                _erpSigLogData.Rows.IndexOf(ErpWFLogRowData.Current) + 1 == _erpSigLogData.Rows.Count)
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
                                var user = string.IsNullOrWhiteSpace(ErpWFLogRowData.SigUserID)
                                    ? ConvertUserIDLength(WFFlowData.NewUserID) 
                                    : ConvertUserIDLength(ErpWFLogRowData.SigUserID);
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
                            (ErpWFLogRowData.Desc.Contains(GetEnumDescription(EnumLogDescType.PASS))
                             || ErpWFLogRowData.Desc.Contains(GetEnumDescription(EnumLogDescType.COMPLETE)))
                                ? EnumSigResultID.A.ToString()
                                : (ErpWFLogRowData.Desc.Contains(GetEnumDescription(EnumLogDescType.BACK)))
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
                        if (ErpWFLogRowData.SigCategory != "F" &&
                            (ErpWFLogRowData.Next.ItemArray.Any() == false || (ErpWFLogRowData.SigCategory == ErpWFLogRowData.Next.Field<string>("lrec93_fsts"))))
                        {
                            AddWFRemark(ErpWFLogRowData.SigCategory);
                        }
                        else
                        {
                            string sigUserID;

                            if (string.IsNullOrWhiteSpace(ErpWFLogRowData.SigUserID))
                            {
                                sigUserID = GetSigRemoveUserID(sign, ErpWFLogRowData.Current.Field<string>("lrec93_mstfn"));
                                if (string.IsNullOrWhiteSpace(sigUserID))
                                {
                                    _forceEnd = true;
                                    break;
                                }
                            }
                            else
                            {
                                sigUserID = ErpWFLogRowData.SigUserID;
                            }

                            if (EditWFSignature(sigResultID, sigUserID))
                            {
                                if (sigResultID.Equals(EnumSigResultID.A.ToString()) &&
                                    (ErpWFLogRowData.Current.Field<string>("lrec93_fsts").Equals("4")
                                     || ErpWFLogRowData.Current.Field<string>("lrec93_fsts").Equals("5")
                                     || ErpWFLogRowData.Current.Field<string>("lrec93_fsts").Equals("F")))
                                {
                                    if (ErpWFLogRowData.Current.Field<string>("lrec93_fsts").Equals("F"))
                                    {
                                        EditToEndNode();
                                    }
                                    else
                                    {
                                        var erpNodeNum = (ErpWFLogRowData.Current.Field<string>("lrec93_fsts").Equals("F")) ? string.Empty : ErpWFLogRowData.Next.Field<string>("lrec93_fsts");
                                        NextToNode(erpNodeNum, ErpWFLogRowData.SigUserID);
                                    }
                                }
                            }
                        }
                        break;

                    case "5":
                    case "6":
                    case "7":
                    case "A":
                        if (ErpWFLogRowData.Current.Field<string>("lrec93_fsts").Equals("A") &&
                            (ErpWFLogRowData.Next.ItemArray.Any() && (ErpWFLogRowData.Current.Field<string>("lrec93_fsts") != ErpWFLogRowData.Next.Field<string>("lrec93_fsts"))))
                        {//簽核身分A & 當前和下一筆紀錄簽核關卡不同
                            NextToNode(ErpWFLogRowData.Next.Field<string>("lrec93_fsts"), ErpWFLogRowData.SigUserID);

                            if (EditWFNodeProcessUserID(ErpWFLogRowData.Current.Field<string>("lrec93_fsts")))
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
                            AddWFRemark(ErpWFLogRowData.Current.Field<string>("lrec93_fsts"));
                        }
                        break;
                }

                if (_forceEnd)
                {
                    break;
                }
            }

            _forceEnd = false;
            _firstTimeSig = false;
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
                Entity.WFFilePara para = new Entity.WFFilePara
                {
                    WFNo = formNO
                };
                var wfFileList = _connErpStr.CheckWFFile(para);

                foreach (var row in wfFileList)
                {
                    long contentLength;
                    string fileNM = row.FilePath.Split(new[] { "/17/" }, StringSplitOptions.RemoveEmptyEntries).Last();
                    string erpFilePath = $"{GetEnumDescription(EnumERPFilePath.PATH)}{row.FilePath}";
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

                        Entity.AddDocumentPara docPara = new Entity.AddDocumentPara
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

        #region - 取得ERP簽核單LOG檔 -
        /// <summary>
        /// 取得ERP簽核單LOG檔
        /// </summary>
        /// <param name="formNo"></param>
        /// <returns></returns>
        protected bool GetERPSigLog(string formNo)
        {
            try
            {
                Entity.ERPSigLogPara para = new Entity.ERPSigLogPara
                {
                    Rec93Form = formNo
                };

                _erpSigLogData = _connErpStr.GetERPSigLog(para);

                return true;
            }
            catch (Exception ex)
            {
                WriteErrorFormLog($"取得ERP簽核單LOG檔失敗:{formNo} / {ex.Message}{Environment.NewLine}");
            }

            return false;
        }
        #endregion

        #region - 設定ERP簽核紀錄節點資訊 -
        /// <summary>
        /// 設定ERP簽核紀錄節點資訊
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="idx"></param>
        protected void SetErpWFLogRowData(DataTable dataTable, int idx)
        {
            ErpWFLogRowData = new ErpWFLogRow
            {
                Current = dataTable.Rows[idx],
                Next = (dataTable.Rows.Count == idx + 1) ? new DataTable().NewRow() : dataTable.Rows[idx + 1],
                Desc = dataTable.Rows[idx].Field<string>("lrec93_desc"),
                SigUserID = ConvertUserIDLength(dataTable.Rows[idx].Field<string>("stfn_stfn")),
                SigCategory = dataTable.Rows[idx].Field<string>("lrec93_fsts")
            };

            WFFlowData.NewUserID = dataTable.Rows[idx].Field<string>("rec93_stfn");
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
                Entity.NewWFFlowPara para = new Entity.NewWFFlowPara
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

        #region - 取得ERP簽核名單 -
        /// <summary>
        /// 取得ERP簽核名單
        /// </summary>
        /// <param name="rec93Form"></param>
        /// <returns></returns>
        protected bool GetERPSigUserList(string rec93Form)
        {
            try
            {
                Entity.SetSigUserListPara para = new Entity.SetSigUserListPara
                {
                    Rec94Form = rec93Form
                };

                _setSigUserList = _connErpStr.GetSetSigUserList(para);

                return _setSigUserList != null && _setSigUserList.Any();
            }
            catch (Exception ex)
            {
                WriteErrorFormLog($"取得ERP簽核名單失敗:{rec93Form} / {ex.Message}{Environment.NewLine}");
            }

            return false;
        }
        #endregion

        #region - 取得結點簽核名單 -
        /// <summary>
        ///  取得結點簽核名單
        /// </summary>
        private List<Entity.SetSigValue> GetNodeSigUserList()
        {
            var sigStep = 1;
            var addSigStep = 5;
            var onceAppearedList = new List<string>();
            bool isSignStep = new List<string> { null, "1", "2" }.Contains(ErpWFLogRowData.SigCategory);

            var sigUserList = _setSigUserList
                .Where(f => (isSignStep)
                    ? (Regex.IsMatch(f.rec94_fsts, @"[0-9]$") && int.Parse(f.rec94_fsts) < 5)
                    : (Regex.IsMatch(f.rec94_fsts, @"[0-9]$") && new List<string> { "6", "7" }.Contains(f.rec94_fsts))
                      || new List<string> { "B", "F" }.Contains(f.rec94_fsts)).ToList();

            var tt = sigUserList.GroupBy(c => new { c.rec94_fsts, c.rec94_stfn });

            var unitSigUser = sigUserList.Where(n => n.rec94_fsts == "2").Select(e => e.rec94_stfn).LastOrDefault();
            var processSigUser = sigUserList.Where(n => n.rec94_fsts == "4").Select(e => e.rec94_stfn).LastOrDefault();

            var result = sigUserList.Select(n =>
            {
                var sigSeq =
                    (Regex.IsMatch(n.rec94_fsts, @"[0-9]$"))
                        ? n.rec94_fsts.PadLeft(3, '0')
                        : new List<string> { "2", "4" }[new List<string> { "B", "F" }.IndexOf(n.rec94_fsts)].PadLeft(3, '0');
                var userID = n.rec94_stfn;

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
                        if (onceAppearedList.Contains(sigSeq))
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
                        if (onceAppearedList.Contains(sigSeq))
                        {
                            sigSeq = Convert.ToString(addSigStep++).PadLeft(3, '0');
                        }
                        onceAppearedList.Add(sigSeq);
                        break;
                }

                return new Entity.SetSigValue
                {
                    SigStep = sigStep++,
                    SigUserID = ConvertUserIDLength(userID),
                    WFSigSeq = sigSeq
                };
            }).Where(d => string.IsNullOrWhiteSpace(d.WFSigSeq) == false).ToList();

            return result.Any() ? result : new List<Entity.SetSigValue>();
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
                    Entity.SetWFSignaturePara para = new Entity.SetWFSignaturePara
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

        #region - 簽核 -
        /// <summary>
        /// 簽核
        /// </summary>
        /// <param name="sigResultID"></param>
        /// <param name="sigUserID"></param>
        private bool EditWFSignature(string sigResultID, string sigUserID)
        {
            var resultList = new List<string> { "NotProcessNode", "NotSignUser" };
            Entity.WFSignaturePara para = new Entity.WFSignaturePara
            {
                WFNo = WFFlowData.WFNo,
                NodeNO = GetRunTimeWFFlow(),
                UserID = ConvertUserIDLength(sigUserID),
                SigResultID = sigResultID
            };

            return resultList.Contains(_connUSerpStr.EditWFSignature(para).Result) == false;
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
            Entity.SigRemoveUserIDPara para = new Entity.SigRemoveUserIDPara
            {
                UserNM = userNM
            };

            var userInfo = _connErpStr.GetSigRemoveUserID(para);

            if (userInfo.Any())
            {
                return userInfo.First().STFN;
            }

            WriteErrorFormLog($"單號:{sign.Rec93Form} / {sign.SignFormNewUserID} - 無此人{Environment.NewLine}");
            WriteErrorUserLog($"{sign.SignFormNewUserID}{Environment.NewLine}");
            return string.Empty;
        }
        #endregion

        #region - 取得目前節點 -
        /// <summary>
        /// 取得目前節點
        /// </summary>
        /// <returns></returns>
        private string GetRunTimeWFFlow()
        {
            Entity.RunTimeWfFlowPara para = new Entity.RunTimeWfFlowPara
            {
                WFNo = WFFlowData.WFNo
            };

            var nodeNo = _connUSerpStr.GetRunTimeWFFlow(para);

            return nodeNo;
        }
        #endregion

        #region - 取得節點處理人 -
        /// <summary>
        /// 取得節點處理人
        /// </summary>
        /// <param name="erpNodeNum"></param>
        private string GetProcessUserID(string erpNodeNum)
        {
            string processUserID = _setSigUserList
                .Where(f => f.rec94_fsts == erpNodeNum)
                .Select(n => n.rec94_stfn).First();

            return ConvertUserIDLength(processUserID);
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
            Entity.NextToNodePara para = new Entity.NextToNodePara
            {
                NewUserID = DBNull.Value.ToString(),
                WFNo = string.IsNullOrWhiteSpace(WFFlowData.WFNo) ? DBNull.Value.ToString() : WFFlowData.WFNo,
                UserID = string.IsNullOrWhiteSpace(sigUerID) ? DBNull.Value.ToString() : ConvertUserIDLength(sigUerID),
                UpdUserID = WFFlowData.UpdUserID,
                NodeUserParaList = new List<Entity.NodeNewUserPara>
                {
                    new Entity.NodeNewUserPara { NewUserID = string.IsNullOrWhiteSpace(erpNodeNum) ? ConvertUserIDLength(sigUerID) : GetProcessUserID(erpNodeNum) }
                }
            };

            _connUSerpStr.NextToProcessNode(para);
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

        #region - 成為節點處理人 -
        /// <summary>
        /// 成為節點處理人
        /// </summary>
        /// <param name="fsts"></param>
        /// <returns></returns>
        private bool EditWFNodeProcessUserID(string fsts)
        {
            var userID = GetProcessUserID(fsts);

            Entity.EditWFNodeProcessUserIDPara para = new Entity.EditWFNodeProcessUserIDPara
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
            Entity.ToEndNodePara para = new Entity.ToEndNodePara
            {
                WFNo = WFFlowData.WFNo,
                NodeNO = GetRunTimeWFFlow(),
                UserID = userID,
                UpdUserID = userID
            };

            _connUSerpStr.EditToEndNode(para);
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
            Entity.AddRemarkPara para = new Entity.AddRemarkPara
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

        #region - 增加WF註記 -
        /// <summary>
        /// 增加WF註記
        /// </summary>
        /// <param name="erpNodeNum"></param>
        private void AddWFRemark(string erpNodeNum)
        {
            var userID = GetProcessUserID(erpNodeNum);

            Entity.AddWFRemarkPara para = new Entity.AddWFRemarkPara
            {
                WFNo = WFFlowData.WFNo,
                UserID = userID,
                UpdUserID = userID,
                Remark = string.IsNullOrWhiteSpace(ErpWFLogRowData.Desc) ? DBNull.Value.ToString() : ErpWFLogRowData.Desc
            };

            _connUSerpStr.AddWFRemark(para);
        }
        #endregion

        #region - 員工編號4碼轉6碼 -
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

        #region - 結案單_結束節點 -
        /// <summary>
        /// 結案單_結束節點
        /// </summary>
        /// <param name="userID"></param>
        public void EditWFENDFlow(string userID)
        {
            Entity.WFENDFlowPara para = new Entity.WFENDFlowPara
            {
                WFNo = WFFlowData.WFNo,
                UserID = userID,
                UpdUserID = userID
            };

            _connUSerpStr.EditWFENDFlow(para);
        }
        #endregion
    }
}
