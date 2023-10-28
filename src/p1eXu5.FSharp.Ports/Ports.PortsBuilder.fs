namespace p1eXu5.FSharp.Ports


type Port<'env, 'a> = Port of action: ('env -> 'a)


module Port =

    open System

    let run env (Port action) = action env

    let runf env (port: 'a -> Port<_,_>) = port >> run env

    let runf2 env (port: 'a -> 'b -> Port<_,_>) = fun a b -> port a b |> run env

    let runf3 env (port: 'a -> 'b -> 'c -> Port<_,_>) = fun a b c -> port a b c |> run env

    let ask = Port id

    let map f port =
        Port (fun env -> f (run env port))

    let bind f port =
        let newAction env =
            let x = run env port
            run env (f x)
        Port newAction

    let combine port1 port2 =
        port1 |> bind (fun () -> port2)

    let delay<'env, 'a> (func: unit -> Port<'env, 'a>) = func

    let retn v = (fun _ -> v) |> Port

    let withEnv f port =
        Port (fun env -> run (f env) port)

    let tryFinally compensation delayed =
        let action env =
            try
                run env delayed
            finally
                compensation ()
        Port action

    let using (f: 'a -> Port<_,_>) (v: #IDisposable) =
        tryFinally (fun () -> v.Dispose()) (f v)


module PortBuilderCE =

    type PortBuilder () =
        member _.Return(v) = Port.retn v
        member _.ReturnFrom(expr) = expr
        member _.Bind(m: Port<_,_>, f) = Port.bind f m
        member _.Zero() = Port (fun _ -> ())
        member _.Combine(expr1, expr2) = Port.combine expr1 expr2
        member _.Delay(func) = Port.delay func
        member this.While(guard, body) =
            if not (guard())
            then this.Zero()
            else this.Bind( body (), fun () ->
                this.While(guard, body))

        member _.TryFinally(body, compensation) = Port.tryFinally compensation body
        member _.Using(v, f) = Port.using f v
        member this.For(sequence: seq<_>, f) =
            this.Using(sequence.GetEnumerator(),fun enum ->
                this.While(enum.MoveNext,
                    this.Delay(fun () -> f enum.Current)))
        member _.Run(delay) =
            fun env ->
                let m = delay ()
                Port.run env m
            |> Port


    let port = PortBuilder()
