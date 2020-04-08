yarn run concurrently ^
	"powershell -c ""dotnet run --project ../src/MechaHaze.FeatureDispatcher.Daemon"" " ^
	"powershell -c ""dotnet run --project ../src/MechaHaze.UI.Backend"" " ^
	"powershell -c ""cd ../src/MechaHaze.UI.Frontend; dotnet fake run build.fsx -t FastRun"" "
pause
