* Test FoxPro PRG used for unit testing with error
Local loErr as Exception

Try 
	Error "Fox Test Error"
Catch to loErr
	_Screen.cTestErrorMessage = loErr.Message
	If _VFP.StartMode = 2	&& COM EXE
		ComReturnError("FoxPro", "FoxPro Error")
	Else	&& debug mode
		Cancel 	&& causes error to be raised in .NET
	EndIf 
EndTry