--SELECT * FROM WF_FLOW
-- WHERE WF_NO = '20170000000163'
--SELECT * FROM WF_NODE
-- WHERE WF_NO = '20170000000163' 
--SELECT * FROM WF_SIG
--WHERE WF_NO = '20170000000163'
--SELECT * FROM WF_REMARK
--WHERE WF_NO = '20170000000163'

--20170000000156 ���� 
--20170000000158 �禬�`�I�h�^�B�z�`�I
--20170000000161 �禬�`�I�h�^�B�z�`�I
--20170000000162 �]�w��ñ�֦W��A���f��
--20170000000163 �]�w��ñ�֦W��A�Ĥ@���h�^
--NEXT TO NODE���|�g������WF_REMARK

--������
SELECT TOP 50 prof_dname,* FROM ispfm00
WHERE prof_dname = '��T'

--recm93�������WF_FLOW
SELECT * FROM recm93
 WHERE rec93_form = '17100015'

--�o�i����WF_REMARK
SELECT * FROM logrecm93
 WHERE lrec93_form = '17100015'

--��Ӭy�{���W�� ����WF_SIG
SELECT * FROM recm94
 WHERE rec94_form = '17100015'
 ORDER BY rec94_fsts
--17100015 �ק�ñ�֦W��&ñ�ֳq�L�h�^
--17100003 ����
--17110001 �@���ק�ñ�֦W��

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

--�t�Xasp_id�d�ɭ����t�ΧOmm0_sys1
SELECT TOP 50 * FROM mism00

--���W���ɮת��p����
 SELECT rec95_file,* FROM recm95
   JOIN recm93
     ON rec93_form = rec95_form
	AND rec93_formno = '2' 


--//////////////////////////////////////////////////////////////////////////////
--�p���椺�e���ZD223_SIGN_FORM
--��L�ӭn�t�@��WF_NO����
--WF_NO = 20 + ��渹�e��X + (��0��10�X + ��渹��6�X)
SELECT TOP 50
       ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2) 
	       + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8)) AS �渹
     , ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2) 
	         + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8)) AS ñ�ֳ渹
     , CM93.rec93_sts AS �O�_�@�o
	 , CM93.rec93_title AS �D��
	 , CM96A.r96a_data1 AS �ƥѭ�]
	 , CM96A.r96a_data2 AS �ӽгB�z
	 , CM96A.r96a_char1 AS �q��~��
	 , CM96A.r96a_char2 AS �q��s��
     , CM96A.r96a_int1 AS ����
     , CM96A.r96a_int2 AS �D�nERP�@�~
	 , CM93.rec93_needlion AS BU�D�n�Ʒ~��
	 , CM96A.r96a_char4 AS �P�椽�q
	 , CM96A.r96a_char3 AS �ϥΪ�
	 , CM93.rec93_stfn AS ñ�ֳ�ӽФH
	 , CONVERT(datetime,CM93.rec93_date) AS �ӽФ��--rec93_date CHAR(8) > DATETIME���A���P
	 , CM93.rec93_mstfn AS ��s�H�� --����&���A���P
	 , CM93.rec93_mdate AS ��s�ɶ�
	 , CM93.rec93_fsts AS �f�֬y�{
  FROM recm93 CM93
  JOIN recm96a CM96A
    ON CM96A.r96a_form = CM93.rec93_form
  JOIN opagm20 GM20
    ON CM93.rec93_stfn = GM20.stfn_stfn
 WHERE CM93.rec93_formno = '2'
   AND CM93.rec93_fsts = 'F'
 ORDER BY �渹 DESC

--/////////////////////////////////ñ�֬���//////////////////////////////////////////
BEGIN TRAN
--////////////////////INSERT INTO WF_FLOW////////////////////////////////////////////
SELECT ('20' + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),1,2) 
	         + REPLICATE('0',4) + SUBSTRING(CONVERT(VARCHAR,CM93.rec93_form),3,8))
  FROM recm93 CM93


--////////////////////INSERT INTO WF_SIG///////////////////////////////////////////////////////////////
--rec94_fsts = 1 �� ApplySignForm�`�I�D�޼f��ñ�֤H�A���h�Ӫ��ܼg�J�[ñ�W��A������n�b�e��
--rec94_fsts = 3 ���|ñ�A����ApplySignForm�`�I���[ñ�H
--rec94_fsts = 2 OR 4 �� ApplySignForm�`�I002�M004��ñ�֤H
--rec94_fsts������ ���� WF_SIG��SIG_STEP
--�D�޼f�ֶ���|�b�Ĥ@��(�i���n�X��)
--�|ñ����b���D�ީM�B�z���D�ޤ���(�i���n�X��)
--�٥��}�lñ�֮ɭԡA�|ñ��D�ޤ����W��A��쥲��i�H��A���D��ñ�� �B�z���D�x�٬O���ѤU�W��
--ñ�ֹL�{���A�Y�Q�h�^�A�ӽЪ̥i�H��ñ�֦W��
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
		   WHEN rec94_fsts = 2 THEN rec94_stfn END --���D��
     , @PROCESS_BOSS_STFN = 
	       CASE WHEN @PROCESS_BOSS_STFN IS NOT NULL
		   THEN @PROCESS_BOSS_STFN 
		   WHEN rec94_fsts = 4 THEN rec94_stfn END --�B�z���D��
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

DECLARE @START VARCHAR(200) = '�ӽ�'
DECLARE @BACK VARCHAR(500) = '�h�^!';
DECLARE @PASS VARCHAR(500) = '�֭�!';
DECLARE @PAUSE VARCHAR(500) = '�Ȥ���!';
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






