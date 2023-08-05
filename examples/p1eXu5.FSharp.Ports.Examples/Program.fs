open p1eXu5.FSharp.Ports.PortTaskBuilderCE
open p1eXu5.FSharp.Ports.PortTaskResultBuilderCE

open Microsoft.Extensions.Logging
open p1eXu5.FSharp.Ports


let logger = LoggerFactory.Create(fun builder -> builder.AddConsole() |> ignore).CreateLogger("Program")


// ========================
// Deps as anonymous record
// ========================

let deps = {| Logger = logger |}

// ------------------------
// PortTask
// ------------------------

let inline loggerPortTask<'T when 'T : (member Logger: ILogger)> =
    portTask {
        return! 
            PortTask.ask
            |> PortTask.map (fun deps -> (^T : (member Logger: ILogger) deps))
    }

let logInPortTask =
    portTask {
        let! logger = loggerPortTask
        do logger.LogInformation("In logInPortTask")
    }

do
    logInPortTask
    |> PortTask.runSynchronously deps


// ------------------------
// PortTaskResult
// ------------------------

let loggerPortTaskResult =
    loggerPortTask |> PortTaskResult.fromPortTask

// need to specify error type
let logInPortTaskResult : PortTaskResult<_, _, string> =
    portTaskResult {
        let! logger = loggerPortTaskResult
        do logger.LogInformation("In logInPortTaskResult")
    }

do
    logInPortTaskResult
    |> PortTaskResult.runSynchronously deps
    |> Result.iter (fun _ -> ())
    