using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;

namespace ERPWF
{
    internal class BatchModel : WorkflowModel
    {
        #region - Constructor -
        public BatchModel()
        {
            _connUSerpStr = new EntityBatch(ConfigurationManager.ConnectionStrings["USERPConnection"].ConnectionString);
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

        public List<EntityWorkflow.AddRemarkPara> AddRemarkParaList = new List<EntityWorkflow.AddRemarkPara>();
        public List<EntityWorkflow.NewWFFlowPara> NewWFFlowParaList = new List<EntityWorkflow.NewWFFlowPara>();
        public List<SignForm> AddSignFormParaList = new List<SignForm>();
        public List<EntityWorkflow.AddDocumentPara> AddDocumentParaList = new List<EntityWorkflow.AddDocumentPara>();
        public List<Rec94> SignedUserList { get; private set; }
        public List<LogInfo> LogInfoList { get; private set; }
        public List<SignForm> BatchSignFormList { get; set; }
        public List<SignForm> SignFormList { get; set; }
        public List<Rec94> Rec94List { get; set; }
        public List<LogRecm93> LogRecm93List { get; set; }

        #region - Private -
        protected readonly EntityBatch _connUSerpStr;
        private bool _forceEnd;
        private bool _firstTimeSig;
        #endregion

        public void BatchAddWFData()
        {
            List<EntityBatch.NewWFFlow> wFnoList = _connUSerpStr.EditNewWFFlow(NewWFFlowParaList);

            foreach (var wfno in wFnoList)
            {
                #region - 新增聯絡單 -
                AddSignFormParaList.Add(new SignForm
                {
                    SignFormNO = string.IsNullOrWhiteSpace(sign.SignFormNO) ? DBNull.Value.ToString() : sign.SignFormNO,
                    SignFormWFNO = wfno,
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
                #endregion
            }
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

             var batchCount = Convert.ToInt32(Math.Ceiling(SignFormList.Count / (double)1000));

            foreach (var index in Enumerable.Range(0, batchCount))
            {
                BatchSignFormList = SignFormList.Skip(index * 1000).Take(1000).ToList();

                foreach (var sign in BatchSignFormList)
                {
                    Console.WriteLine(executeNum++);

                    #region - 新增WF -
                    NewWFFlowParaList.Add(new EntityWorkflow.NewWFFlowPara
                    {
                        SysID = WFFlowData.SysID,
                        FlowID = WFFlowData.FlowID,
                        FlowVer = WFFlowData.FlowVer,
                        Subject = WFFlowData.Subject,
                        UserID = ConvertUserIDLength(sign.SignFormNewUserID),
                        SignFormNo = sign.SignFormNO
                    });
                    #endregion

                    Console.Clear();
                }

                BatchAddWFData();
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
                            #region - 新增WF -
                            NewWFFlowParaList.Add(new EntityWorkflow.NewWFFlowPara
                            {
                                SysID = WFFlowData.SysID,
                                FlowID = WFFlowData.FlowID,
                                FlowVer = WFFlowData.FlowVer,
                                Subject = WFFlowData.Subject,
                                UserID = ConvertUserIDLength(WFFlowData.NewUserID)
                            });
                            #endregion

                            #region - 新增聯絡單 -
                            AddSignFormParaList.Add(new SignForm
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
                            });
                            #endregion

                            //CheckFileAndUpload(sign.SignFormNewUserID, sign.Rec93Form.ToString());
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

        #region - 取得指定單號LOG資訊清單 -
        /// <summary>
        /// 取得指定單號LOG資訊清單
        /// </summary>
        /// <param name="signFromNo"></param>
        private void GetSpecificFormNOLogInfoList(string signFromNo)
        {
            SignedUserList = Rec94List.Where(r => r.rec94Form.ToString() == signFromNo)
                                      .OrderBy(s => s.rec94Fsts).ThenBy(s => s.rec94NO).ToList(); //SignedUserList

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

                        AddDocumentParaList.Add(new EntityWorkflow.AddDocumentPara
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
                        });
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

        #region - 增加註記 -
        /// <summary>
        /// 增加註記
        /// </summary>
        /// <param name="nodeNum"></param>
        /// <param name="userID"></param>
        /// <param name="nodeID"></param>
        private void AddRemark(string nodeNum, string userID, string nodeID)
        {
            AddRemarkParaList.Add(new EntityWorkflow.AddRemarkPara
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
            });
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
    }
}