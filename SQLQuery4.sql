SELECT * FROM opagm20
WHERE stfn_stfn IN ('00D470','002578','008877','008382','00F148','ZZZ077')

SELECT * FROM recm93 CM93
  JOIN recm96a R96A
     ON R96A.r96a_form = CM93.rec93_form 
WHERE rec93_form = '17110007'

SELECT * FROM recm99a
SELECT * FROM recm99

SELECT * FROM recm94
 WHERE rec94_form = '17110005' 

SELECT * FROM recm93
WHERE rec93_form = '17100003'

SELECT  * FROM logrecm93
 WHERE lrec93_form = '17110005' 

SELECT * FROM recm95
WHERE rec95_form = '17110007'

SELECT stfn_pswd AS USER_PWD 
FROM opagm20 
WHERE stfn_stfn='00D223' 

SELECT TOP 5 msg_message,msg_prod,msg_ordr,msg_sys
FROM MESSAGE NOLOCK
WHERE msg_stfn='00D223' and msg_sts='0' 
AND (msg_hdate <= CONVERT(VARCHAR(8),GETDATE(),112) OR msg_hdate IS NULL )
AND msg_date < GETDATE() ORDER BY msg_date DESC

SELECT TOP 20 * FROM MESSAGE

--單位相關
SELECT TOP 50 prof_dname,* FROM ispfm00
WHERE prof_dname = '資訊'

--recm93比較接近WF_FLOW
SELECT *,CM96A.r96a_int1 AS 項目
     , CM96A.r96a_int2 AS 主要ERP作業
	 , CM93.rec93_needlion AS BU主要事業體
     , CM96A.r96a_char1 AS 訂單年分
	 , CM96A.r96a_char2 AS 訂單編號
	 , CM96A.r96a_char3 AS 使用者
	 , CM96A.r96a_char4 AS 同行公司
	 , CM93.rec93_title AS 主旨
	 , CM96A.r96a_data1 AS 事由原因
	 , CM96A.r96a_data2 AS 申請處理
	 , CM93.rec93_prof AS 申請人單位 --另外抓
	 , PFM00.prof_dname AS 單位名 --另外抓
  FROM recm93 CM93
  JOIN recm96a CM96A
    ON CM96A.r96a_form = CM93.rec93_form
  JOIN ispfm00 PFM00
    ON PFM00.prof_prof = CM93.rec93_prof
 WHERE CM93.rec93_form = '17110007'

 --這張對應WF_REMARK
 SELECT * FROM logrecm93
 WHERE lrec93_form = '17110001'

 --整個流程的名單 對應WF_SIG
 SELECT * FROM recm94
 WHERE rec94_form = '17110001'


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

SELECT TOP 50 * FROM mism00

SELECT * FROM opagm20
 WHERE stfn_right1='1'
   AND stfn_sts = '0' 
   AND stfn_prof = 'T1'


DECLARE @START VARCHAR(200) = '申請'
DECLARE @BACK VARCHAR(500) = '退回!';
DECLARE @PASS VARCHAR(500) = '核准!';
DECLARE @PAUSE VARCHAR(500) = '暫不核!';
DECLARE @APPLY_USER VARCHAR(10) = NULL;
DECLARE @WF_SYS_ID VARCHAR(12) = 'PUBAP';
DECLARE @WF_FLOW_ID VARCHAR(50) = 'SignForm';
DECLARE @WF_FLOW_VER CHAR(3) = '001'

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


SELECT * FROM logrecm93
WHERE lrec93_form = '17100015'
SELECT * FROM recm93
WHERE rec93_form = '17100015'


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


SELECT * FROM logrecm93
WHERE lrec93_form = '17100015'
 SELECT TOP 1 * FROM logrecm93
 WHERE lrec93_form = '17100015' AND lrec93_fsts IS NOT NULL
  

