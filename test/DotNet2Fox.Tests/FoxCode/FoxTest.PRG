DEFINE CLASS FoxTest AS Custom

PROCEDURE AddNumbers
	* Test FoxPro class used for unit testing
	Lparameters lnNum1, lnNum2
	Local lnResult

	lnResult = lnNum1 + lnNum2
	? lnResult

	Return lnResult
ENDPROC

EndDefine 