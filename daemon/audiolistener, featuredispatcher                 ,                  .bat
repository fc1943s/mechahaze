yarn run concurrently ^
	"powershell -c ""dotnet run --project ../src/MechaHaze.AudioListener.Daemon"" " ^
	"powershell -c ""dotnet run --project ../src/MechaHaze.FeatureDispatcher.Daemon"" "
pause
