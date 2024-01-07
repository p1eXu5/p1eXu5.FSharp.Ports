namespace p1eXu5.FSharp.Ports

open System.Threading.Tasks
open FsToolkit.ErrorHandling
open p1eXu5.FSharp.Ports.PortTaskBuilderCE
open PortResultBuilderCE

type PortTaskResult<'env, 'Ok, 'Error> = Port<'env, Task<Result<'Ok, 'Error>>>

module PortTaskResult =
    let run env (portTaskResult: PortTaskResult<_,_,_>) = PortTask.run env portTaskResult

    let runf env (port: 'a -> PortTaskResult<_,_,_>) = port >> run env

    let runf2 env (port: 'a -> 'b -> PortTaskResult<_,_,_>) = fun a b -> port a b |> run env

    let runf3 env (port: 'a -> 'b -> 'c -> PortTaskResult<_,_,_>) = fun a b c -> port a b c |> run env

    let runSynchronously env (portTaskResult: PortTaskResult<_,_,_>) = PortTask.runSynchronously env portTaskResult

    let retn v : PortTaskResult<_,_,_> = (fun _ -> taskResult { return v }) |> Port

    /// Create a TaskPort which returns the environment itself
    let ask : PortTaskResult<_,_,_> = Port (fun env -> taskResult { return env })

    let map f (portTaskResult: PortTaskResult<_,'OkA,_>) : PortTaskResult<'env, 'OkB, 'Error> =
        Port (fun env -> taskResult { return! TaskResult.map f (PortTask.run env portTaskResult) })

    let mapError f (portTaskResult: PortTaskResult<_,'Ok, 'ErrorInput>) : PortTaskResult<'env, 'Ok, 'ErrorOutput> =
        Port (fun env ->
            task {
                let! result = portTaskResult |> run env
                return
                    result |> Result.mapError f
            }
        )

    /// flatMap a function over a TaskPort
    let bind (f: 'a -> PortTaskResult<'env,_,_>) (portTaskResult: PortTaskResult<'env,'a,_>) : PortTaskResult<'env,_,_> =
        fun env ->
            taskResult {
                let! x = run env portTaskResult
                return! (run env (f x))
            }
        |> Port

    let withEnv f (portTaskResult: PortTaskResult<_,_,_>) : PortTaskResult<_,_,_> =
        Port (fun env ->
            taskResult {
                return! run (f env) portTaskResult
            }
        )

    // ===============
    // Port
    // ===============

    let fromPort (port: Port<_,_>) : PortTaskResult<_,_,_> =
        fun env -> taskResult { return Port.run env port }
        |> Port

    let fromPortResult (port: PortResult<'env, 'ok, 'err>) : PortTaskResult<'env, 'ok, 'err> =
        fun env -> taskResult { return! PortResult.run env port }
        |> Port

    let fromPortTask (portTask: PortTask<_,_>) : PortTaskResult<_,_,_> =
        fun env ->
            task {
                let! res = PortTask.run env portTask
                return res |> Ok
            }
        |> Port

    let fromPortF (f: 'a -> Port<'envA,'b>) : PortTaskResult<'envB,_,'Error> =
        fun _ -> taskResult { return f }
        |> Port

    let applyPort (portTaskResult: PortTaskResult<_,_,_>) (mf: PortTaskResult<_, ('Ok -> Port<_, 'b>), _>) : PortTaskResult<_,_,_> =
        fun env ->
            taskResult {
                let! a = run env portTaskResult
                let! f = run env mf
                let p = f a
                return Port.run env p
            }
        |> Port

    let fromResult (res: Result<_,_>) : PortTaskResult<_,_,_> =
        Port (fun _ -> taskResult { return! res })

    let fromTaskResult (taskRes: Task<Result<_,_>>) : PortTaskResult<_,_,_> =
        Port (fun _ -> taskResult { return! taskRes })

    let fromTaskT (t: Task<_>) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = t
                return Ok v
            }
        )

    let fromTask (t: Task) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = t
                return Ok v
            }
        )

    let fromValueTaskResult (vtRes: ValueTask<Result<_,_>>) : PortTaskResult<_,_,_> =
        Port (fun _ -> taskResult { return! vtRes })

    let fromValueTaskT (valueTask: ValueTask<_>) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = valueTask
                return Ok v
            }
        )

    let fromValueTask (valueTask: ValueTask) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = valueTask
                return Ok v
            }
        )


open System

module PortTaskResultBuilderCE =

    type PortTaskResultBuilder () =
        member _.Zero() = PortTaskResult.retn ()

        member _.Return(v) = PortTaskResult.retn v
        
        member _.ReturnFrom(expr: PortTaskResult<_,_,_>) = expr
        member _.ReturnFrom(expr: Result<_,_>) = expr |> PortTaskResult.fromResult
        member _.ReturnFrom(expr: Task<Result<'a,'err>>) = expr |> PortTaskResult.fromTaskResult
        member _.ReturnFrom(expr: ValueTask<Result<'a,'err>>) = expr |> PortTaskResult.fromValueTaskResult
        // member _.ReturnFrom(expr: Task) = PortTaskResult.fromTask expr
        // member _.ReturnFrom(expr: ValueTask) = PortTaskResult.fromValueTask expr

        member    _.Bind(m: PortTaskResult<'env,'a,'err>, f: 'a -> PortTaskResult<'env,'b,'err>) = PortTaskResult.bind f m
        // member this.Bind(m: PortTask<'env,'a>,            f: 'a -> PortTaskResult<'env,'b,_>) = this.Bind(m |> PortTaskResult.fromPortTask, f)
        // member this.Bind(m: Port<'env,'a>,                f: 'a -> PortTaskResult<'env,'b,'err>) = this.Bind(m |> PortTaskResult.fromPort, f)
        member this.Bind(m: Task<Result<'a,'err>>,        f: 'a -> PortTaskResult<'env,'b,'err>) = this.Bind(m |> PortTaskResult.fromTaskResult, f)
        member this.Bind(m: ValueTask<Result<'a,'err>>,   f: 'a -> PortTaskResult<'env,'b,'err>) = this.Bind(m |> PortTaskResult.fromValueTaskResult, f)
        member this.Bind(m: Result<'a,'err>,        f: 'a -> PortTaskResult<'env,'b,'err>) = this.Bind(m |> PortTaskResult.fromResult, f)
        // member this.Bind(m: Task,                         f: unit -> PortTaskResult<'env,'b,'err>) = this.Bind(m |> PortTaskResult.fromTask, f)
        // member this.Bind(m: ValueTask,                    f: unit -> PortTaskResult<'env,'b,'err>) = this.Bind(m |> PortTaskResult.fromValueTask, f)

        member this.Combine(expr1: PortTaskResult<_,_,_>, expr2: PortTaskResult<_,_,_>)       = this.Bind(expr1, (fun () -> expr2))
        member this.Combine(expr1: PortTaskResult<_,_,_>, expr2: 'a -> PortTaskResult<_,_,_>) = this.Bind(expr1, expr2)

        member _.Delay(func) = func
        member _.Run(delay) =
            Port (fun env ->
                taskResult {
                    let m = delay ()
                    return! PortTaskResult.run env m
                }
            )

        member _.TryWith(delayed: unit -> PortTaskResult<_,_,_>, handler: exn -> PortTaskResult<_,_,_>) =
            fun env ->
                taskResult {
                    try
                        return! delayed() |> PortTaskResult.run env
                    with e ->
                        return! handler e |> PortTaskResult.run env
                }
            |> Port

        member _.TryFinally(delayed: unit -> PortTaskResult<_,_,_>, compensation) =
            Port (fun env ->
                taskResult {
                    try
                        return! PortTaskResult.run env (delayed())
                    finally compensation()
                }
            )


        member this.Using(disposable:'a, body: 'a -> PortTaskResult<_,_,_>) =
            match box disposable with
            | :? IAsyncDisposable as disp ->
                Port (fun env ->
                    taskResult {
                        use! d = task { return disp }
                        return! PortTaskResult.run env (body disposable)
                    }
                )

            | :? IDisposable as disp ->
                let body' =
                    fun () -> body disposable

                this.TryFinally(body', fun () -> disp.Dispose ())

            | _ ->
                let body' =
                    fun () -> body disposable

                this.TryFinally(body', fun () -> ())

        member this.While(predicate, delayed: unit -> PortTaskResult<'env, unit, _>) =
            if predicate () then
                this.Zero()
            else
                this.Bind( delayed (), fun () ->
                    this.While(predicate, delayed))

        member this.For(sequence:seq<_>, body) =
           this.Using(sequence.GetEnumerator(),fun enum ->
                this.While(enum.MoveNext,
                    this.Delay(fun () -> body enum.Current)))

    let portTaskResult = PortTaskResultBuilder()