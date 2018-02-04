--SELECT * FROM WF_FLOW
-- WHERE WF_NO = '20170000000163'
--SELECT * FROM WF_NODE
-- WHERE WF_NO = '20170000000163' 
--SELECT * FROM WF_SIG
--WHERE WF_NO = '20170000000163'
--SELECT * FROM WF_REMARK
--WHERE WF_NO = '20170000000163'

--20170000000156 結單 
--20170000000158 驗收節點退回處理節點
--20170000000161 驗收節點退回處理節點
--20170000000162 設定完簽核名單，未審核
--20170000000163 設定完簽核名單，第一關退回
--NEXT TO NODE不會寫紀錄到WF_REMARK

--單位相關
SELECT TOP 50 prof_dname,* FROM ispfm00
WHERE prof_dname = '資訊'

--recm93比較接近WF_FLOW
SELECT * FROM recm93
 WHERE rec93_form = '17100015'

--這張對應WF_REMARK
SELECT * FROM logrecm93
 WHERE lrec93_form = '17100015'

--整個流程的名單 對應WF_SIG
SELECT * FROM recm94
 WHERE rec94_form = '17100015'
 ORDER BY rec94_fsts
--17100015 修改簽核名單&簽核通過退回
--17100003 結單
--17110001 一直修改簽核名單

--算是設定檔
SELECT * FROM recm97 --有各種單個關卡 人員SQL條件 可以設定API 條件寫在API就好?
SELECT * FROM recm99
SELECT * FROM recm99a

--與聯絡單同畫面
--2-聯絡單
--10-法務處理申請單
--11-EC處理網站申請單
--14-SOP文件上架
--16-網路客服授權
--17-客戶資料調閱與分析
--18-資安處理申請單
SELECT * FROM recm99
WHERE rec99_aspid = 'recm96_a'

SELECT * FROM recm99
WHERE rec99_name = 'recm93_name'

--配合asp_id查導頁的系統別mm0_sys1
SELECT TOP 50 * FROM mism00

--有上傳檔案的聯絡單
 SELECT rec95_file,* FROM recm95
   JOIN recm93
     ON rec93_form = rec95_form
	AND rec93_formno = '2' 


--//////////////////////////////////////////////////////////////////////////////
--聯絡單內容轉到ZD223_SIGN_FORM
--轉過來要配一個WF_NO給它
--WF_NO = 20 + 原單號前兩碼 + (補0到10碼 + 原單號後6碼)
SELECT TOP 50
       ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2) 
	       + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8)) AS 單號
     , ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2) 
	         + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8)) AS 簽核單號
     , CM93.rec93_sts AS 是否作廢
	 , CM93.rec93_title AS 主旨
	 , CM96A.r96a_data1 AS 事由原因
	 , CM96A.r96a_data2 AS 申請處理
	 , CM96A.r96a_char1 AS 訂單年份
	 , CM96A.r96a_char2 AS 訂單編號
     , CM96A.r96a_int1 AS 項目
     , CM96A.r96a_int2 AS 主要ERP作業
	 , CM93.rec93_needlion AS BU主要事業體
	 , CM96A.r96a_char4 AS 同行公司
	 , CM96A.r96a_char3 AS 使用者
	 , CM93.rec93_stfn AS 簽核單申請人
	 , CONVERT(datetime,CM93.rec93_date) AS 申請日期--rec93_date CHAR(8) > DATETIME型態不同
	 , CM93.rec93_mstfn AS 更新人員 --長度&型態不同
	 , CM93.rec93_mdate AS 更新時間
	 , CM93.rec93_fsts AS 審核流程
  FROM recm93 CM93
  JOIN recm96a CM96A
    ON CM96A.r96a_form = CM93.rec93_form
  JOIN opagm20 GM20
    ON CM93.rec93_stfn = GM20.stfn_stfn
 WHERE CM93.rec93_formno = '2'
   AND CM93.rec93_fsts = 'F'
 ORDER BY 單號 DESC

--/////////////////////////////////簽核紀錄//////////////////////////////////////////
BEGIN TRAN
--////////////////////INSERT INTO WF_FLOW////////////////////////////////////////////
SELECT ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2) 
	         + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8))
  FROM recm93 CM93


--////////////////////INSERT INTO WF_SIG///////////////////////////////////////////////////////////////
--rec94_fsts = 1 為 ApplySignForm節點主管審核簽核人，有多個的話寫入加簽名單，但順位要在前面
--rec94_fsts = 3 為會簽，對應ApplySignForm節點的加簽人
--rec94_fsts = 2 OR 4 為 ApplySignForm節點002和004的簽核人
--rec94_fsts的順序 對應 WF_SIG的SIG_STEP
--主管審核順位會在第一個(可有好幾個)
--會簽順位在單位主管和處理單位主管之間(可有好幾個)
--還未開始簽核時候，會簽跟主管不能改名單，兩位必填可以改，單位主管簽完 處理單位主官還是能改剩下名單
--簽核過程中，若被退回，申請者可以改簽核名單
--////////////////////////////////////////////////////////////////////////////////////////////////////
DECLARE @UNIT_BOSS_STFN CHAR(6) = NULL;
DECLARE @PROCESS_BOSS_STFN CHAR(6) = NULL;
DECLARE @WILL_SIGN_NUM INT = NULL;
DECLARE @WF_SYS_ID VARCHAR(12) = 'PUBAP';
DECLARE @WF_FLOW_ID VARCHAR(50) = 'SignForm';
DECLARE @WF_FLOW_VER CHAR(3) = '001'
--WITH #INFO AS (
SELECT @UNIT_BOSS_STFN = 
           CASE WHEN @UNIT_BOSS_STFN IS NOT NULL
		   THEN @UNIT_BOSS_STFN
		   WHEN rec94_fsts = 2 THEN rec94_stfn END --單位主管
     , @PROCESS_BOSS_STFN = 
	       CASE WHEN @PROCESS_BOSS_STFN IS NOT NULL
		   THEN @PROCESS_BOSS_STFN 
		   WHEN rec94_fsts = 4 THEN rec94_stfn END --處理單位主管
  FROM recm94 R
 WHERE R.rec94_form = '17110001'
   AND R.rec94_fsts < '5'
SELECT @UNIT_BOSS_STFN;
SELECT @PROCESS_BOSS_STFN;

SELECT @WILL_SIGN_NUM = COUNT(rec94_stfn)
            FROM recm94
		   WHERE rec94_fsts = '3'
		     AND rec94_form = '17110001';
SELECT @WILL_SIGN_NUM;

DECLARE @START VARCHAR(200) = '申請'
DECLARE @BACK VARCHAR(500) = '退回!';
DECLARE @PASS VARCHAR(500) = '核准!';
DECLARE @PAUSE VARCHAR(500) = '暫不核!';
DECLARE @APPLY_USER VARCHAR(10) = NULL;

--WF_NODE
SELECT @APPLY_USER = rec93_stfn
  FROM recm93
 WHERE rec93_form = '17100003'
 SELECT @APPLY_USER

SELECT '20170000100003' AS WF_NO
     , IDENTITY(INT,1,1) AS NODE_NO
	 , @WF_SYS_ID AS SYS_ID
	 , @WF_FLOW_ID AS WF_FLOW_ID
	 , @WF_FLOW_VER AS WF_FLOW_VER
	 , CASE WHEN CM94.rec94_fsts < '5' THEN 'ApplySignForm'
	        WHEN CM94.rec94_fsts = '5' THEN 'ProcessSignForm'
			WHEN CM94.rec94_fsts > '5' THEN 'AcceptSignForm' END AS WF_NODE_ID
	 , CASE WHEN CM94.rec94_stfn IS NULL THEN @APPLY_USER ELSE CM94.rec94_stfn END AS NEW_USER_ID
  INTO #WF_NODE_INFO
  FROM logrecm93 LOG93
  JOIN recm93 CM93
    ON CM93.rec93_form = LOG93.lrec93_form
  JOIN recm94 CM94
    ON CM94.rec94_form = CM93.rec93_form
   AND CM94.rec94_fsts = LOG93.lrec93_fsts
 WHERE lrec93_form = '17100003'
SELECT * FROM #WF_NODE_INFO;
DROP TABLE #WF_NODE_INFO;






