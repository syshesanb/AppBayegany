Set WshShell = CreateObject("WScript.Shell")
WshShell.CurrentDirectory = "D:\Projects_AI\tahgigat\Khodam\Bayegany\App_Bayegany\bin\Release\net9.0-windows\win-x64"
WshShell.Run """D:\Projects_AI\tahgigat\Khodam\Bayegany\App_Bayegany\bin\Release\net9.0-windows\win-x64\BayeganyApp.exe""", 1, False
