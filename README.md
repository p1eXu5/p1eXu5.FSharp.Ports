<link href="css/splendor.css" rel="stylesheet"></link>

p1eXu5.FSharp.Ports & p1eXu5.FSharp.Ports.PortTaskResult
========================================================

| Package                            | Versions                                                                                                                                                   |
| ---------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| p1eXu5.FSharp.Ports                | [![NuGet](https://img.shields.io/badge/nuget-1.0.10--preview-yellowgreen)](https://www.nuget.org/packages/p1eXu5.FSharp.Ports/1.0.10-preview)                |
| p1eXu5.FSharp.Ports.PortTaskResult | [![NuGet](https://img.shields.io/badge/nuget-1.0.10--preview-yellowgreen)](https://www.nuget.org/packages/p1eXu5.FSharp.Ports.PortTaskResult/1.0.10-preview) |

Computation expressions implementing Reader (also called the Environment monad) monade.

## Examples

Are located in [p1eXu5.FSharp.Ports.Examples](./examples/p1eXu5.FSharp.Ports.Examples/)

```fs
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
```

## p1eXu5.FSharp.Ports

### Port

```fs
type Port<'env, 'a> = Port of action: ('env -> 'a)

// CE
open p1eXu5.FSharp.Ports.PortBuilderCE

let foo =
    port {
        ...
    }
```

#### Port module:

| Function or value | Signature |
| ----------------- | --------- |
| run env port      | |
| ask               | |
| map f port        | |
| bind f port       | |
| retn v            | |
| withEnv f port    | ('a -> 'b) -> Port<'b,'c> -> Port<'a,'c> |


### PortResult

```fs
type PortResult<'env, 'Ok, 'Error> = Port<'env, Result<'Ok, 'Error>>

// CE
open p1eXu5.FSharp.Ports.PortBuilderCE

let foo =
    portResult {
        ...
    }
```

#### PortResult module:

| Function or value | Signature |
| ----------------- | --------- |
| run env port      | |
| ask               | |
| map f port        | |
| mapError f port   | |
| bind f port       | |
| retn v            | |
| withEnv f port    | ('a -> 'b) -> Port<'b,'c> -> Port<'a,'c> |
| fromResult res    | |


### PortTask

```fs
type PortTask<'env, 'a> = Port<'env, Task<'a>>

// CE
open p1eXu5.FSharp.Ports.PortTaskBuilderCE

let foo =
    portTask {
        ...
    }
```

#### PortTask module:

| Function or value       | Signature |
| ----------------------- | --------- |
| run env portTask        | |
| retn v                  | |
| ask                     | |
| map f portTask          | |
| bindT taskf portTask    | |
| bind portTaskf portTask | |
| withEnv f portTask      | |
| tee f portTask          | |
| apply mf portTask       | |
| fromTaskT t             | Task<'a> -> PortTask<'b,'a>                        |
| fromTask t              | Task -> PortTask<'a,unit>                          |
| fromTaskF f             | ('a -> Task<'b>) -> PortTask<'a0,('a -> Task<'b>)> |
| applyTask portTask portTaskf | PortTask<'cfg,'a> -> PortTask<'cfg,('a -> Task<'b>)> -> PortTask<'cfg,'b> | |
| fromValueTaskT t | |
| fromValueTask t | |
| applyValueTask portTask portTaskF | |


### PortTaskResult

```fs
type PortTaskResult<'env, 'Ok, 'Error> = Port<'env, Task<Result<'Ok, 'Error>>>

// CE
open p1eXu5.FSharp.Ports.PortTaskResultBuilderCE

let foo =
    portTaskResult {
        ...
    }
```

#### PortTaskResult module:

| Function or value       | Signature |
| ----------------------- | --------- |
| run env portTaskResult  |           |
| runSynchronously env portTaskResult | |
| retn v | |
| ask | |
| map f portTaskResult | |
| bind f portTaskResult | |
| withEnv f portTaskResult | |
| fromPort port | |
| fromPortResult port | |
| fromPortTask portTask | |
| fromPortF f | ('a -> Port<'envA,'b>) -> PortTaskResult<'envB,('a -> Port<'envA,'b>),'Error> |
| applyPort portTaskResult mf | |
| fromResult res | |
| fromTaskResult taskRes | |
| fromTaskT | |
| fromTaskT t | |
| fromTask t | |
| fromValueTaskResult vtRes | |
| fromValueTaskT valueTask | |
| fromValueTask valueTask | |
