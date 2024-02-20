namespace p1eXu5.FSharp.Ports

open FsToolkit.ErrorHandling

type PortResult<'env, 'Ok, 'Error> = Port<'env, Result<'Ok, 'Error>>

module PortResult =

    open System

    let run env (portResult: PortResult<_,_,_>) = Port.run env portResult

    let runf env (port: 'a -> PortResult<_,_,_>) = port >> run env

    let runf2 env (port: 'a -> 'b -> PortResult<_,_,_>) = fun a b -> port a b |> run env

    let runf3 env (port: 'a -> 'b -> 'c -> PortResult<_,_,_>) = fun a b c -> port a b c |> run env

    let ask : PortResult<_,_,_> = Port (fun env -> result { return env })

    let map f (portResult: PortResult<_,_,_>) =
        fun env ->
            result {
                let! x = portResult |> run env
                return f x
            }
        |> Port

    let mapError f (portResult: PortResult<_,_,_>) =
        fun env ->
            result {
                let res = portResult |> run env
                return! res |> Result.mapError f
            }
        |> Port

    let bind (f: 'a -> PortResult<_,'b,_>) (portResult: PortResult<_,'a,_>) =
        fun env ->
            result {
                let! x = portResult |> run env
                return! f x |> run env
            }
        |> Port

    let combine port1 port2 =
        port1 |> bind (fun () -> port2)

    let delay<'env, 'a, 'err> (func: unit -> PortResult<'env, 'a, 'err>) = func

    let retn v : PortResult<_,_,_> = (fun _ -> v |> Ok) |> Port

    let withEnv f (portResult: PortResult<_,_,_>) : PortResult<_,_,_>=
        fun env ->
            result {
                return! run (f env) portResult
            }
        |> Port

    let tryFinally compensation delayed =
        let action env =
            result {
                try
                    return! run env delayed
                finally
                    compensation ()
            }
        Port action

    let using (f: 'a -> PortResult<_,_,_>) (v: #IDisposable) =
        tryFinally (fun () -> v.Dispose()) (f v)

    let fromResult (res: Result<_,_>) : PortResult<_,_,_> =
        Port (fun _ -> result { return! res })

    let fromPort (port: Port<_,_>) : PortResult<_,_,_> =
        fun env -> result { return Port.run env port }
        |> Port

module PortResultBuilderCE =

    type PortResultBuilder () =
        member _.Return(v) = PortResult.retn v
        member _.ReturnFrom(expr: PortResult<_,_,_>) = expr
        member _.ReturnFrom(expr: Result<_,_>) = expr |> PortResult.fromResult

        member _.Bind(m: PortResult<_,'a,_>, f: 'a -> PortResult<_,'b,_>) = PortResult.bind f m
        member this.Bind(m: Result<'a,_>, f: 'a -> PortResult<_,'b,_>) = this.Bind(m |> PortResult.fromResult, f)
        member this.Bind(m: Port<_,'a>, f: 'a -> PortResult<_,'b,_>) = this.Bind(m |> PortResult.fromPort, f)

        member _.Zero() = Port (fun _ -> Ok ())
        member _.Combine(expr1, expr2) = PortResult.combine expr1 expr2
        member _.Delay(func) = func

        member this.While(guard, body: unit -> PortResult<'env, unit, _>) : PortResult<_,_,_> =
            if not (guard())
            then this.Zero()
            else this.Bind( body (), fun () ->
                this.While(guard, body))

        member _.TryWith(delayed: unit -> PortResult<_,_,_>, handler: exn -> PortResult<_,_,_>) =
            fun env ->
                result {
                    try
                        return! delayed() |> PortResult.run env
                    with e ->
                        return! handler e |> PortResult.run env
                }
            |> Port

        member _.TryFinally(body, compensation) = PortResult.tryFinally compensation body
        member _.Using(v, f) = PortResult.using f v
        member this.For(sequence: seq<_>, f) =
            this.Using(sequence.GetEnumerator(),fun enum ->
                this.While(enum.MoveNext,
                    this.Delay(fun () -> f enum.Current)))
        member _.Run(delay) =
            fun env ->
                result {
                    let m = delay ()
                    return! PortResult.run env m
                }
            |> Port


    let portResult = PortResultBuilder()