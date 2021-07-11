* Very generic VFP COM server.
* The sole purpose of this class is to start a VFP COM environment and 
*	expose the _VFP Application object.  All other code is in the main app/exe.
* The intention is that this class/interface will never change.  Therefore, once
*	EXE is registered it will never need to be registered again. 
*	The rest of the code can be updated independently, and multiple version
*	can exist on a system without running into DLL Hell issues.

DEFINE CLASS Application AS Custom OLEPUBLIC

VFP = _VFP
lQuitOnDestroy = .f.
oTestCallback = null
oTestCallbackTimer = null

* Hide all other properties and methods
Hidden BaseClass
Hidden Class 
Hidden ClassLibrary 
Hidden Comment 
Hidden ControlCount 
Hidden Controls 
Hidden Height 
Hidden HelpContextID 
Hidden Left 
Hidden Name 
Hidden Objects 
Hidden Parent 
Hidden ParentClass 
Hidden Picture 
Hidden Tag 
Hidden Top 
Hidden WhatsThisHelpID 
Hidden Width 
Hidden Destroy 
Hidden Error 
Hidden Init 
Hidden AddObject 
Hidden AddProperty 
Hidden NewObject 
Hidden ReadExpression 
Hidden ReadMethod 
Hidden RemoveObject 
Hidden ResetToDefault 
Hidden SaveAsClass 
Hidden ShowWhatsThis 
Hidden WriteExpression 
Hidden WriteMethod 

Procedure SetTestCallback
	Lparameters loTestCallback, lnTimerSeconds
	Local loTimer as Timer
	
	lnTimerSeconds = Evl(lnTimerSeconds, 60)

	This.oTestCallback = loTestCallback
	
	* Set timer to periodically call TestDotNet()
	loTimer = CreateObject("Timer")
	BindEvent(loTimer, "Timer", This, "TestDotNet")
	loTimer.Interval = lnTimerSeconds * 1000
	loTimer.Enabled = .t.
	This.oTestCallbackTimer = loTimer
EndProc 

* Make sure calling DotNet process is still alive
* If process is killed or .NET debugging is stopped from VS, then Fox instances will stay open
Procedure TestDotNet
	Try 
		This.oTestCallback.TestDotNet()
	Catch
		Quit 
	EndTry
EndProc 

PROCEDURE Destroy
	* Optionally quit when object destroyed
	Debugout Time(0), Program()
	If This.lQuitOnDestroy
*		Inkey(1, "H")
		Quit
	EndIf 
ENDPROC

ENDDEFINE
