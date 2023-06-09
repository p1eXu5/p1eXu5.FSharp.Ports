﻿namespace p1eXu5.FSharp.Ports

open System.Threading.Tasks
open FsToolkit.ErrorHandling
open p1eXu5.FSharp.Ports.PortTaskBuilderCE

type PortTaskResult<'env, 'Ok, 'Error> = Port<'env, Task<Result<'Ok, 'Error>>>

module PortTaskResult =
    let run env (m: PortTaskResult<_,_,_>) = PortTask.run env m

    let runSynchronously env (m: PortTaskResult<_,_,_>) = PortTask.runSynchronously env m

    let retn v : PortTaskResult<_,_,_> = (fun _ -> taskResult { return v }) |> Port

    /// Create a TaskPort which returns the environment itself
    let ask : PortTaskResult<_,_,_> = Port (fun env -> taskResult { return env })

    let map f taskResultPort : PortTaskResult<'env, 'Ok, 'Error> =
        Port (fun env -> taskResult { return! TaskResult.map f (PortTask.run env taskResultPort) })

    /// flatMap a function over a TaskPort
    let bind (f: 'a -> PortTaskResult<'env,_,_>) (m: PortTaskResult<'env,'a,_>) : PortTaskResult<'env,_,_> =
        fun env ->
            taskResult {
                let! x = run env m
                return! (run env (f x))
            }
        |> Port

    let withEnv f (m: PortTaskResult<_,_,_>) : PortTaskResult<_,_,_> =
        Port (fun env ->
            taskResult {
                return! run (f env) m
            }
        )

    // ===============
    // Port
    // ===============

    let fromPort (expr: Port<_,_>) : PortTaskResult<_,_,_> =
        fun env -> taskResult { return Port.run env expr }
        |> Port

    let fromPortTask (expr: PortTask<_,_>) : PortTaskResult<_,_,_> =
        fun env ->
            task {
                let! res = PortTask.run env expr
                return res |> Ok
            }
        |> Port

    let fromPortF (f: 'a -> Port<_,_>) : PortTaskResult<_,_,_> =
        fun _ -> taskResult { return f }
        |> Port

    let applyPort (m: PortTaskResult<_,_,_>) (mf: PortTaskResult<_, ('ok -> Port<_, 'b>), _>) : PortTaskResult<_,_,_> =
        fun env ->
            taskResult {
                let! a = run env m
                let! f = run env mf
                let p = f a
                return Port.run env p
            }
        |> Port

    let fromResult (res: Result<_,_>) : PortTaskResult<_,_,_> =
        Port (fun _ -> taskResult { return! res })

    let fromTaskResult (expr: Task<Result<_,_>>) : PortTaskResult<_,_,_> =
        Port (fun _ -> taskResult { return! expr })

    let fromTaskT (expr: Task<_>) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = expr
                return Ok v
            }
        )

    let fromTask (expr: Task) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = expr
                return Ok v
            }
        )

    let fromValueTaskResult (expr: ValueTask<Result<_,_>>) : PortTaskResult<_,_,_> =
        Port (fun _ -> taskResult { return! expr })

    let fromValueTaskT (valueTask: ValueTask<_>) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = valueTask
                return Ok v
            }
        )

    let fromValueTask (expr: ValueTask) : PortTaskResult<_,_,_> =
        Port (fun _ ->
            task {
                let! v = expr
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

        member this.TryWith(delayed: unit -> PortTaskResult<_,_,_>, handler: exn -> PortTaskResult<_,_,_>) =
            try
                this.ReturnFrom(delayed())
            with
                e ->
                    handler e

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