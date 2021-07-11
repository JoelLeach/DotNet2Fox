**************************************************
*-- Class:        foxrun (c:\apps.net\foxcom\foxrun.vcx)
*-- ParentClass:  custom
*-- BaseClass:    custom
*-- Time Stamp:   03/24/18 03:12:12 PM
*
*
DEFINE CLASS foxrun AS session 


	*-- XML Metadata for customizable properties
*JAL*		_memberdata = [<VFPData><memberdata name="createparameterclause" display="CreateParameterClause"/><memberdata name="call" display="Call"/><memberdata name="callmethod" display="CallMethod"/><memberdata name="do" display="Do"/><memberdata name="docmd" display="DoCmd"/><memberdata name="eval" display="Eval"/><memberdata name="execscript" display="ExecScript"/><memberdata name="createnewobject" display="CreateNewObject"/><memberdata name="factory" display="Factory"/><memberdata name="lquitondestroy" display="lQuitOnDestroy"/><memberdata name="geterrormessage" display="GetErrorMessage"/><memberdata name="osession" display="oSession"/><memberdata name="setprivatedatasession" display="SetPrivateDataSession"/></VFPData>]
	*-- When set to .T., will Quit FoxPro when object is destroyed.
	lquitondestroy = .F.
	*-- Reference to private data session.
*JAL*	osession = .F.
	Name = "foxrun"


	*-- Create parameter clause with specified number of parameters.
	PROTECTED PROCEDURE createparameterclause
		* Create parameter clause with specified number of parameters
		* Pass in parameter variables by reference ("out" variables)
		Lparameters laParameters
		Local lnParameter, lcParameter, lcParameterClause, lnPCount

		Debugout Time(0), Program()

		lcParameterClause = ""
		If Type("laParameters", 1) = "A"
			lnPCount = Alen(laParameters)
			For lnParameter = 1 to lnPCount
				* Parameters are in array
				lcParameter = "laParameters[" + Transform(lnParameter) + "]"
				If !Empty(lcParameterClause)
					lcParameterClause = lcParameterClause + ", "
				EndIf 
				lcParameterClause = lcParameterClause + lcParameter
			EndFor 
		EndIf 

		Return lcParameterClause
	ENDPROC


	*-- Execute/call function.
	PROCEDURE call
		* Execute/call function
		Lparameters lcFunction, laParameters
		Local lcParameterClause, lcFunctionCall

		lcParameterClause = This.CreateParameterClause(@laParameters)

		Debugout Time(0), Program(), lcFunction, lcParameterClause 

		lcFunctionCall = Alltrim(lcFunction)+ "(" + lcParameterClause + ")"
		Return &lcFunctionCall
	ENDPROC


	*-- Instantiate object and execute/call class method.
	PROCEDURE callmethod
		* Instantiate object and execute/call class method.
		Lparameters lcMethod, lcClassName, lcModule, lcInApplication, laParameters
		Local lcParameterClause, lcMethodCall, loObject

		lcInApplication = Evl(lcInApplication, "")
		lcParameterClause = This.CreateParameterClause(@laParameters)

		Debugout Time(0), Program(), lcMethod, lcClassName, lcModule, lcInApplication, lcParameterClause 

		loObject = NewObject(lcClassName, lcModule, lcInApplication)
		lcMethodCall = Alltrim(lcMethod)+ "(" + lcParameterClause + ")"
		Return loObject.&lcMethodCall
	ENDPROC


	*-- Execute program.
	PROCEDURE do
		* Execute program
		Lparameters lcPRG, lcInProgram, laParameters
		Local lcParameterClause

		lcInProgram = Evl(lcInProgram, "")

		lcParameterClause = This.CreateParameterClause(@laParameters)
		If !Empty(lcParameterClause)
			* DO passes all parameters by reference, so strip out "@" to avoid syntax error
		*JAL*		lcParameterClause = Chrtran(lcParameterClause, "@", "")
			Debugout Time(0), Program(), lcPRG, lcInProgram, lcParameterClause 
		? Time(0), Program(), lcPRG, lcInProgram, lcParameterClause 
			Do (lcPRG) in (lcInProgram) with &lcParameterClause
		Else 
			Debugout Time(0), Program(), lcPRG, lcInProgram
			Do (lcPRG) in (lcInProgram)
		EndIf 

		* No return value from DO command, so always .T.
	ENDPROC


	*-- Do command.
	PROCEDURE docmd
		Lparameters lcCommand

		Debugout Time(0), Program(), lcCommand

		&lcCommand

		* Return _VFP.DoCmd(lcCommand)
	ENDPROC


	*-- Evaluate expression.
	PROCEDURE eval
		Lparameters lcExpression

		Debugout Time(0), Program(), lcExpression

		Return Evaluate(lcExpression)
		* Return _VFP.Eval(lcExpression)
	ENDPROC


	*-- Execute script.
	PROCEDURE execscript
		* Execute script on worker
		Lparameters lcScript, laParameters
		Local lcParameterClause

		lcParameterClause = This.CreateParameterClause(@laParameters)
		If !Empty(lcParameterClause)
			Debugout Time(0), Program(), "(Script)", lcParameterClause 
			Return ExecScript(lcScript, &lcParameterClause)
		Else
			Debugout Time(0), Program(), "(Script)"
			Return ExecScript(lcScript)
		EndIf 
	ENDPROC


	*-- Create and return new object.
	PROCEDURE createnewobject
		* Create and return new object.
		Lparameters lcClassName, lcModule, lcInApplication, laParameters
		Local lcParameterClause, loObject

		lcInApplication = Evl(lcInApplication, "")

		lcParameterClause = This.CreateParameterClause(@laParameters)
		If !Empty(lcParameterClause)
			Debugout Time(0), Program(), lcClassName, lcModule, lcInApplication, lcParameterClause 
			loObject = NewObject(lcClassName, lcModule, lcInApplication, &lcParameterClause)
		Else 
			Debugout Time(0), Program(), lcClassName, lcModule, lcInApplication
			loObject = NewObject(lcClassName, lcModule, lcInApplication)
		EndIf 

		Return loObject
	ENDPROC


	*-- Create and return object using abstract factory.
	PROCEDURE factory
		* Create and return object using abstract factory.
		Lparameters lcFactoryKey, laParameters
		Local lcParameterClause

		lcParameterClause = This.CreateParameterClause(@laParameters)
		If !Empty(lcParameterClause)
			Debugout Time(0), Program(), lcParameterClause 
			Return Factory(lcFactoryKey, &lcParameterClause)
		Else
			Debugout Time(0), Program()
			Return Factory(lcFactoryKey)
		EndIf 
	ENDPROC


	*-- Get last recorded error.  Requires error handler in Fox app to set _Screen.cErrorMessage.
	PROCEDURE geterrormessage
		* Get last recorded error.  
		* Requires error handler in Fox app to set _Screen.cErrorMessage.
		Local lcErrorMessage

		lcErrorMessage = ""
		If Type("_Screen.cErrorMessage") = "C"
			lcErrorMessage = _Screen.cErrorMessage
			* Reset error message
			_Screen.cErrorMessage = ""
		EndIf 

		Return lcErrorMessage
	ENDPROC


	*-- Set up private data session for object.
	PROTECTED PROCEDURE setprivatedatasession
		* Set up private data session for object
*JAL*			This.oSession = CreateObject("Session")
*JAL*			This.oSession.Name = This.Name
*JAL*			Set Datasession To This.oSession.DataSessionID
		Set Deleted On
		Set Multilocks On
		Set TablePrompt Off
		* DebugInfo("Using Session "+Alltrim(Str(Set("Datasession"))))
		Debugout Time(0), Program(), Alltrim(Str(Set("Datasession")))
	ENDPROC


	PROCEDURE Init
		This.SetPrivateDataSession()

		* Create and reset error message property
		If Type("_Screen.cErrorMessage") = "U"
			_Screen.AddProperty("cErrorMessage", "")
		EndIf 
		_Screen.cErrorMessage = ""

		Return DoDefault()
	ENDPROC


	PROCEDURE Destroy
		* Optionally quit when object destroyed
		Debugout Time(0), Program()

		? "Session", Time(0), Program()
		* Clean up and release data session
		*JAL*	Set Datasession To This.oSession.DataSessionID
		*JAL*	Close Databases All
*JAL*			Set Datasession To 1
*JAL*			This.oSession = .f.

		If This.lQuitOnDestroy
			Quit
		EndIf 
	ENDPROC


ENDDEFINE
*
*-- EndDefine: foxrun
**************************************************
