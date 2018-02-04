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

--������
SELECT TOP 50 prof_dname,* FROM ispfm00
WHERE prof_dname = '��T'

--recm93�������WF_FLOW
SELECT *,CM96A.r96a_int1 AS ����
     , CM96A.r96a_int2 AS �D�nERP�@�~
	 , CM93.rec93_needlion AS BU�D�n�Ʒ~��
     , CM96A.r96a_char1 AS �q��~��
	 , CM96A.r96a_char2 AS �q��s��
	 , CM96A.r96a_char3 AS �ϥΪ�
	 , CM96A.r96a_char4 AS �P�椽�q
	 , CM93.rec93_title AS �D��
	 , CM96A.r96a_data1 AS �ƥѭ�]
	 , CM96A.r96a_data2 AS �ӽгB�z
	 , CM93.rec93_prof AS �ӽФH��� --�t�~��
	 , PFM00.prof_dname AS ���W --�t�~��
  FROM recm93 CM93
  JOIN recm96a CM96A
    ON CM96A.r96a_form = CM93.rec93_form
  JOIN ispfm00 PFM00
    ON PFM00.prof_prof = CM93.rec93_prof
 WHERE CM93.rec93_form = '17110007'

 --�o�i����WF_REMARK
 SELECT * FROM logrecm93
 WHERE lrec93_form = '17110001'

 --��Ӭy�{���W�� ����WF_SIG
 SELECT * FROM recm94
 WHERE rec94_form = '17110001'


--��O�]�w��
SELECT * FROM recm97 --���U�س�����d �H��SQL���� �i�H�]�wAPI ����g�bAPI�N�n?
SELECT * FROM recm99
SELECT * FROM recm99a

--�P�p����P�e��
--2-�p����
--10-�k�ȳB�z�ӽг�
--11-EC�B�z�����ӽг�
--14-SOP���W�[
--16-�����ȪA���v
--17-�Ȥ��ƽվ\�P���R
--18-��w�B�z�ӽг�
SELECT * FROM recm99
WHERE rec99_aspid = 'recm96_a'

SELECT * FROM recm99
WHERE rec99_name = 'recm93_name'

SELECT TOP 50 * FROM mism00

SELECT * FROM opagm20
 WHERE stfn_right1='1'
   AND stfn_sts = '0' 
   AND stfn_prof = 'T1'


DECLARE @START VARCHAR(200) = '�ӽ�'
DECLARE @BACK VARCHAR(500) = '�h�^!';
DECLARE @PASS VARCHAR(500) = '�֭�!';
DECLARE @PAUSE VARCHAR(500) = '�Ȥ���!';
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
  

