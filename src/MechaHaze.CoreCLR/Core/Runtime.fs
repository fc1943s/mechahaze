namespace MechaHaze.CoreCLR.Core

open Serilog
open System.Diagnostics
open System.Linq
open System.Collections.Concurrent
open System

module Runtime =
    let getStackTrace () =
        Environment.StackTrace.Split (Environment.NewLine.ToCharArray (), StringSplitOptions.RemoveEmptyEntries)

    let executePowerShellAsync (cmd: string list) =
        async {
            let cmd = cmd |> String.concat "; "
            let cmd = cmd.Replace("\"", "\\\"\"")
            //  let cmd = if powershell then cmd.Replace ("\\", "\\\\") else cmd
            let startInfo =
                ProcessStartInfo
                    (FileName = "pwsh.exe",
                     Arguments = $""" -c "{cmd}" """,
                     RedirectStandardOutput = true,
                     RedirectStandardError = true,
                     UseShellExecute = false,
                     CreateNoWindow = true)

            use proc = new Process(StartInfo = startInfo)
            let log = ConcurrentStack()

            let event =
                fun (error: bool) (e: DataReceivedEventArgs) ->
                    if e.Data <> null then
                        let txt =
                            (Regexxer.matchAllEx (e.Data, @"\[(={20,})", Some(fun _ -> "="), None))
                                .ReplacedText

                        try
                            Log.Verbose("{Error}{Id}: " + txt, (if error then 'E' else ' '), proc.Id)
                        with ex -> Log.Error(ex, "ERROR ON PROCESS DATA. TXT: {txt}", txt)

                        log.Push txt

            proc.OutputDataReceived.Add(event false)
            proc.ErrorDataReceived.Add(event true)

            Log.Debug("Starting process: " + cmd)

            if not (proc.Start())
            then failwithf "Error executing script: %s" cmd

            proc.BeginErrorReadLine()
            proc.BeginOutputReadLine()

            do! proc.WaitForExitAsync() |> Async.AwaitTask

            let result =
                log.Reverse() |> String.concat Environment.NewLine

            Log.Debug("Process finished with result: {@Code}", proc.ExitCode)

            return proc.ExitCode, result
        }


    let executeCondaAsync env cmd =
        async { return! executePowerShellAsync ([ $"conda activate {env}" ] @ cmd) }
